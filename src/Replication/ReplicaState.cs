using System.Net.Sockets;

namespace codecrafters_redis.src.Replication;

public sealed class ReplicaState(NetworkStream stream)
{
  private long _ackOffset;
  public NetworkStream Stream { get; } = stream;
  public long GetAckOffset() => Interlocked.Read(ref _ackOffset);
  public void SetAckOffset(long offset) => Interlocked.Exchange(ref _ackOffset, offset);
}