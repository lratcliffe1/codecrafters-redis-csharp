namespace codecrafters_redis.src.Replication;

public sealed class ReplicationWaiter(long targetOffset, int requiredReplicas)
{
  public long TargetOffset { get; } = targetOffset;
  public int RequiredReplicas { get; } = requiredReplicas;
  public TaskCompletionSource<int> Signal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}