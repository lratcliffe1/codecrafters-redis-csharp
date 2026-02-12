namespace codecrafters_redis.src.Commands.Multi;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class TypeCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildErrorAsync("wrong number of arguments for 'type'");
    }

    string key = args[1].ToString();

    if (!Cache.TryGetValue(key, out CacheValue? val) || val == null)
    {
      return CommandHepler.FormatSimpleAsync("none");
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

    return CommandHepler.FormatSimpleAsync(type);
  }
}
