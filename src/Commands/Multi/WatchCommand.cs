using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class WatchCommand(IClientWatchStore clientWatchStore) : IRedisCommand
{
  public string Name => "WATCH";

  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'watch'");
    }

    List<string> keys = args
      .Skip(1)
      .Select(arg => arg.ToString())
      .ToList();

    clientWatchStore.Add(context.ClientId, keys);

    return CommandHelper.FormatSimpleAsync("OK");
  }
}
