namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class BLPopCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 3)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'blpop'");
    }

    string key = args[1].ToString();
    string expirationRaw = args[2].ToString();

    if (!double.TryParse(expirationRaw, out double expiration) || expiration < 0)
    {
      return CommandHepler.BuildError("invalid expiration for 'blpop'");
    }

    List<string>? removed = Cache.BLPop(key, expiration);

    if (removed == null)
    {
      return CommandHepler.FormatNull(RespType.Array);
    }

    return CommandHepler.FormatArray(removed.Prepend(key).ToList());
  }
}
