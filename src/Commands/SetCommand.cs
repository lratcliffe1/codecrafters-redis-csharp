namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;
using System;

public static class SetCommand
{
  public static string Process(RespValue value)
  {
    if (value.ArrayValue.Count == 3)
      return SetWithoutExpiration(value);
    else if (value.ArrayValue.Count == 5)
      return SetWithExpiration(value);

    return CommandHepler.BuildError("wrong number of arguments for 'set'");
  }

  private static string SetWithoutExpiration(RespValue value)
  {
    string? key = CommandHepler.ReadBulkOrSimple(value.ArrayValue[1]);
    string? val = CommandHepler.ReadBulkOrSimple(value.ArrayValue[2]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError("invalid value for 'set'");
    }

    Cache.Set(key, val);
    return "+OK\r\n";
  }

  private static string SetWithExpiration(RespValue value)
  {
    string? key = CommandHepler.ReadBulkOrSimple(value.ArrayValue[1]);
    string? val = CommandHepler.ReadBulkOrSimple(value.ArrayValue[2]);
    string? expirationType = CommandHepler.ReadBulkOrSimple(value.ArrayValue[3]);

    bool isEx = string.Compare(expirationType, "ex", StringComparison.CurrentCultureIgnoreCase) == 0;
    bool isPx = string.Compare(expirationType, "px", StringComparison.CurrentCultureIgnoreCase) == 0;

    if (!isEx && !isPx)
    {
      return CommandHepler.BuildError("wrong expiration type for 'set'");
    }

    string? expirationRaw = CommandHepler.ReadBulkOrSimple(value.ArrayValue[4]);
    int expiration = int.Parse(expirationRaw);
    int expirationInMilliseconds = isPx ? expiration : expiration * 1000;

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError("invalid value for 'set'");
    }

    Cache.Set(key, val, expirationInMilliseconds);
    return "+OK\r\n";
  }
}