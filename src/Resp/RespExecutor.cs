using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Resp;

static class RespExecutor
{
  public static string Execute(RespValue value)
  {
    if (value.Type != RespType.Array || value.ArrayValue == null || value.ArrayValue.Count == 0)
    {
      return CommandHepler.BuildError("expected array command");
    }

    List<RespValue> args = value.ArrayValue;
    string command = ReadCommandName(args[0]);

    if (string.IsNullOrEmpty(command))
    {
      return CommandHepler.BuildError("invalid command");
    }

    command = command.ToUpperInvariant();

    if (command == "PING")
    {
      return PingCommand.Process(args);
    }

    if (command == "ECHO")
    {
      return EchoCommand.Process(args);
    }

    if (command == "SET")
    {
      return SetCommand.Process(args);
    }

    if (command == "GET")
    {
      return GetCommand.Process(args);
    }

    if (command == "RPUSH")
    {
      return PushCommand.Process(args, PushDirection.Right, "rpush");
    }

    if (command == "LPUSH")
    {
      return PushCommand.Process(args, PushDirection.Left, "lpush");
    }

    if (command == "LRANGE")
    {
      return LRangeCommand.Process(args);
    }

    if (command == "LLEN")
    {
      return LLenCommand.Process(args);
    }

    if (command == "LPOP")
    {
      return LPopCommand.Process(args);
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
