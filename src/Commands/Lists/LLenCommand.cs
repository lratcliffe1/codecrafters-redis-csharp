namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class LLenCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildErrorAsync("wrong number of arguments for 'llen'");
    }

    string key = args[1].ToString();

    int count = Cache.GetLLen(key);

    return CommandHepler.FormatIntegerAsync(count);
  }
}
