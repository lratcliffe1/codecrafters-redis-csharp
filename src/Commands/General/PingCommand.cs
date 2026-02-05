namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class PingCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count == 1)
    {
      return "+PONG\r\n";
    }

    if (args.Count == 2)
    {
      string? payload = CommandHepler.ReadBulkOrSimple(args[1]);
      return payload == null ? "$-1\r\n" : CommandHepler.FormatBulk(payload);
    }

    return CommandHepler.BuildError("wrong number of arguments for 'ping'");
  }
}
