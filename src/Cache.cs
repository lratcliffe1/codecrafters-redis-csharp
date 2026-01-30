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

  public static int Append(string key, string value)
  {
    if (_memoryCache.TryGetValue(key, out List<string>? existingValues) && existingValues != null)
    {
      existingValues.Add(value);
      return existingValues.Count;
    }
    _memoryCache.Set(key, new List<string> { value });
    return 1;
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

  public static void Remove(string key)
  {
    _memoryCache.Remove(key);
  }
}