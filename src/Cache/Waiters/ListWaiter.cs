namespace codecrafters_redis.src.Cache.Waiters;

public sealed class ListWaiter
{
  public TaskCompletionSource<bool> Signal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
