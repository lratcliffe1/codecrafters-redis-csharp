namespace codecrafters_redis.src.Commands;

using codecrafters_redis.src.Helpers;

public static class GetCommand
{
  public static string Process(RespValue value)
  {
    if (value.ArrayValue.Count != 2)
    {
      return CommandHepler.BuildError("wrong number of arguments for 'get'");
    }

    string? key = CommandHepler.ReadBulkOrSimple(value.ArrayValue[1]);

    if (string.IsNullOrEmpty(key))
    {
      return CommandHepler.BuildError("invalid of key for 'get'");
    }

    if (!Cache.TryGetValue(key, out string val))
    {
      return "$-1\r\n";
    }

    return CommandHepler.FormatBulk(val);
  }
}