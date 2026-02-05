namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XAddCommand
{
  public static string Process(List<RespValue> args)
  {
    if (!HasValidArgs(args))
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xadd'");
    }

    if (!TryReadKeyAndId(args, out var key, out var idToken, out var error))
    {
      return error;
    }

    List<StreamEntry> entries = [];
    List<long>? lastEntryIdParts = null;
    if (Cache.TryGetValue(key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var existingEntries))
    {
      entries = existingEntries;
      if (entries.Count > 0)
      {
        var lastEntry = entries.Last();
        lastEntryIdParts = lastEntry.Id.Split("-").Select(long.Parse).ToList();
      }
    }

    if (!TryResolveEntryId(idToken, lastEntryIdParts, out var entryId, out error))
    {
      return error;
    }

    var fields = ReadFields(args);
    entries.Add(new StreamEntry(entryId, fields));
    Cache.Set(key, CacheValue.StreamEntries(entries));

    return CommandHepler.FormatBulk(entryId);
  }

  private static bool HasValidArgs(List<RespValue> args)
  {
    return args.Count >= 5 && args.Count % 2 == 1;
  }

  private static bool TryReadKeyAndId(List<RespValue> args, out string key, out string idToken, out string error)
  {
    key = CommandHepler.ReadBulkOrSimple(args[1]) ?? string.Empty;
    if (string.IsNullOrEmpty(key))
    {
      idToken = string.Empty;
      error = CommandHepler.BuildError("invalid key for 'xadd'");
      return false;
    }

    idToken = CommandHepler.ReadBulkOrSimple(args[2]) ?? string.Empty;
    if (string.IsNullOrEmpty(idToken))
    {
      error = CommandHepler.BuildError("invalid id for 'xadd'");
      return false;
    }

    if (idToken == "0-0")
    {
      error = CommandHepler.BuildError("The ID specified in XADD must be greater than 0-0");
      return false;
    }

    if (idToken == "*")
    {
      idToken = "*-*";
    }

    error = string.Empty;
    return true;
  }

  private static bool TryResolveEntryId(string idToken, List<long>? lastEntryIdParts, out string entryId, out string error)
  {
    var idParts = idToken.Split("-");
    var milliseconds = ParseMillisecondsToken(idParts[0]);

    var sequence = ParseSequenceToken(idParts[1], milliseconds, lastEntryIdParts);
    if (lastEntryIdParts == null && milliseconds == 0 && sequence == 0)
    {
      sequence++;
    }

    var newEntryIdParts = new List<long> { milliseconds, sequence };
    entryId = string.Join("-", newEntryIdParts);

    if (lastEntryIdParts != null && IsNotGreaterThanLastId(newEntryIdParts, lastEntryIdParts))
    {
      error = CommandHepler.BuildError("The ID specified in XADD is equal or smaller than the target stream top item");
      return false;
    }

    error = string.Empty;
    return true;
  }

  private static Dictionary<string, string> ReadFields(List<RespValue> args)
  {
    Dictionary<string, string> fields = [];
    for (var i = 3; i < args.Count; i += 2)
    {
      var field = CommandHepler.ReadBulkOrSimple(args[i]) ?? string.Empty;
      var value = CommandHepler.ReadBulkOrSimple(args[i + 1]) ?? string.Empty;
      fields.Add(field, value);
    }

    return fields;
  }

  private static long ParseMillisecondsToken(string token)
  {
    if (token == "*")
    {
      return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    return long.Parse(token);
  }

  private static long ParseSequenceToken(string token, long milliseconds, List<long>? lastEntryIdParts = null)
  {
    if (token == "*")
    {
      if (lastEntryIdParts == null || milliseconds > lastEntryIdParts[0])
        return 0;

      return lastEntryIdParts[1] + 1;
    }

    return long.Parse(token);
  }

  private static bool IsNotGreaterThanLastId(List<long> candidateId, List<long> lastEntryId)
  {
    if (lastEntryId[0] > candidateId[0])
      return true;

    return lastEntryId[0] == candidateId[0] && lastEntryId[1] >= candidateId[1];
  }
}
