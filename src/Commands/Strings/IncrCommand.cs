using System.Globalization;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Strings;

public class IncrCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "INCR";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'incr'");
    }

    string key = args[1].ToString();

    if (!cacheStore.TryGetValue(key, out CacheValue? value))
    {
      cacheStore.Set(key, CacheValue.String("1"));
      return CommandHelper.FormatIntegerAsync(1);
    }

    if (value == null)
    {
      cacheStore.Set(key, CacheValue.String("1"));
      return CommandHelper.FormatIntegerAsync(1);
    }

    if (!value.TryGetString(out string currentRaw))
    {
      return CommandHelper.BuildErrorAsync("value is not an integer or out of range");
    }

    if (!long.TryParse(currentRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long current))
    {
      return CommandHelper.BuildErrorAsync("value is not an integer or out of range");
    }

    if (current == long.MaxValue)
    {
      return CommandHelper.BuildErrorAsync("value is not an integer or out of range");
    }

    long next = current + 1;
    cacheStore.Set(key, CacheValue.String(next.ToString(CultureInfo.InvariantCulture)));
    return CommandHelper.FormatIntegerAsync(next);
  }
}
