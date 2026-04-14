using System.Collections.Concurrent;

namespace codecrafters_redis.src.Cache;

public interface IClientWatchStore
{
  void Add(long clientId, IEnumerable<string> keys);
  bool TryGetValue(long clientId, out HashSet<string>? keys);
  void Remove(long clientId);
}

public sealed class ClientWatchStore : IClientWatchStore
{
  private readonly ConcurrentDictionary<long, HashSet<string>> _cache = [];

  public void Add(long clientId, IEnumerable<string> keys)
  {
    if (_cache.TryGetValue(clientId, out HashSet<string>? watchedKeys))
    {
      foreach (string key in keys)
      {
        watchedKeys.Add(key);
      }

      return;
    }

    _cache[clientId] = keys.ToHashSet(StringComparer.Ordinal);
  }

  public bool TryGetValue(long clientId, out HashSet<string>? keys)
  {
    return _cache.TryGetValue(clientId, out keys);
  }

  public void Remove(long clientId)
  {
    _cache.TryRemove(clientId, out _);
  }
}
