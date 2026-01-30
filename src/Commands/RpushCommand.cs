namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class RpushCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count < 3)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'rpush'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);
    string? val = CommandHepler.ReadBulkOrSimple(args[2]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'set'");
    }
    if (string.IsNullOrEmpty(val))
    {
      return CommandHepler.BuildError("invalid value for 'set'");
    }

    int count = Cache.Append(key, val);
    return $":{count}\r\n";
  }
}