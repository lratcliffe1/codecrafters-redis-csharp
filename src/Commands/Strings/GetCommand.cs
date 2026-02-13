namespace codecrafters_redis.src.Commands.Strings;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class GetCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "GET";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'get'");
    }

    string key = args[1].ToString();

    if (!cacheStore.TryGetValue(key, out CacheValue? val) || val == null)
    {
      return CommandHelper.FormatNullAsync(RespType.SimpleString);
    }

    return CommandHelper.FormatValueAsync(val);
  }
}
