namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XAddCommand
{
  public static string Process(List<RespValue> args)
  {
    if (!HasValidArity(args))
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xadd'");
    }

    if (!TryReadKeyAndId(args, out var key, out var id, out var error))
    {
      return error;
    }

    if (!ValidateNewEntryId(key, id, out error))
    {
      return error;
    }

    var keyValuePairs = ReadKeyValuePairs(args);
    Cache.Set(key, CacheValue.Stream([new StreamEntry(id, keyValuePairs)]));

    return CommandHepler.FormatBulk(id);
  }

  private static bool HasValidArity(List<RespValue> args)
  {
    return args.Count >= 5 && args.Count % 2 == 1;
  }

  private static bool TryReadKeyAndId(List<RespValue> args, out string key, out string id, out string error)
  {
    key = CommandHepler.ReadBulkOrSimple(args[1]) ?? string.Empty;
    if (string.IsNullOrEmpty(key))
    {
      error = CommandHepler.BuildError("invalid of key for 'xadd'");
      id = string.Empty;
      return false;
    }

    id = CommandHepler.ReadBulkOrSimple(args[2]) ?? string.Empty;
    if (string.IsNullOrEmpty(id))
    {
      error = CommandHepler.BuildError("invalid of key for 'xadd'");
      return false;
    }

    if (id == "0-0")
    {
      error = CommandHepler.BuildError("The ID specified in XADD must be greater than 0-0");
      return false;
    }

    error = string.Empty;
    return true;
  }

  private static bool ValidateNewEntryId(string key, string id, out string error)
  {
    error = string.Empty;
    if (!Cache.TryGetValue(key, out var cacheValue) || cacheValue == null)
    {
      return true;
    }

    if (!cacheValue.TryGetStream(out var oldValue) || oldValue.Count == 0)
    {
      return true;
    }

    var lastValue = oldValue.Last();
    var lastValueIdSplit = lastValue.Id.Split("-").Select(int.Parse).ToList();
    var newValueIdSplit = id.Split("-").Select(int.Parse).ToList();

    if (lastValueIdSplit[0] > newValueIdSplit[0])
    {
      error = CommandHepler.BuildError("The ID specified in XADD is equal or smaller than the target stream top item");
      return false;
    }

    if (lastValueIdSplit[0] == newValueIdSplit[0] && lastValueIdSplit[1] >= newValueIdSplit[1])
    {
      error = CommandHepler.BuildError("The ID specified in XADD is equal or smaller than the target stream top item");
      return false;
    }

    return true;
  }

  private static Dictionary<string, string> ReadKeyValuePairs(List<RespValue> args)
  {
    Dictionary<string, string> keyValuePairs = [];
    for (var i = 3; i < args.Count; i += 2)
    {
      var field = CommandHepler.ReadBulkOrSimple(args[i]) ?? string.Empty;
      var value = CommandHepler.ReadBulkOrSimple(args[i + 1]) ?? string.Empty;
      keyValuePairs.Add(field, value);
    }

    return keyValuePairs;
  }
}