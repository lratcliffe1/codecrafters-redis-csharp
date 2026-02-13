namespace codecrafters_redis.src.Commands.Multi;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class ExecCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    return CommandHepler.BuildErrorAsync("EXEC without MULTI");
  }
}