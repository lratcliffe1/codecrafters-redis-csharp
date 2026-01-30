using Microsoft.Extensions.Caching.Memory;

namespace codecrafters_redis.src;

public static class Cache
{
  private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

  public static void Set(string key, string value)
  {
    _cache.Set(key, value);
  }

  public static void Set(string key, string value, int expirationInMilliseconds)
  {
    _cache.Set(key, value, TimeSpan.FromMilliseconds(expirationInMilliseconds));
  }

  public static int Append(string key, string value)
  {
    if (_cache.TryGetValue(key, out List<string> val))
    {
      _cache.Set(key, val.Append(value));
      return val.Count + 1;
    }
    _cache.Set(key, new List<string> { value });
    return 1;
  }

  public static bool TryGetValue(string key, out string val)
  {
    return _cache.TryGetValue(key, out val);
  }

  public static void Remove(string key)
  {
    _cache.Remove(key);
  }
}