namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class PingCommand : IRedisCommand
{
  public string Name => "PING";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count == 1)
    {
      return CommandHelper.FormatSimpleAsync("PONG");
    }

    if (args.Count == 2)
    {
      return CommandHelper.FormatBulkAsync(args[1].ToString());
    }

    return CommandHelper.BuildErrorAsync("wrong number of arguments for 'ping'");
  }
}
