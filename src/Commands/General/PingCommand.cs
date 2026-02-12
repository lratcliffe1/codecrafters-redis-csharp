namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class PingCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count == 1)
    {
      return CommandHepler.FormatSimpleAsync("PONG");
    }

    if (args.Count == 2)
    {
      return CommandHepler.FormatBulkAsync(args[1].ToString());
    }

    return CommandHepler.BuildErrorAsync("wrong number of arguments for 'ping'");
  }
}
