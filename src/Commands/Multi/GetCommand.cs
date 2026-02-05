namespace codecrafters_redis.src.Commands.Multi;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class GetCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'get'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid of key for 'get'");
    }

    if (!Cache.TryGetValue(key, out CacheValue? val) || val == null)
    {
      return "$-1\r\n";
    }

    return CommandHepler.FormatValue(val);
  }
}
