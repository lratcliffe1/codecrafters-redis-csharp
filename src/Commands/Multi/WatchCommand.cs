using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class WatchCommand : IRedisCommand
{
  public string Name => "WATCH";

  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'watch'");
    }

    return CommandHelper.FormatSimpleAsync("OK");
  }
}
