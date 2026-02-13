using System.Collections.Concurrent;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Cache;

public static class ClientMultiCache
{
  private static readonly ConcurrentDictionary<long, List<RespValue>> _cache = [];

  public static bool ContainsKey(long clientId)
  {
    return _cache.ContainsKey(clientId);
  }

  public static void Set(long clientId, RespValue? command)
  {
    if (_cache.TryGetValue(clientId, out var commands))
    {
      if (command != null)
      {
        commands.Add(command);
      }
    }
    else
    {
      _cache[clientId] = command != null ? [command] : [];
    }
  }

  public static bool TryGetValue(long clientId, out List<RespValue>? commands)
  {
    return _cache.TryGetValue(clientId, out commands);
  }

  public static void Remove(long clientId)
  {
    _cache.TryRemove(clientId, out _);
  }
}