using Microsoft.Extensions.Caching.Memory;

namespace codecrafters_redis.src.Cache;

public class Cache
{
  private static readonly MemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

  private static readonly Dictionary<string, List<ManualResetEvent>> _events = [];

  public static void Set(string key, CacheValue value)
  {
    _memoryCache.Set(key, value);

    UpdateBlockingEvents(key);
  }

  public static void Set(string key, CacheValue value, int expirationMilliseconds)
  {
    _memoryCache.Set(key, value, TimeSpan.FromMilliseconds(expirationMilliseconds));

    UpdateBlockingEvents(key);
  }

  public static int Append(string key, List<string> values)
  {
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
    if (TryGetListValue(key, out List<string> existingValues))
    {
      return existingValues.Count;
    }

    return 0;
  }

  public static List<string>? LPop(string key, int popCount)
  {
    if (TryGetListValue(key, out List<string> existingValues))
    {
      if (existingValues.Count > 0)
      {
        List<string> value = existingValues[0..popCount];
        existingValues.RemoveRange(0, popCount);
        return value;
      }
    }

    return null;
  }

  public static List<string>? BLPop(string key, double expiration)
  {
    var result = LPop(key, 1);
    if (result != null)
      return result;

    var newEvent = new ManualResetEvent(false);

    try
    {
      lock (_events)
      {
        if (_events.TryGetValue(key, out var blockingEvents))
        {
          blockingEvents.Add(newEvent);
        }
        else
        {
          _events.Add(key, [newEvent]);
        }
      }

      if (expiration == 0)
      {
        newEvent.WaitOne();
      }
      else
      {
        newEvent.WaitOne((int)(expiration * 1000));
      }

      return LPop(key, 1);
    }
    finally
    {
      lock (_events)
      {
        if (_events.TryGetValue(key, out var blockingEvents))
        {
          blockingEvents.Remove(newEvent);
          if (blockingEvents.Count == 0)
          {
            _events.Remove(key);
          }
        }
      }

      newEvent.Dispose();
    }
  }

  private static bool TryGetListValue(string key, out List<string> values)
  {
    values = [];
    if (_memoryCache.TryGetValue(key, out CacheValue? cachedValue) && cachedValue != null)
    {
      return cachedValue.TryGetList(out values);
    }

    return false;
  }

  private static void UpdateBlockingEvents(string key)
  {
    lock (_events)
    {
      if (_events.TryGetValue(key, out var blockingEvents) && blockingEvents.Count > 0)
      {
        var nextEvent = blockingEvents[0];
        blockingEvents.RemoveAt(0);

        if (blockingEvents.Count == 0)
        {
          _events.Remove(key);
        }

        nextEvent.Set();
      }
    }
  }
}