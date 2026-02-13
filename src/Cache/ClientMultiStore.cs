using System.Collections.Concurrent;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Cache;

public interface IClientMultiStore
{
  bool ContainsKey(long clientId);
  void Set(long clientId, RespValue? command);
  bool TryGetValue(long clientId, out List<RespValue>? commands);
  void Remove(long clientId);
}

public sealed class ClientMultiStore : IClientMultiStore
{
  private readonly ConcurrentDictionary<long, List<RespValue>> _cache = [];

  public bool ContainsKey(long clientId)
  {
    return _cache.ContainsKey(clientId);
  }

  public void Set(long clientId, RespValue? command)
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

  public bool TryGetValue(long clientId, out List<RespValue>? commands)
  {
    return _cache.TryGetValue(clientId, out commands);
  }

  public void Remove(long clientId)
  {
    _cache.TryRemove(clientId, out _);
  }
}
