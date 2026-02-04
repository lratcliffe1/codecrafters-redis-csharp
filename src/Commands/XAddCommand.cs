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
      return CommandHepler.BuildError("invalid of key for 'get'");
    }

    string? id = CommandHepler.ReadBulkOrSimple(args[2]);

    if (string.IsNullOrEmpty(id))
    {
      return CommandHepler.BuildError("invalid of key for 'get'");
    }

    Dictionary<string, string> keyValuePairs = [];

    for (var i = 1; i < args.Count; i += 2)
    {
      keyValuePairs.Add(CommandHepler.ReadBulkOrSimple(args[i])!, CommandHepler.ReadBulkOrSimple(args[i + 1])!);
    }

    Cache.Set(key, CacheValue.Stream([new StreamEntry(id, keyValuePairs)]));

    return CommandHepler.FormatBulk(id);
  }
}