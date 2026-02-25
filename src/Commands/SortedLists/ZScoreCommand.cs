namespace codecrafters_redis.src.Commands.SortedLists;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class ZScoreCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "ZSCORE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'zscore'");
    }

    string key = args[1].ToString();
    string member = args[2].ToString();

    double? score = cacheStore.ZScore(key, member);
    return score == null ? CommandHelper.FormatNullAsync(RespType.BulkString) : CommandHelper.FormatBulkAsync(score.Value.ToString());
  }
}