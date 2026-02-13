using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public static class MultiCommand
{
  public static Task<string> ProcessAsync(RespValue? value, long clientId)
  {
    ClientMultiCache.Set(clientId, value);

    return CommandHepler.FormatSimpleAsync(value == null ? "OK" : "QUEUED");
  }
}