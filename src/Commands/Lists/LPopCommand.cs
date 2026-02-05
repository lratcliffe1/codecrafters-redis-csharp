namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class LPopCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count == 2)
    {
      args.Add(RespValue.Simple("1"));
    }

    if (args.Count != 3)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'lpop'");
    }

    string key = args[1].ToString();
    string popCountRaw = args[2].ToString();

    if (!int.TryParse(popCountRaw, out int popCount))
    {
      return CommandHepler.BuildError("invalid count for 'lpop'");
    }

    List<string>? removed = Cache.LPop(key, popCount);

    if (removed == null)
    {
      return CommandHepler.FormatNull(RespType.BulkString);
    }
    if (removed.Count == 1)
    {
      return CommandHepler.FormatBulk(removed[0]);
    }

    return CommandHepler.FormatArray(removed);
  }
}
