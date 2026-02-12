namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class LRangeCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count != 4)
    {
      return CommandHepler.BuildErrorAsync("wrong number of arguments for 'lrange'");
    }

    string key = args[1].ToString();

    if (!int.TryParse(args[2].ToString(), out int start))
    {
      return CommandHepler.BuildErrorAsync("invalid start range for 'lrange'");
    }
    if (!int.TryParse(args[3].ToString(), out int stop))
    {
      return CommandHepler.BuildErrorAsync("invalid value for 'lrange'");
    }

    List<string> values = Cache.GetLRange(key, start, stop);

    return CommandHepler.FormatArrayAsync(values);
  }
}
