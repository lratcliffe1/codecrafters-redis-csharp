namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XAddCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count < 5 || args.Count % 2 != 1)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xadd'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid of key for 'xadd'");
    }

    string? id = CommandHepler.ReadBulkOrSimple(args[2]);

    if (string.IsNullOrEmpty(id))
    {
      return CommandHepler.BuildError("invalid of key for 'xadd'");
    }

    if (id == "0-0")
    {
      return CommandHepler.BuildError("The ID specified in XADD must be greater than 0-0");
    }

    var testing = id.Split("-");

    if (Cache.TryGetValue(key, out var cacheValue) && cacheValue != null)
    {
      if (cacheValue.TryGetStream(out var oldValue))
      {
        var lastValue = oldValue.Last();

        var lastValueIdSplit = lastValue.Id.Split("-").Select(long.Parse).ToList();
        var a = Parse(testing[0]);
        var b = Parse(testing[1], a, lastValueIdSplit);

        var newValueIdSplit = new List<long> { a, b };
        id = string.Join("-", newValueIdSplit);

        if (lastValueIdSplit[0] > newValueIdSplit[0])
          return CommandHepler.BuildError("The ID specified in XADD is equal or smaller than the target stream top item");

        if (lastValueIdSplit[0] == newValueIdSplit[0] && lastValueIdSplit[1] >= newValueIdSplit[1])
          return CommandHepler.BuildError("The ID specified in XADD is equal or smaller than the target stream top item");
      }
    }
    else if (testing[1] == "*")
    {
      if (testing[0] == "0")
        id = string.Join("-", testing[0], 1);
      else
        id = string.Join("-", testing[0], 0);
    }

    Dictionary<string, string> keyValuePairs = [];

    for (var i = 1; i < args.Count; i += 2)
    {
      keyValuePairs.Add(CommandHepler.ReadBulkOrSimple(args[i])!, CommandHepler.ReadBulkOrSimple(args[i + 1])!);
    }

    Cache.Set(key, CacheValue.Stream([new StreamEntry(id, keyValuePairs)]));

    return CommandHepler.FormatBulk(id);
  }

  private static long Parse(string value)
  {
    if (value == "*")
    {
      return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    return long.Parse(value);
  }

  private static long Parse(string value, long previousValue, List<long> lastValueIdSplit)
  {
    if (value == "*")
    {
      if (previousValue > lastValueIdSplit[0])
        return 0;

      return lastValueIdSplit[1] + 1;
    }

    return long.Parse(value);
  }
}