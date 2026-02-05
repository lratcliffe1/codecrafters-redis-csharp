namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class XReadCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count < 4 || args.Count % 2 != 0)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'xread'");
    }

    int streamCount = (args.Count - 2) / 2;
    List<(string key, string id)> streams = [];

    for (int i = 0; i < streamCount; i++)
    {
      streams.Add((CommandHepler.ReadBulkOrSimple(args[2 + i])!, CommandHepler.ReadBulkOrSimple(args[2 + streamCount + i])!));
    }

    List<string> streamResponses = [];

    foreach (var stream in streams)
    {
      if (Cache.TryGetValue(stream.key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
      {
        var result = StreamRangeHelper.FilterEntries(entries, stream.id, "+");
        if (result.Count > 0)
        {
          string entriesResp = CommandHepler.FormatStreamEntries(result);
          string streamResp = CommandHepler.FormatArrayOfResp([CommandHepler.FormatBulk(stream.key), entriesResp]);
          streamResponses.Add(streamResp);
        }
      }
    }

    if (streamResponses.Count == 0)
    {
      return "*-1\r\n";
    }

    return CommandHepler.FormatArrayOfResp(streamResponses);
  }
}
