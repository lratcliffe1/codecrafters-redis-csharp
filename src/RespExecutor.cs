using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src;

static class RespExecutor
{
  public static string Execute(RespValue value)
  {
    if (value.Type != RespType.Array || value.ArrayValue == null || value.ArrayValue.Count == 0)
    {
      return CommandHepler.BuildError("expected array command");
    }

    string command = ReadCommandName(value.ArrayValue[0]);

    if (string.IsNullOrEmpty(command))
    {
      return CommandHepler.BuildError("invalid command");
    }

    command = command.ToUpperInvariant();

    if (command == "PING")
    {
      return PingCommand.Process(value);
    }

    if (command == "ECHO")
    {
      return EchoCommand.Process(value);
    }

    if (command == "SET")
    {
      return SetCommand.Process(value);
    }

    if (command == "GET")
    {
      return GetCommand.Process(value);
    }

    return CommandHepler.BuildError("unknown command'");
  }

  static string ReadCommandName(RespValue value)
  {
    string? payload = ReadBulkOrSimple(value);
    return payload ?? string.Empty;
  }

  static string? ReadBulkOrSimple(RespValue value)
  {
    if (value.Type == RespType.BulkString)
    {
      return value.StringValue;
    }

    if (value.Type == RespType.SimpleString)
    {
      return value.StringValue;
    }

    return null;
  }
}
