namespace codecrafters_redis.src.Bootstrap;

public sealed record ServerOptions(int Port, int? ReplicaOfPort)
{
  public bool IsReplica => ReplicaOfPort.HasValue;
}
