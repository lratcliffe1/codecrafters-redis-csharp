using System.Text;
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

  public static string FormatArray(IReadOnlyList<string> values)
  {
    StringBuilder builder = new();
    builder.Append($"*{values.Count}\r\n");

    foreach (string value in values)
    {
      builder.Append(FormatBulk(value));
    }

    return builder.ToString();
  }

  public static string BuildError(string value)
  {
    return $"-ERR {value}\r\n";
  }
}