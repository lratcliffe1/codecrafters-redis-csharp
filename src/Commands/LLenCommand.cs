namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class LLenCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'llen'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'llen'");
    }

    int count = Cache.GetLLen(key);

    return $":{count}\r\n";
  }
}