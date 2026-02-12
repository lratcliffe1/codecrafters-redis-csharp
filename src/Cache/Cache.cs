using Microsoft.Extensions.Caching.Memory;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Cache;

public class Cache
{
  // Contract: all cache and waiter state is owned by the command event-loop lane.
  private static readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

  private static readonly Dictionary<string, List<ListWaiter>> _listWaitersByKey = [];
  private static readonly Dictionary<string, List<StreamWaiter>> _streamWaitersByKey = [];

  public static void Set(string key, CacheValue value)
  {
    EnsureLoopOwner();
    _memoryCache.Set(key, value);
    HandlePostSetEffects(key, value);
  }

  public static void Set(string key, CacheValue value, int expirationMilliseconds)
  {
    EnsureLoopOwner();
    _memoryCache.Set(key, value, TimeSpan.FromMilliseconds(expirationMilliseconds));
    HandlePostSetEffects(key, value);
  }

  public static int Append(string key, List<string> values)
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

  public static int Prepend(string key, List<string> values)
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

  public static bool TryGetValue(string key, out CacheValue? value)
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

  public static List<string> GetLRange(string key, int start, int stop)
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

  public static int GetLLen(string key)
  {
    EnsureLoopOwner();
    if (TryGetListValue(key, out List<string> existingValues))
    {
      return existingValues.Count;
    }

    return 0;
  }

  public static List<string>? LPop(string key, int popCount)
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

  public static async Task<bool> WaitForListEntriesAsync(
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

  public static async Task<bool> WaitForStreamEntriesAsync(
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

  public static void NotifyStreamEntryAdded(string key, string entryId)
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

  private static bool TryGetListValue(string key, out List<string> values)
  {
    EnsureLoopOwner();
    values = [];
    if (_memoryCache.TryGetValue(key, out CacheValue? cachedValue) && cachedValue != null)
    {
      return cachedValue.TryGetList(out values);
    }

    return false;
  }

  private static void HandlePostSetEffects(string key, CacheValue value)
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

  private static async Task<bool> WaitForSignalAsync(
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

  private static void UpdateBlockingEvents(string key)
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

  private static void RegisterStreamWaiter(StreamWaiter waiter)
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

  private static void UnregisterStreamWaiter(StreamWaiter waiter)
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

  private static void RegisterBlockingEvent(string key, ListWaiter waiter)
  {
    EnsureLoopOwner();
    if (_listWaitersByKey.TryGetValue(key, out var blockingEvents))
    {
      blockingEvents.Add(waiter);
      return;
    }

    _listWaitersByKey.Add(key, [waiter]);
  }

  private static void UnregisterBlockingEvent(string key, ListWaiter waiter)
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

  private sealed class ListWaiter
  {
    public TaskCompletionSource<bool> Signal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
  }

  private sealed class StreamWaiter
  {
    private readonly Dictionary<string, string> _lastSeenIds;

    public StreamWaiter(IReadOnlyList<(string key, string id)> streams)
    {
      Streams = streams;
      Signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      _lastSeenIds = streams.ToDictionary(stream => stream.key, stream => stream.id);
    }

    public IReadOnlyList<(string key, string id)> Streams { get; }
    public TaskCompletionSource<bool> Signal { get; }

    public bool TryGetLastSeenId(string key, out string lastSeenId)
    {
      if (_lastSeenIds.TryGetValue(key, out string? value))
      {
        lastSeenId = value;
        return true;
      }

      lastSeenId = string.Empty;
      return false;
    }
  }

  private static void EnsureLoopOwner()
  {
    if (!LoopOwnerContext.IsOnOwnerLane)
    {
      throw new InvalidOperationException("Cache access must execute on the command event-loop owner lane.");
    }
  }
}