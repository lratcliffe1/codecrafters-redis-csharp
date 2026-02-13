namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class XAddCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "XADD";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (!HasValidArgs(args))
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'xadd'");
    }

    var key = args[1].ToString();
    var idToken = args[2].ToString();

    if (idToken == "0-0")
    {
      return CommandHelper.BuildErrorAsync("The ID specified in XADD must be greater than 0-0");
    }

    if (idToken == "*")
    {
      idToken = "*-*";
    }

    List<StreamEntry> entries = [];
    List<long>? lastEntryIdParts = null;
    if (cacheStore.TryGetValue(key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var existingEntries))
    {
      entries = existingEntries;
      if (entries.Count > 0)
      {
        var lastEntry = entries.Last();
        lastEntryIdParts = lastEntry.Id.Split("-").Select(long.Parse).ToList();
      }
    }

    if (!TryResolveEntryId(idToken, lastEntryIdParts, out var entryId, out var error))
    {
      return Task.FromResult(error);
    }

    var fields = ReadFields(args);
    entries.Add(new StreamEntry(entryId, fields));
    cacheStore.Set(key, CacheValue.StreamEntries(entries));

    return CommandHelper.FormatBulkAsync(entryId);
  }

  private static bool HasValidArgs(List<RespValue> args)
  {
    return args.Count >= 5 && args.Count % 2 == 1;
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
      error = CommandHelper.BuildError("The ID specified in XADD is equal or smaller than the target stream top item");
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
      var field = args[i].ToString();
      var value = args[i + 1].ToString();
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
