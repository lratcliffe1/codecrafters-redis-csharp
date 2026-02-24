using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Hosting;

public interface IReplicaConnectionRegistry
{
  void RegisterReplica(long clientId, NetworkStream stream);
  void UnregisterReplica(long clientId);
  Task PropagateAsync(string encodedCommand, CancellationToken cancellationToken);
}

public sealed class ReplicaConnectionRegistry : IReplicaConnectionRegistry
{
  private readonly ConcurrentDictionary<long, NetworkStream> _replicaStreams = new();
  private readonly SemaphoreSlim _writeLock = new(1, 1);

  public void RegisterReplica(long clientId, NetworkStream stream)
  {
    _replicaStreams[clientId] = stream;
  }

  public void UnregisterReplica(long clientId)
  {
    _replicaStreams.TryRemove(clientId, out _);
  }

  public async Task PropagateAsync(string encodedCommand, CancellationToken cancellationToken)
  {
    if (_replicaStreams.IsEmpty || string.IsNullOrEmpty(encodedCommand))
    {
      return;
    }

    byte[] payload = Encoding.UTF8.GetBytes(encodedCommand);

    await _writeLock.WaitAsync(cancellationToken);
    try
    {
      List<long> disconnectedReplicaIds = [];

      foreach ((long replicaId, NetworkStream stream) in _replicaStreams.ToArray())
      {
        try
        {
          await stream.WriteAsync(payload, cancellationToken);
          await stream.FlushAsync(cancellationToken);
        }
        catch (IOException)
        {
          disconnectedReplicaIds.Add(replicaId);
        }
        catch (ObjectDisposedException)
        {
          disconnectedReplicaIds.Add(replicaId);
        }
      }

      foreach (long replicaId in disconnectedReplicaIds)
      {
        UnregisterReplica(replicaId);
      }
    }
    finally
    {
      _writeLock.Release();
    }
  }
}
