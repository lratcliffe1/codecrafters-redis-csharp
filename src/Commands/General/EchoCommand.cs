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

    string? payload = CommandHepler.ReadBulkOrSimple(args[1]);
    return payload == null ? "$-1\r\n" : CommandHepler.FormatBulk(payload);
  }
}
