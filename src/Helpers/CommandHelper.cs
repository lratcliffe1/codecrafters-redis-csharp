using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Helpers;

public static class CommandHepler
{
  public static string? ReadBulkOrSimple(RespValue value)
  {
    if (value.Type == RespType.BulkString)
    {
      return value.StringValue;
    }

    if (value.Type == RespType.SimpleString)
    {
      return value.StringValue;
    }

    return null;
  }

  public static string FormatBulk(string value)
  {
    return $"${value.Length}\r\n{value}\r\n";
  }

  public static string BuildError(string value)
  {
    return $"-ERR {value}\r\n";
  }
}