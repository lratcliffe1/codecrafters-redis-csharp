using System.Globalization;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;
using RedisCache = codecrafters_redis.src.Cache.Cache;

namespace codecrafters_redis.src.Commands.Strings;

public static class IncrCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildErrorAsync("wrong number of arguments for 'incr'");
    }

    string key = args[1].ToString();

    if (!RedisCache.TryGetValue(key, out CacheValue? value))
    {
      RedisCache.Set(key, CacheValue.String("1"));
      return CommandHepler.FormatIntegerAsync(1);
    }

    if (value == null)
    {
      RedisCache.Set(key, CacheValue.String("1"));
      return CommandHepler.FormatIntegerAsync(1);
    }

    if (!value.TryGetString(out string currentRaw))
    {
      return CommandHepler.BuildErrorAsync("value is not an integer or out of range");
    }

    if (!long.TryParse(currentRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long current))
    {
      return CommandHepler.BuildErrorAsync("value is not an integer or out of range");
    }

    if (current == long.MaxValue)
    {
      return CommandHepler.BuildErrorAsync("value is not an integer or out of range");
    }

    long next = current + 1;
    RedisCache.Set(key, CacheValue.String(next.ToString(CultureInfo.InvariantCulture)));
    return CommandHepler.FormatIntegerAsync(next);
  }
}
