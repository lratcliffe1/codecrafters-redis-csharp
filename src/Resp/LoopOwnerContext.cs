namespace codecrafters_redis.src.Resp;

internal static class LoopOwnerContext
{
  private static readonly AsyncLocal<int> _ownerDepth = new();

  public static bool IsOnOwnerLane => _ownerDepth.Value > 0;

  public static async Task<T> RunOnOwnerLaneAsync<T>(Func<Task<T>> callback)
  {
    _ownerDepth.Value++;
    try
    {
      return await callback();
    }
    finally
    {
      _ownerDepth.Value--;
    }
  }
}
