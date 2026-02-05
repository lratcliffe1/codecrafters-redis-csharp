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
      return SetWithoutExpiration(args);
    else if (args.Count == 5)
      return SetWithExpiration(args);

    return CommandHepler.BuildError("wrong number of arguments for 'set'");
  }

  private static string SetWithoutExpiration(List<RespValue> args)
  {
    string? key = CommandHepler.ReadBulkOrSimple(args[1]);
    string? val = CommandHepler.ReadBulkOrSimple(args[2]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError("invalid value for 'set'");
    }

    Cache.Set(key, CacheValue.String(val));
    return "+OK\r\n";
  }

  private static string SetWithExpiration(List<RespValue> args)
  {
    string? key = CommandHepler.ReadBulkOrSimple(args[1]);
    string? val = CommandHepler.ReadBulkOrSimple(args[2]);
    string? expirationType = CommandHepler.ReadBulkOrSimple(args[3]);

    bool isEx = string.Compare(expirationType, "ex", StringComparison.CurrentCultureIgnoreCase) == 0;
    bool isPx = string.Compare(expirationType, "px", StringComparison.CurrentCultureIgnoreCase) == 0;

    if (!isEx && !isPx)
    {
      return CommandHepler.BuildError("wrong expiration type for 'set'");
    }

    string? expirationRaw = CommandHepler.ReadBulkOrSimple(args[4]);
    if (!int.TryParse(expirationRaw, out int expiration))
    {
      return CommandHepler.BuildError("invalid expiration for 'set'");
    }
    int expirationInMilliseconds = isPx ? expiration : expiration * 1000;

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError("invalid value for 'set'");
    }

    Cache.Set(key, CacheValue.String(val), expirationInMilliseconds);
    return "+OK\r\n";
  }
}
