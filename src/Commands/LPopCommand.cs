namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class LPopCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 2)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'lpop'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'lpop'");
    }

    string? removed = Cache.LPop(key);

    return removed == null ? "$-1\r\n" : CommandHepler.FormatBulk(removed);
  }
}