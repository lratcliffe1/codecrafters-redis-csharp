using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class WatchCommand(IClientWatchStore clientWatchStore, ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "WATCH";

  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'watch'");
    }

    Dictionary<string, long> keyVersions = args
      .Skip(1)
      .Select(arg => arg.ToString())
      .Distinct(StringComparer.Ordinal)
      .ToDictionary(key => key, cacheStore.GetKeyVersion, StringComparer.Ordinal);

    clientWatchStore.WatchKeys(context.ClientId, keyVersions);

    return CommandHelper.FormatSimpleAsync("OK");
  }
}
