namespace codecrafters_redis.src.Commands.Strings;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;
using System;

public static class SetCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count == 3)
    {
      return SetWithoutExpiration(args);
    }
    else if (args.Count == 5)
    {
      return SetWithExpiration(args);
    }

    return CommandHepler.BuildError("wrong number of arguments for 'set'");
  }

  private static string SetWithoutExpiration(List<RespValue> args)
  {
    string key = args[1].ToString();
    string val = args[2].ToString();

    Cache.Set(key, CacheValue.String(val));

    return CommandHepler.FormatSimple("OK");
  }

  private static string SetWithExpiration(List<RespValue> args)
  {
    string key = args[1].ToString();
    string val = args[2].ToString();
    string expirationType = args[3].ToString();
    string expirationRaw = args[4].ToString();

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError("invalid value for 'set'");
    }

    bool isEx = string.Compare(expirationType, "ex", StringComparison.CurrentCultureIgnoreCase) == 0;
    bool isPx = string.Compare(expirationType, "px", StringComparison.CurrentCultureIgnoreCase) == 0;

    if (!isEx && !isPx)
    {
      return CommandHepler.BuildError("wrong expiration type for 'set'");
    }

    if (!int.TryParse(expirationRaw, out int expiration))
    {
      return CommandHepler.BuildError("invalid expiration for 'set'");
    }

    int expirationInMilliseconds = isPx ? expiration : expiration * 1000;

    Cache.Set(key, CacheValue.String(val), expirationInMilliseconds);

    return CommandHepler.FormatSimple("OK");
  }
}
