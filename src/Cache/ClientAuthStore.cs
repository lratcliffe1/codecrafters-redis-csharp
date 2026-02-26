using System.Collections.Concurrent;

namespace codecrafters_redis.src.Cache;

public interface IClientAuthStore
{
  bool IsAuthenticated(long clientId);
  void Authenticate(long clientId, string username);
  bool TryGetUsername(long clientId, out string username);
  void Remove(long clientId);
}

public sealed class ClientAuthStore : IClientAuthStore
{
  private readonly ConcurrentDictionary<long, string> _authenticatedUsers = [];

  public bool IsAuthenticated(long clientId)
  {
    return _authenticatedUsers.ContainsKey(clientId);
  }

  public void Authenticate(long clientId, string username)
  {
    _authenticatedUsers[clientId] = username;
  }

  public bool TryGetUsername(long clientId, out string username)
  {
    return _authenticatedUsers.TryGetValue(clientId, out username!);
  }

  public void Remove(long clientId)
  {
    _authenticatedUsers.TryRemove(clientId, out _);
  }
}
