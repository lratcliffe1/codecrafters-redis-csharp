using System.Collections.Concurrent;

namespace codecrafters_redis.src.Cache;

public interface IClientWatchStore
{
  void WatchKeys(long clientId, IReadOnlyDictionary<string, long> versionsByKey);
  bool TryGetValue(long clientId, out Dictionary<string, long>? versionsByKey);
  void Remove(long clientId);
}

public sealed class ClientWatchStore : IClientWatchStore
{
  private readonly ConcurrentDictionary<long, Dictionary<string, long>> _cache = [];

  public void WatchKeys(long clientId, IReadOnlyDictionary<string, long> versionsByKey)
  {
    if (_cache.TryGetValue(clientId, out Dictionary<string, long>? watchedKeys))
    {
      foreach (KeyValuePair<string, long> entry in versionsByKey)
      {
        watchedKeys[entry.Key] = entry.Value;
      }

      return;
    }

    _cache[clientId] = new Dictionary<string, long>(versionsByKey, StringComparer.Ordinal);
  }

  public bool TryGetValue(long clientId, out Dictionary<string, long>? versionsByKey)
  {
    return _cache.TryGetValue(clientId, out versionsByKey);
  }

  public void Remove(long clientId)
  {
    _cache.TryRemove(clientId, out _);
  }
}
