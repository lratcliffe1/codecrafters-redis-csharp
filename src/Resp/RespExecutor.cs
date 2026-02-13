using codecrafters_redis.src.Commands.General;
using codecrafters_redis.src.Commands.Lists;
using codecrafters_redis.src.Commands.Multi;
using codecrafters_redis.src.Commands.Streams;
using codecrafters_redis.src.Commands.Strings;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Resp;

static class RespExecutor
{
  public static Task<string> ExecuteAsync(RespValue value, CancellationToken cancellationToken = default)
  {
    if (value.Type != RespType.Array || value.ArrayValue == null || value.ArrayValue.Count == 0)
    {
      return CommandHepler.BuildErrorAsync("expected array command");
    }

    List<RespValue> args = value.ArrayValue;
    string command = args[0].ToString();

    command = command.ToUpperInvariant();

    return command switch
    {
      "PING" => PingCommand.ProcessAsync(args),
      "ECHO" => EchoCommand.ProcessAsync(args),
      "SET" => SetCommand.ProcessAsync(args),
      "INCR" => IncrCommand.ProcessAsync(args),
      "GET" => GetCommand.ProcessAsync(args),
      "RPUSH" => PushCommand.ProcessAsync(args, PushDirection.Right, "rpush"),
      "LPUSH" => PushCommand.ProcessAsync(args, PushDirection.Left, "lpush"),
      "LRANGE" => LRangeCommand.ProcessAsync(args),
      "LLEN" => LLenCommand.ProcessAsync(args),
      "LPOP" => LPopCommand.ProcessAsync(args),
      "BLPOP" => BLPopCommand.ProcessAsync(args, cancellationToken),
      "TYPE" => TypeCommand.ProcessAsync(args),
      "XADD" => XAddCommand.ProcessAsync(args),
      "XRANGE" => XRangeCommand.ProcessAsync(args),
      "XREAD" => XReadCommand.ProcessAsync(args, cancellationToken),
      "MULTI" => MultiCommand.ProcessAsync(args),
      _ => CommandHepler.BuildErrorAsync($"unknown command: {command}"),
    };
  }
}
