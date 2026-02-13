namespace codecrafters_redis.src.Hosting;

public interface IClientIdAllocator
{
  long Next();
}

public sealed class ClientIdAllocator : IClientIdAllocator
{
  private long _nextClientId;

  public long Next()
  {
    return Interlocked.Increment(ref _nextClientId);
  }
}
