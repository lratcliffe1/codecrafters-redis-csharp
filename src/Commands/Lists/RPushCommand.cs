namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class RPushCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "RPUSH";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count < 3)
    {
      return CommandHelper.BuildErrorAsync($"wrong number of arguments for rpush");
    }

    string? key = args[1].ToString();
    string? val = args[2].ToString();

    List<string> vals = args[2..]
      .Select(value => value.ToString())
      .Where(value => !string.IsNullOrEmpty(value))
      .Select(value => value!)
      .ToList();

    int count = cacheStore.Append(key, vals);

    return CommandHelper.FormatIntegerAsync(count);
  }
}
