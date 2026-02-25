namespace codecrafters_redis.src.Cache;

public interface IPubSubStore
{
  bool ContainsKey(long clientId);
  int Subscribe(long clientId, string channel);
}

public class PubSubStore : IPubSubStore
{
  private readonly Dictionary<long, List<string>> _subscriptions = [];

  public bool ContainsKey(long clientId)
  {
    return _subscriptions.ContainsKey(clientId);
  }

  public int Subscribe(long clientId, string channel)
  {
    if (!_subscriptions.TryGetValue(clientId, out var channels))
    {
      channels = [];
      _subscriptions[clientId] = channels;
    }
    channels.Add(channel);
    return channels.Count;
  }
}