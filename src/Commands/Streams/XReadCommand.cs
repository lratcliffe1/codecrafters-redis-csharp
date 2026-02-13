namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class XReadCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "XREAD";
  public async Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (!TryParseArgs(args, out var streams, out var isBlocking, out var expirationMilliseconds))
    {
      return CommandHelper.BuildError("wrong number of arguments for 'xread'");
    }

    streams = ResolveDollarIds(streams, cacheStore);

    var streamResponses = ReadStreamResponses(streams, cacheStore);
    if (streamResponses.Count > 0)
    {
      return CommandHelper.FormatArrayOfResp(streamResponses);
    }

    if (!isBlocking)
    {
      return CommandHelper.FormatNull(RespType.Array);
    }

    bool signaled = await cacheStore.WaitForStreamEntriesAsync(streams, expirationMilliseconds, context.CancellationToken);
    if (!signaled)
    {
      return CommandHelper.FormatNull(RespType.Array);
    }

    streamResponses = ReadStreamResponses(streams, cacheStore);

    if (streamResponses.Count == 0)
    {
      return CommandHelper.FormatNull(RespType.Array);
    }

    return CommandHelper.FormatArrayOfResp(streamResponses);
  }

  private static bool TryParseArgs(
    List<RespValue> args,
    out List<(string key, string id)> streams,
    out bool isBlocking,
    out double expirationMilliseconds)
  {
    streams = [];
    isBlocking = false;
    expirationMilliseconds = 0;

    if (args.Count < 4)
    {
      return false;
    }

    var index = 1;
    if (index < args.Count && string.Equals(args[index].ToString(), "block", StringComparison.InvariantCultureIgnoreCase))
    {
      if (index + 1 >= args.Count || !double.TryParse(args[index + 1].ToString(), out expirationMilliseconds) || expirationMilliseconds < 0)
      {
        return false;
      }

      isBlocking = true;
      index += 2;
    }

    if (index >= args.Count || !string.Equals(args[index].ToString(), "streams", StringComparison.InvariantCultureIgnoreCase))
    {
      return false;
    }

    index++;
    var remaining = args.Count - index;
    if (remaining < 2 || remaining % 2 != 0)
    {
      return false;
    }

    var streamCount = remaining / 2;
    for (int i = 0; i < streamCount; i++)
    {
      streams.Add((args[index + i].ToString(), args[index + streamCount + i].ToString()));
    }

    return true;
  }

  private static List<string> ReadStreamResponses(IReadOnlyList<(string key, string id)> streams, ICacheStore cacheStore)
  {
    List<string> streamResponses = [];

    foreach (var stream in streams)
    {
      if (cacheStore.TryGetValue(stream.key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
      {
        var result = entries.Where(entry => StreamIdHelper.IsGreaterThan(entry.Id, stream.id)).ToList();
        if (result.Count > 0)
        {
          var entriesResp = CommandHelper.FormatStreamEntries(result);
          var streamResp = CommandHelper.FormatArrayOfResp([CommandHelper.FormatBulk(stream.key), entriesResp]);
          streamResponses.Add(streamResp);
        }
      }
    }

    return streamResponses;
  }

  private static List<(string key, string id)> ResolveDollarIds(IReadOnlyList<(string key, string id)> streams, ICacheStore cacheStore)
  {
    List<(string key, string id)> resolved = [];

    foreach (var stream in streams)
    {
      if (stream.id != "$")
      {
        resolved.Add(stream);
        continue;
      }

      string currentId = "0-0";
      if (cacheStore.TryGetValue(stream.key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
      {
        currentId = entries.Last().Id;
      }

      resolved.Add((stream.key, currentId));
    }

    return resolved;
  }

}
