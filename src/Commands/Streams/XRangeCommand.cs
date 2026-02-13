namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class XRangeCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "XRANGE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 4)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'xrange'");
    }

    string key = args[1].ToString();
    string startValue = args[2].ToString();
    string endValue = args[3].ToString();

    List<StreamEntry> result = [];

    if (cacheStore.TryGetValue(key, out var cacheValue) && cacheValue != null && cacheValue.TryGetStream(out var entries) && entries.Count > 0)
    {
      result = StreamRangeHelper.FilterEntries(entries, startValue, endValue);
    }

    return CommandHelper.FormatStreamEntriesAsync(result);
  }
}
