namespace codecrafters_redis.src.Commands.Multi;

using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class TypeCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'type'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'type'");
    }

    if (!Cache.TryGetValue(key, out CacheValue? val) || val == null)
    {
      return CommandHepler.FormatSimple("none");
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

    return CommandHepler.FormatSimple(type);
  }
}
