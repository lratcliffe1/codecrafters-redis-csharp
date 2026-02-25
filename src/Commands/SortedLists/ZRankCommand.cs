namespace codecrafters_redis.src.Commands.SortedLists;

using codecrafters_redis.src.Commands;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;
using codecrafters_redis.src.Helpers;

public class ZRankCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "ZRANK";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'zrank'");
    }

    string key = args[1].ToString();
    string member = args[2].ToString();

    int rank = cacheStore.ZRank(key, member);
    return rank == -1 ? CommandHelper.FormatNullAsync(RespType.BulkString) : CommandHelper.FormatIntegerAsync(rank);
  }
}