namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class EchoCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildErrorAsync("wrong number of arguments for 'echo'");
    }

    return CommandHepler.FormatBulkAsync(args[1].ToString());
  }
}
