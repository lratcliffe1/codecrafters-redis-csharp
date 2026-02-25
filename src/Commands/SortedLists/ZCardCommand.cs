namespace codecrafters_redis.src.Commands.SortedLists;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class ZCardCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "ZCARD";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'zcard'");
    }

    string key = args[1].ToString();
    int count = cacheStore.ZCard(key);
    return CommandHelper.FormatIntegerAsync(count);
  }
}