using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public static class MultiCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    return CommandHepler.FormatSimpleAsync("OK");
  }
}