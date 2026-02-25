namespace codecrafters_redis.src.Commands.SortedLists;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class ZRangeCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "ZRANGE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 4)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'zrange'");
    }

    string key = args[1].ToString();
    string start = args[2].ToString();
    string end = args[3].ToString();

    if (!int.TryParse(start, out int startValue) || !int.TryParse(end, out int endValue))
    {
      return CommandHelper.BuildErrorAsync("invalid start or end for 'zrange'");
    }

    List<ZSetEntry> values = cacheStore.ZRange(key, startValue, endValue);
    return CommandHelper.FormatArrayAsync(values.Select(entry => entry.Member).ToList());
  }
}