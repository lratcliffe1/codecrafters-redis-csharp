namespace codecrafters_redis.src.Cache;

public interface IPubSubStore
{
  bool ContainsKey(long clientId);
  int Subscribe(long clientId, string channel);
  int Publish(string channel, string message);
}

public class PubSubStore : IPubSubStore
{
  private readonly Dictionary<long, List<string>> _subscriptions = [];
  private readonly Dictionary<string, List<long>> _channels = [];

  public bool ContainsKey(long clientId)
  {
    return _subscriptions.ContainsKey(clientId);
  }

  public int Subscribe(long clientId, string channel)
  {
    if (!_channels.TryGetValue(channel, out var clients))
    {
      clients = [];
      _channels[channel] = clients;
    }

    clients.Add(clientId);

    if (!_subscriptions.TryGetValue(clientId, out var channels))
    {
      channels = [];
      _subscriptions[clientId] = channels;
    }

    channels.Add(channel);
    
    return channels.Count;
  }

  public int Publish(string channel, string message)
  {
    if (!_channels.TryGetValue(channel, out var clients))
    {
      return 0;
    }
    return clients.Count;
  }
}