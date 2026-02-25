using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Hosting;

namespace codecrafters_redis.src.Cache;

public interface IPubSubStore
{
  bool ContainsKey(long clientId);
  int Subscribe(long clientId, string channel);
  Task<int> PublishAsync(string channel, string message, CancellationToken cancellationToken);
  void Remove(long clientId);
}

public sealed class PubSubStore(IClientConnectionRegistry clientConnectionRegistry) : IPubSubStore
{
  private readonly Dictionary<long, HashSet<string>> _subscriptions = [];
  private readonly Dictionary<string, HashSet<long>> _channels = [];

  public bool ContainsKey(long clientId)
  {
    return _subscriptions.ContainsKey(clientId);
  }

  public int Subscribe(long clientId, string channel)
  {
    if (!_subscriptions.TryGetValue(clientId, out HashSet<string>? channels))
    {
      channels = [];
      _subscriptions[clientId] = channels;
    }

    if (channels.Add(channel))
    {
      if (!_channels.TryGetValue(channel, out var clients))
      {
        clients = [];
        _channels[channel] = clients;
      }

      clients.Add(clientId);
    }

    return channels.Count;
  }

  public async Task<int> PublishAsync(string channel, string message, CancellationToken cancellationToken)
  {
    if (!_channels.TryGetValue(channel, out HashSet<long>? clients) || clients.Count == 0)
    {
      return 0;
    }

    long[] subscribers = [.. clients];
    string payload = CommandHelper.FormatArray(["message", channel, message]);
    int deliveredCount = 0;
    List<long> disconnectedClients = [];

    foreach (long clientId in subscribers)
    {
      if (await clientConnectionRegistry.TryWriteAsync(clientId, payload, cancellationToken))
      {
        deliveredCount++;
      }
      else
      {
        disconnectedClients.Add(clientId);
      }
    }

    foreach (long clientId in disconnectedClients)
    {
      Remove(clientId);
    }

    return deliveredCount;
  }

  public void Remove(long clientId)
  {
    if (!_subscriptions.Remove(clientId, out HashSet<string>? channels))
    {
      return;
    }

    foreach (string channel in channels)
    {
      if (!_channels.TryGetValue(channel, out HashSet<long>? clients))
      {
        continue;
      }

      clients.Remove(clientId);
      if (clients.Count == 0)
      {
        _channels.Remove(channel);
      }
    }
  }
}
