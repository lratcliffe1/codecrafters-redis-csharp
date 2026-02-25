namespace codecrafters_redis.src.Cache;

public interface IPubSubStore
{
  bool ContainsKey(long clientId);
  int Subscribe(string channel);
}

public class PubSubStore : IPubSubStore
{
  public bool ContainsKey(long clientId)
  {
    return false;
  }

  public int Subscribe(string channel)
  {
    return 1;
  }
}