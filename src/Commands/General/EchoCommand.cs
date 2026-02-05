namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class EchoCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'echo'");
    }

    return CommandHepler.FormatBulk(args[1].ToString());
  }
}
