namespace codecrafters_redis.src.Commands.SortedLists;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class ZAddCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "ZADD";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'zadd'");
    }

    string key = args[1].ToString();
    string score = args[2].ToString();
    string member = args[3].ToString();

    if (!double.TryParse(score, out double scoreValue))
    {
      return CommandHelper.BuildErrorAsync("invalid score for 'zadd'");
    }

    cacheStore.ZAdd(key, scoreValue, member);

    return CommandHelper.FormatIntegerAsync(1);
  }
}