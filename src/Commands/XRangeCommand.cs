namespace codecrafters_redis.src.Commands;

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

    List<StreamEntry> result = [];

    string startValue = CommandHepler.ReadBulkOrSimple(args[2])!;
    string endValue = CommandHepler.ReadBulkOrSimple(args[3])!;
    List<long> splitStart = (startValue == "-" ? "0-0" : startValue).Split("-").Select(long.Parse).ToList();
    List<long> splitEnd = (endValue == "+" ? $"{long.MaxValue}-{long.MaxValue}" : endValue).Split("-").Select(long.Parse).ToList();

    if (Cache.TryGetValue(key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
    {
      foreach (var entry in entries)
      {
        var splitId = entry.Id.Split("-").Select(long.Parse).ToList();

        if (splitId[0] < splitStart[0])
          continue;

        if (splitId[0] > splitEnd[0])
          continue;

        if (splitId[0] == splitStart[0] && splitId[1] < splitStart[1])
          continue;

        if (splitId[0] == splitEnd[0] && splitId[1] > splitEnd[1])
          continue;

        result.Add(entry);
      }
    }

    return CommandHepler.FormatStreamEntries(result);
  }
}