namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class LLenCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "LLEN";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'llen'");
    }

    string key = args[1].ToString();

    int count = cacheStore.GetLLen(key);

    return CommandHelper.FormatIntegerAsync(count);
  }
}
