namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class EchoCommand : IRedisCommand
{
  public string Name => "ECHO";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'echo'");
    }

    return CommandHelper.FormatBulkAsync(args[1].ToString());
  }
}
