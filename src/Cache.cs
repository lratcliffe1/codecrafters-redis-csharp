using Microsoft.Extensions.Caching.Memory;

namespace codecrafters_redis.src;

public static class Cache
{
  private static readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

  public static void Set(string key, string value)
  {
    _memoryCache.Set(key, value);
  }

  public static void Set(string key, string value, int expirationMilliseconds)
  {
    _memoryCache.Set(key, value, TimeSpan.FromMilliseconds(expirationMilliseconds));
  }

  public static int Append(string key, List<string> values)
  {
    if (_memoryCache.TryGetValue(key, out List<string>? existingValues) && existingValues != null)
    {
      existingValues.AddRange(values);
      return existingValues.Count;
    }
    _memoryCache.Set(key, values);
    return values.Count;
  }

  public static int Prepend(string key, List<string> values)
  {
    values.Reverse();

    if (_memoryCache.TryGetValue(key, out List<string>? existingValues) && existingValues != null)
    {
      existingValues.InsertRange(0, values);
      return existingValues.Count;
    }

    _memoryCache.Set(key, values);
    return values.Count;
  }

  public static bool TryGetValue(string key, out string value)
  {
    if (_memoryCache.TryGetValue(key, out string? cachedValue) && cachedValue != null)
    {
      value = cachedValue;
      return true;
    }

    value = string.Empty;
    return false;
  }

  public static List<string> GetLRange(string key, int start, int stop)
  {
    if (!_memoryCache.TryGetValue(key, out List<string>? existingValues) || existingValues == null)
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
    if (_memoryCache.TryGetValue(key, out List<string>? existingValues) && existingValues != null)
    {
      return existingValues.Count;
    }

    return 0;
  }

  public static string? LPop(string key)
  {
    if (_memoryCache.TryGetValue(key, out List<string>? existingValues) && existingValues != null)
    {
      if (existingValues.Count > 0)
      {
        string value = existingValues[0];
        existingValues.RemoveAt(0);
        return value;
      }
    }

    return null;
  }

  public static void Remove(string key)
  {
    _memoryCache.Remove(key);
  }
}