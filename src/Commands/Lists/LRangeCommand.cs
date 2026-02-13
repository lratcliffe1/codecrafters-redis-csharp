namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class LRangeCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "LRANGE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 4)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'lrange'");
    }

    string key = args[1].ToString();

    if (!int.TryParse(args[2].ToString(), out int start))
    {
      return CommandHelper.BuildErrorAsync("invalid start range for 'lrange'");
    }
    if (!int.TryParse(args[3].ToString(), out int stop))
    {
      return CommandHelper.BuildErrorAsync("invalid value for 'lrange'");
    }

    List<string> values = cacheStore.GetLRange(key, start, stop);

    return CommandHelper.FormatArrayAsync(values);
  }
}
