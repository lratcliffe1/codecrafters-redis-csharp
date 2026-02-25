using Microsoft.Extensions.Caching.Memory;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Cache.Waiters;

namespace codecrafters_redis.src.Cache;

public interface ICacheStore
{
  void Set(string key, CacheValue value);
  void Set(string key, CacheValue value, int expirationMilliseconds);
  int Append(string key, List<string> values);
  int Prepend(string key, List<string> values);
  bool TryGetValue(string key, out CacheValue? value);
  List<string> GetLRange(string key, int start, int stop);
  int GetLLen(string key);
  List<string>? LPop(string key, int popCount);
  Task<bool> WaitForListEntriesAsync(string key, double expirationSeconds, CancellationToken cancellationToken = default);
  Task<bool> WaitForStreamEntriesAsync(IReadOnlyList<(string key, string id)> streams, double expirationMilliseconds, CancellationToken cancellationToken = default);
  List<string> GetKeys(string pattern);
  void ZAdd(string key, double score, string member);
}

public sealed class Cache : ICacheStore
{
  private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

  private readonly Dictionary<string, List<ListWaiter>> _listWaitersByKey = [];
  private readonly Dictionary<string, List<StreamWaiter>> _streamWaitersByKey = [];

  public void Set(string key, CacheValue value)
  {
    EnsureLoopOwner();
    _memoryCache.Set(key, value);
    HandlePostSetEffects(key, value);
  }

  public void Set(string key, CacheValue value, int expirationMilliseconds)
  {
    EnsureLoopOwner();
    _memoryCache.Set(key, value, TimeSpan.FromMilliseconds(expirationMilliseconds));
    HandlePostSetEffects(key, value);
  }

  public int Append(string key, List<string> values)
  {
    EnsureLoopOwner();
    if (TryGetListValue(key, out List<string> existingValues))
    {
      existingValues.AddRange(values);

      UpdateBlockingEvents(key);
      return existingValues.Count;
    }
    _memoryCache.Set(key, CacheValue.List(values));

    UpdateBlockingEvents(key);
    return values.Count;
  }

  public int Prepend(string key, List<string> values)
  {
    EnsureLoopOwner();
    values.Reverse();

    if (TryGetListValue(key, out List<string> existingValues))
    {
      existingValues.InsertRange(0, values);

      UpdateBlockingEvents(key);
      return existingValues.Count;
    }

    _memoryCache.Set(key, CacheValue.List(values));

    UpdateBlockingEvents(key);
    return values.Count;
  }

  public bool TryGetValue(string key, out CacheValue? value)
  {
    EnsureLoopOwner();
    if (_memoryCache.TryGetValue(key, out CacheValue? cachedValue) && cachedValue != null)
    {
      value = cachedValue;
      return true;
    }

    value = null;
    return false;
  }

  public List<string> GetLRange(string key, int start, int stop)
  {
    EnsureLoopOwner();
    if (!TryGetListValue(key, out List<string> existingValues))
      return [];

    int count = existingValues.Count;
    if (count == 0)
      return [];

    int startIndex = start < 0 ? count + start : start;
    int stopIndex = stop < 0 ? count + stop + 1 : stop + 1;

    if (startIndex > stopIndex)
      return [];

    if (startIndex > count)
      return [];

    if (startIndex < 0)
      startIndex = 0;

    if (stopIndex > count)
      stopIndex = count;

    return existingValues[startIndex..stopIndex];
  }

  public int GetLLen(string key)
  {
    EnsureLoopOwner();
    if (TryGetListValue(key, out List<string> existingValues))
    {
      return existingValues.Count;
    }

    return 0;
  }

  public List<string>? LPop(string key, int popCount)
  {
    EnsureLoopOwner();
    if (TryGetListValue(key, out List<string> existingValues))
    {
      if (existingValues.Count > 0)
      {
        int countToPop = Math.Min(popCount, existingValues.Count);
        List<string> value = existingValues[0..countToPop];
        existingValues.RemoveRange(0, countToPop);
        return value;
      }
    }

    return null;
  }

  public async Task<bool> WaitForListEntriesAsync(
    string key,
    double expirationSeconds,
    CancellationToken cancellationToken = default)
  {
    EnsureLoopOwner();
    var waiter = new ListWaiter();
    RegisterBlockingEvent(key, waiter);

    try
    {
      return await WaitForSignalAsync(waiter.Signal.Task, expirationSeconds * 1000, cancellationToken);
    }
    finally
    {
      UnregisterBlockingEvent(key, waiter);
    }
  }

  public async Task<bool> WaitForStreamEntriesAsync(
    IReadOnlyList<(string key, string id)> streams,
    double expirationMilliseconds,
    CancellationToken cancellationToken = default)
  {
    EnsureLoopOwner();
    if (streams.Count == 0)
    {
      return false;
    }

    StreamWaiter waiter = new(streams);
    RegisterStreamWaiter(waiter);

    try
    {
      return await WaitForSignalAsync(waiter.Signal.Task, expirationMilliseconds, cancellationToken);
    }
    finally
    {
      UnregisterStreamWaiter(waiter);
    }
  }

