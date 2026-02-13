namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class BLPopCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "BLPOP";
  public async Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 3)
    {
      return CommandHelper.BuildError("wrong number of arguments for 'blpop'");
    }

    string key = args[1].ToString();
    string expirationRaw = args[2].ToString();

    if (!double.TryParse(expirationRaw, out double expiration) || expiration < 0)
    {
      return CommandHelper.BuildError("invalid expiration for 'blpop'");
    }

    List<string>? removed = cacheStore.LPop(key, 1);
    if (removed == null)
    {
      bool signaled = await cacheStore.WaitForListEntriesAsync(key, expiration, context.CancellationToken);
      if (!signaled)
      {
        return CommandHelper.FormatNull(RespType.Array);
      }

      removed = cacheStore.LPop(key, 1);
    }

    if (removed == null)
    {
      return CommandHelper.FormatNull(RespType.Array);
    }

    return CommandHelper.FormatArray(removed.Prepend(key).ToList());
  }
}
