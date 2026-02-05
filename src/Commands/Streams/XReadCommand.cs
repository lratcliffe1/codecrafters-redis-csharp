namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XReadCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 4)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xread'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[2]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'xread'");
    }

    string startValue = CommandHepler.ReadBulkOrSimple(args[3])!;

    if (Cache.TryGetValue(key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
    {
      var result = StreamRangeHelper.FilterEntries(entries, startValue, "+");
      return CommandHepler.FormatValue(CacheValue.Stream(key, result));
    }

    return "*-1\r\n";
  }
}