  private void NotifyStreamEntryAdded(string key, string entryId)
  {
    EnsureLoopOwner();
    if (!_streamWaitersByKey.TryGetValue(key, out var waiters))
    {
      return;
    }

    foreach (var waiter in waiters.ToList())
    {
      if (waiter.TryGetLastSeenId(key, out string lastSeenId) && StreamIdHelper.IsGreaterThan(entryId, lastSeenId))
      {
        waiter.Signal.TrySetResult(true);
      }
    }
  }

  private bool TryGetListValue(string key, out List<string> values)
  {
    EnsureLoopOwner();
    values = [];
    if (_memoryCache.TryGetValue(key, out CacheValue? cachedValue) && cachedValue != null)
    {
      return cachedValue.TryGetList(out values);
    }

    return false;
  }

  private void HandlePostSetEffects(string key, CacheValue value)
  {
    EnsureLoopOwner();
    if (value.Type == CacheValueType.List)
    {
      UpdateBlockingEvents(key);
    }

    if (value.Type == CacheValueType.StreamEntries)
    {
      if (value.TryGetStream(out var entries) && entries.Count > 0)
      {
        NotifyStreamEntryAdded(key, entries.Last().Id);
      }
    }
  }

  private async Task<bool> WaitForSignalAsync(
    Task signalTask,
    double expirationMilliseconds,
    CancellationToken cancellationToken = default)
  {
    EnsureLoopOwner();
    if (expirationMilliseconds == 0)
    {
      await signalTask.WaitAsync(cancellationToken);
      return true;
    }

    try
    {
      await signalTask.WaitAsync(TimeSpan.FromMilliseconds(expirationMilliseconds), cancellationToken);
      return true;
    }
    catch (TimeoutException)
    {
      return false;
    }
  }

  private void UpdateBlockingEvents(string key)
  {
    EnsureLoopOwner();
    if (!_listWaitersByKey.TryGetValue(key, out var blockingEvents))
    {
      return;
    }

    while (blockingEvents.Count > 0)
    {
      ListWaiter nextWaiter = blockingEvents[0];
      blockingEvents.RemoveAt(0);

      if (blockingEvents.Count == 0)
      {
        _listWaitersByKey.Remove(key);
      }

      if (nextWaiter.Signal.TrySetResult(true))
      {
        break;
      }
    }
  }

  private void RegisterStreamWaiter(StreamWaiter waiter)
  {
    EnsureLoopOwner();
    foreach ((string key, _) in waiter.Streams)
    {
      if (_streamWaitersByKey.TryGetValue(key, out var waiters))
      {
        waiters.Add(waiter);
      }
      else
      {
        _streamWaitersByKey.Add(key, [waiter]);
      }
    }
  }

  private void UnregisterStreamWaiter(StreamWaiter waiter)
  {
    EnsureLoopOwner();
    foreach ((string key, _) in waiter.Streams)
    {
      if (!_streamWaitersByKey.TryGetValue(key, out var waiters))
      {
        continue;
      }

      waiters.Remove(waiter);
      if (waiters.Count == 0)
      {
        _streamWaitersByKey.Remove(key);
      }
    }
  }

  private void RegisterBlockingEvent(string key, ListWaiter waiter)
  {
    EnsureLoopOwner();
    if (_listWaitersByKey.TryGetValue(key, out var blockingEvents))
    {
      blockingEvents.Add(waiter);
      return;
    }

    _listWaitersByKey.Add(key, [waiter]);
  }

  private void UnregisterBlockingEvent(string key, ListWaiter waiter)
  {
    EnsureLoopOwner();
    if (!_listWaitersByKey.TryGetValue(key, out var blockingEvents))
    {
      return;
    }

    blockingEvents.Remove(waiter);
    if (blockingEvents.Count == 0)
    {
      _listWaitersByKey.Remove(key);
    }
  }

  private static void EnsureLoopOwner()
  {
    if (!LoopOwnerContext.IsOnOwnerLane)
    {
      throw new InvalidOperationException("Cache access must execute on the command event-loop owner lane.");
    }
  }

  public List<string> GetKeys(string pattern)
  {
    EnsureLoopOwner();
    return _memoryCache.Keys
      .Where(key => key is string)
      .Where(key => key.ToString()!.StartsWith(pattern))
      .Select(key => key.ToString()!).ToList();
  }

  public void ZAdd(string key, double score, string member)
  {
    EnsureLoopOwner();
    if (!TryGetValue(key, out CacheValue? cachedValue) || cachedValue == null || !cachedValue.TryGetZSet(out List<ZSetEntry> existingValues))
    {
      existingValues = [];
    }

    existingValues.Add(new ZSetEntry(member, score));
    _memoryCache.Set(key, existingValues);
  }
}
