using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Commands.General;
using codecrafters_redis.src.Commands.Lists;
using codecrafters_redis.src.Commands.Multi;
using codecrafters_redis.src.Commands.Replication;
using codecrafters_redis.src.Commands.Streams;
using codecrafters_redis.src.Commands.Strings;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Resp;

static class RespExecutor
{
  public static Task<string> ExecuteAsync(RespValue value, long clientId, int port, CancellationToken cancellationToken = default)
  {
    if (!TryReadCommand(value, out List<RespValue>? args, out string command))
    {
      return CommandHepler.BuildErrorAsync("expected array command");
    }

    if (ClientMultiCache.ContainsKey(clientId))
    {
      return ExecuteInMultiAsync(value, command, clientId, port, cancellationToken);
    }

    return ExecuteCommandAsync(args!, command, clientId, port, cancellationToken);
  }

  public static void OnClientDisconnected(long clientId)
  {
    ClientMultiCache.Remove(clientId);
  }

  private static bool TryReadCommand(RespValue value, out List<RespValue>? args, out string command)
  {
    command = string.Empty;
    args = value.ArrayValue;
    if (value.Type != RespType.Array || args == null || args.Count == 0)
    {
      return false;
    }

    command = args[0].ToString().ToUpperInvariant();
    return true;
  }

  private static Task<string> ExecuteInMultiAsync(
    RespValue originalValue,
    string command,
    long clientId,
    int port,
    CancellationToken cancellationToken)
  {
    return command switch
    {
      "EXEC" => ExecCommandAsync(clientId, port, cancellationToken),
      "MULTI" => CommandHepler.BuildErrorAsync("MULTI calls can not be nested"),
      "DISCARD" => DiscardHelper.ProcessAsync(clientId, cancellationToken),
      _ => MultiCommand.ProcessAsync(originalValue, clientId),
    };
  }

  private static Task<string> ExecuteCommandAsync(
    List<RespValue> args,
    string command,
    long clientId,
    int port,
    CancellationToken cancellationToken)
  {
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
      "MULTI" => MultiCommand.ProcessAsync(null, clientId),
      "EXEC" => ExecCommandAsync(clientId, port, cancellationToken),
      "DISCARD" => CommandHepler.BuildErrorAsync("DISCARD without MULTI"),
      "INFO" => InfoCommand.ProcessAsync(args, port),
      _ => CommandHepler.BuildErrorAsync($"unknown command: {command}"),
    };
  }

  private static Task<string> ExecCommandAsync(long clientId, int port, CancellationToken cancellationToken)
  {
    if (!ClientMultiCache.TryGetValue(clientId, out var commands) || commands == null)
    {
      return CommandHepler.BuildErrorAsync("EXEC without MULTI");
    }

    ClientMultiCache.Remove(clientId);
    return ExecCommand.ProcessAsync(commands, clientId, port, cancellationToken);
  }
}
