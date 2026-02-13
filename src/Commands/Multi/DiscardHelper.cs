using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Commands.Multi;

public static class DiscardHelper
{
  public static Task<string> ProcessAsync(long clientId, CancellationToken cancellationToken)
  {
    ClientMultiCache.Remove(clientId);
    
    return CommandHepler.FormatSimpleAsync("OK");
  }
}