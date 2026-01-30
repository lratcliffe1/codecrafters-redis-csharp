namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class LRangeCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 4)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'lrange'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'lrange'");
    }
    if (!int.TryParse(CommandHepler.ReadBulkOrSimple(args[2]), out int start))
    {
      return CommandHepler.BuildError("invalid start range for 'lrange'");
    }
    if (!int.TryParse(CommandHepler.ReadBulkOrSimple(args[3]), out int stop))
    {
      return CommandHepler.BuildError("invalid value for 'lrange'");
    }

    List<string> values = Cache.GetLRange(key, start, stop);

    return CommandHepler.FormatArray(values);
  }
}