namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;

public static class PingCommand
{
  public static string Process(RespValue value)
  {
    if (value.ArrayValue.Count == 1)
    {
      return "+PONG\r\n";
    }

    if (value.ArrayValue.Count == 2)
    {
      string? payload = CommandHepler.ReadBulkOrSimple(value.ArrayValue[1]);
      return payload == null ? "$-1\r\n" : CommandHepler.FormatBulk(payload);
    }

    return CommandHepler.BuildError("wrong number of arguments for 'ping'");
  }
}