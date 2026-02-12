namespace codecrafters_redis.src.Commands.Multi;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class GetCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildErrorAsync("wrong number of arguments for 'get'");
    }

    string key = args[1].ToString();

    if (!Cache.TryGetValue(key, out CacheValue? val) || val == null)
    {
      return CommandHepler.FormatNullAsync(RespType.SimpleString);
    }

    return CommandHepler.FormatValueAsync(val);
  }
}
