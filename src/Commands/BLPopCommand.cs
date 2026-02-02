namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

public static class BLPopCommand
{
  public static string Process(List<RespValue> args)
  {
    if (args.Count != 3)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'blpop'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(args[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid key for 'blpop'");
    }

    string? expirationRaw = CommandHepler.ReadBulkOrSimple(args[2]);
    if (!int.TryParse(expirationRaw, out int expiration) || expiration < 0)
    {
      return CommandHepler.BuildError("invalid expiration for 'blpop'");
    }

    List<string>? removed = Cache.BLPop(key, expiration);

    if (removed == null)
    {
      return "$-1\r\n";
    }

    return CommandHepler.FormatArray(removed.Prepend(key).ToList());
  }
}