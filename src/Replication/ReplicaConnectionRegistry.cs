using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Replication;

public interface IReplicaConnectionRegistry
{
  void RegisterReplica(long clientId, NetworkStream stream);
  void UnregisterReplica(long clientId);
  Task PropagateAsync(string encodedCommand, CancellationToken cancellationToken);
  Task RequestAcknowledgementsAsync(CancellationToken cancellationToken);
  void UpdateReplicaAckOffset(long clientId, long offset);
  int CountReplicasAcknowledgingOffset(long offset);
  Task<int> WaitForReplicasAsync(int requiredReplicas, long targetOffset, TimeSpan timeout, CancellationToken cancellationToken);
  long GetReplicationOffset();
  bool TryMarkWaitOffset(long offset);
}

public sealed class ReplicaConnectionRegistry : IReplicaConnectionRegistry
{
  private readonly ConcurrentDictionary<long, ReplicaState> _replicaStreams = new();
  private readonly SemaphoreSlim _writeLock = new(1, 1);
  private readonly object _waitersLock = new();
  private readonly List<ReplicationWaiter> _waiters = [];
  private long _replicationOffset;
  private long _lastWaitOffset;

  public void RegisterReplica(long clientId, NetworkStream stream)
  {
    ReplicaState state = new(stream);
    state.SetAckOffset(GetReplicationOffset());
    _replicaStreams[clientId] = state;
    NotifyWaiters();
  }

  public void UnregisterReplica(long clientId)
  {
    _replicaStreams.TryRemove(clientId, out _);
  }

  public async Task PropagateAsync(string encodedCommand, CancellationToken cancellationToken)
  {
    if (string.IsNullOrEmpty(encodedCommand))
    {
      return;
    }

    byte[] payload = Encoding.UTF8.GetBytes(encodedCommand);
    Interlocked.Add(ref _replicationOffset, payload.Length);

    if (_replicaStreams.IsEmpty)
    {
      return;
    }

    await WriteToReplicasAsync(payload, cancellationToken);
  }

  public async Task RequestAcknowledgementsAsync(CancellationToken cancellationToken)
  {
    if (_replicaStreams.IsEmpty)
    {
      return;
    }

    string getAckCommand = CommandHelper.FormatArray(["REPLCONF", "GETACK", "*"]);
    byte[] payload = Encoding.UTF8.GetBytes(getAckCommand);
    await WriteToReplicasAsync(payload, cancellationToken);
  }

  public void UpdateReplicaAckOffset(long clientId, long offset)
  {
    if (_replicaStreams.TryGetValue(clientId, out ReplicaState? replica))
    {
      replica.SetAckOffset(offset);
      NotifyWaiters();
    }
  }

  public int CountReplicasAcknowledgingOffset(long offset)
  {
    int acknowledgedReplicas = 0;

    foreach (ReplicaState replica in _replicaStreams.Values)
    {
      if (replica.GetAckOffset() >= offset)
      {
        acknowledgedReplicas++;
      }
    }

    return acknowledgedReplicas;
  }

  public long GetReplicationOffset() => Interlocked.Read(ref _replicationOffset);

  public async Task<int> WaitForReplicasAsync(
    int requiredReplicas,
    long targetOffset,
    TimeSpan timeout,
    CancellationToken cancellationToken)
  {
    int acknowledgedReplicas = CountReplicasAcknowledgingOffset(targetOffset);
    if (acknowledgedReplicas >= requiredReplicas || timeout == TimeSpan.Zero)
    {
      return acknowledgedReplicas;
    }

    ReplicationWaiter waiter = new(targetOffset, requiredReplicas);
    RegisterWaiter(waiter);

    try
    {
      // Handle races where an ACK arrives between the initial check and waiter registration.
      acknowledgedReplicas = CountReplicasAcknowledgingOffset(targetOffset);
      if (acknowledgedReplicas >= requiredReplicas)
      {
        waiter.Signal.TrySetResult(acknowledgedReplicas);
      }

      try
      {
        _ = await waiter.Signal.Task.WaitAsync(timeout, cancellationToken);
      }
      catch (TimeoutException)
      {
        // Timeout is expected: we'll return the latest acknowledged count below.
      }

      return CountReplicasAcknowledgingOffset(targetOffset);
    }
    finally
    {
      UnregisterWaiter(waiter);
    }
  }

  public bool TryMarkWaitOffset(long offset)
  {
    while (true)
    {
      long currentOffset = Interlocked.Read(ref _lastWaitOffset);
      if (offset <= currentOffset)
      {
        return false;
      }

      if (Interlocked.CompareExchange(ref _lastWaitOffset, offset, currentOffset) == currentOffset)
      {
        return true;
      }
    }
  }

  private async Task WriteToReplicasAsync(byte[] payload, CancellationToken cancellationToken)
  {
    await _writeLock.WaitAsync(cancellationToken);
    try
    {
      List<long> disconnectedReplicaIds = [];

      foreach ((long replicaId, ReplicaState replica) in _replicaStreams.ToArray())
      {
        try
        {
          await replica.Stream.WriteAsync(payload, cancellationToken);
          await replica.Stream.FlushAsync(cancellationToken);
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

  private void RegisterWaiter(ReplicationWaiter waiter)
  {
    lock (_waitersLock)
    {
      _waiters.Add(waiter);
    }
  }

  private void UnregisterWaiter(ReplicationWaiter waiter)
  {
    lock (_waitersLock)
    {
      _waiters.Remove(waiter);
    }
  }

  private void NotifyWaiters()
  {
    List<ReplicationWaiter> waitersSnapshot;
    lock (_waitersLock)
    {
      if (_waiters.Count == 0)
      {
        return;
      }

      waitersSnapshot = _waiters.ToList();
    }

    foreach (ReplicationWaiter waiter in waitersSnapshot)
    {
      int acknowledgedReplicas = CountReplicasAcknowledgingOffset(waiter.TargetOffset);
      if (acknowledgedReplicas >= waiter.RequiredReplicas)
      {
        waiter.Signal.TrySetResult(acknowledgedReplicas);
      }
    }
  }
}
