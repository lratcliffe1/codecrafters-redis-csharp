namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XReadCommand
{
  public static async Task<string> ProcessAsync(List<RespValue> args, CancellationToken cancellationToken = default)
  {
    if (!TryParseArgs(args, out var streams, out var isBlocking, out var expirationMilliseconds))
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xread'");
    }

    streams = ResolveDollarIds(streams);

    var streamResponses = ReadStreamResponses(streams);
    if (streamResponses.Count > 0)
    {
      return CommandHepler.FormatArrayOfResp(streamResponses);
    }

    if (!isBlocking)
    {
      return CommandHepler.FormatNull(RespType.Array);
    }

    bool signaled = await Cache.WaitForStreamEntriesAsync(streams, expirationMilliseconds, cancellationToken);
    if (!signaled)
    {
      return CommandHepler.FormatNull(RespType.Array);
    }

    streamResponses = ReadStreamResponses(streams);

    if (streamResponses.Count == 0)
    {
      return CommandHepler.FormatNull(RespType.Array);
    }

    return CommandHepler.FormatArrayOfResp(streamResponses);
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

  private static List<string> ReadStreamResponses(IReadOnlyList<(string key, string id)> streams)
  {
    List<string> streamResponses = [];

    foreach (var stream in streams)
    {
      if (Cache.TryGetValue(stream.key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
      {
        var result = entries.Where(entry => StreamIdHelper.IsGreaterThan(entry.Id, stream.id)).ToList();
        if (result.Count > 0)
        {
          var entriesResp = CommandHepler.FormatStreamEntries(result);
          var streamResp = CommandHepler.FormatArrayOfResp([CommandHepler.FormatBulk(stream.key), entriesResp]);
          streamResponses.Add(streamResp);
        }
      }
    }

    return streamResponses;
  }

  private static List<(string key, string id)> ResolveDollarIds(IReadOnlyList<(string key, string id)> streams)
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
      if (Cache.TryGetValue(stream.key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
      {
        currentId = entries.Last().Id;
      }

      resolved.Add((stream.key, currentId));
    }

    return resolved;
  }

}
