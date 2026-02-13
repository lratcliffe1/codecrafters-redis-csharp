namespace codecrafters_redis.src.Commands.Lists;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class LPopCommand(ICacheStore cacheStore) : IRedisCommand
{
  public string Name => "LPOP";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count == 2)
    {
      args.Add(RespValue.Simple("1"));
    }

    if (args.Count != 3)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'lpop'");
    }

    string key = args[1].ToString();
    string popCountRaw = args[2].ToString();

    if (!int.TryParse(popCountRaw, out int popCount))
    {
      return CommandHelper.BuildErrorAsync("invalid count for 'lpop'");
    }

    List<string>? removed = cacheStore.LPop(key, popCount);

    if (removed == null)
    {
      return CommandHelper.FormatNullAsync(RespType.BulkString);
    }
    if (removed.Count == 1)
    {
      return CommandHelper.FormatBulkAsync(removed[0]);
    }

    return CommandHelper.FormatArrayAsync(removed);
  }
}
