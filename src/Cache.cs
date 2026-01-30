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

  public static List<string> GetLrange(string key, int start, int stop)
  {
    if (start > stop)
      return [];

    if (!_memoryCache.TryGetValue(key, out List<string>? existingValues) || existingValues == null)
      return [];

    if (existingValues.Count < start)
      return [];

    if (stop > existingValues.Count)
      stop = existingValues.Count;

    return existingValues[start..stop];
  }

  public static void Remove(string key)
  {
    _memoryCache.Remove(key);
  }
}