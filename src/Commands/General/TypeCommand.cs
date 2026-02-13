namespace codecrafters_redis.src.Commands.General;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public class TypeCommand(ICacheStore cacheStore) : IRedisCommand
{
  public ICacheStore cacheStore = cacheStore;

  public string Name => "TYPE";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count != 2)
    {
      return CommandHelper.BuildErrorAsync("wrong number of arguments for 'type'");
    }

    string key = args[1].ToString();

    if (!cacheStore.TryGetValue(key, out CacheValue? val) || val == null)
    {
      return CommandHelper.FormatSimpleAsync("none");
    }

    string type = val.Type switch
    {
      CacheValueType.String => "string",
      CacheValueType.List => "list",
      CacheValueType.Set => "set",
      CacheValueType.ZSet => "zset",
      CacheValueType.Hash => "hash",
      CacheValueType.Stream => "stream",
      CacheValueType.StreamEntries => "stream",
      CacheValueType.VectorSet => "vectorset",
      _ => "none",
    };

    return CommandHelper.FormatSimpleAsync(type);
  }
}
