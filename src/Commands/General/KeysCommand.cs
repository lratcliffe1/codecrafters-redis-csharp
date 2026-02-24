using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.General;

public class KeysCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "KEYS";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'keys'");
    }

    string pattern = args[1].ToString();

    if (pattern == "*")
    {
      return CommandHelper.FormatArrayAsync(cacheStore.GetKeys(""));
    }
    else
    {
      return CommandHelper.FormatArrayAsync(cacheStore.GetKeys(pattern));
    }
  }
}