using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class UnwatchCommand(IClientWatchStore clientWatchStore) : IRedisCommand
{
  public string Name => "UNWATCH";

  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 1)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'unwatch'");
    }

    clientWatchStore.Remove(context.ClientId);
    return CommandHelper.FormatSimpleAsync("OK");
  }
}
