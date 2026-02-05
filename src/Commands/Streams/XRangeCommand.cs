namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XRangeCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 4)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xrange'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'xrange'");
    }

    string startValue = CommandHepler.ReadBulkOrSimple(args[2])!;
    string endValue = CommandHepler.ReadBulkOrSimple(args[3])!;
    List<StreamEntry> result = [];

    if (Cache.TryGetValue(key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
    {
      result = StreamRangeHelper.FilterEntries(entries, startValue, endValue);
    }

    return CommandHepler.FormatStreamEntries(result);
  }
}
