using codecrafters_redis.src.Commands.General;
using codecrafters_redis.src.Commands.Lists;
using codecrafters_redis.src.Commands.Multi;
using codecrafters_redis.src.Commands.Streams;
using codecrafters_redis.src.Commands.Strings;
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

    return command switch
    {
      "PING" => PingCommand.Process(args),
      "ECHO" => EchoCommand.Process(args),
      "SET" => SetCommand.Process(args),
      "GET" => GetCommand.Process(args),
      "RPUSH" => PushCommand.Process(args, PushDirection.Right, "rpush"),
      "LPUSH" => PushCommand.Process(args, PushDirection.Left, "lpush"),
      "LRANGE" => LRangeCommand.Process(args),
      "LLEN" => LLenCommand.Process(args),
      "LPOP" => LPopCommand.Process(args),
      "BLPOP" => BLPopCommand.Process(args),
      "TYPE" => TypeCommand.Process(args),
      "XADD" => XAddCommand.Process(args),
      "XRANGE" => XRangeCommand.Process(args),
      "XREAD" => XReadCommand.Process(args),
      _ => CommandHepler.BuildError("unknown command"),
    };
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
