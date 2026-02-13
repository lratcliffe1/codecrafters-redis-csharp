namespace codecrafters_redis.src.Commands.Strings;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;
using System;

public class SetCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "SET";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count == 3)
    {
      return Task.FromResult(SetWithoutExpiration(args, cacheStore));
    }
    else if (args.Count == 5)
    {
      return Task.FromResult(SetWithExpiration(args, cacheStore));
    }

    return CommandHelper.BuildErrorAsync("wrong number of arguments for 'set'");
  }

  private static string SetWithoutExpiration(List<RespValue> args, ICacheStore cacheStore)
  {
    string key = args[1].ToString();
    string val = args[2].ToString();

    cacheStore.Set(key, CacheValue.String(val));

    return CommandHelper.FormatSimple("OK");
  }

  private static string SetWithExpiration(List<RespValue> args, ICacheStore cacheStore)
  {
    string key = args[1].ToString();
    string val = args[2].ToString();
    string expirationType = args[3].ToString();
    string expirationRaw = args[4].ToString();

    if (string.IsNullOrEmpty(key))
    {
      return CommandHelper.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHelper.BuildError("invalid value for 'set'");
    }

    bool isEx = string.Compare(expirationType, "ex", StringComparison.CurrentCultureIgnoreCase) == 0;
    bool isPx = string.Compare(expirationType, "px", StringComparison.CurrentCultureIgnoreCase) == 0;

    if (!isEx && !isPx)
    {
      return CommandHelper.BuildError("wrong expiration type for 'set'");
    }

    if (!int.TryParse(expirationRaw, out int expiration))
    {
      return CommandHelper.BuildError("invalid expiration for 'set'");
    }

    int expirationInMilliseconds = isPx ? expiration : expiration * 1000;

    cacheStore.Set(key, CacheValue.String(val), expirationInMilliseconds);

    return CommandHelper.FormatSimple("OK");
  }
}
