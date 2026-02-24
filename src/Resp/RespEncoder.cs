using System.Globalization;
using System.Text;

namespace codecrafters_redis.src.Resp;

public static class RespEncoder
{
  public static string Encode(RespValue value)
  {
    return value.Type switch
    {
      RespType.SimpleString => $"+{value.StringValue ?? string.Empty}\r\n",
      RespType.Error => $"-{value.StringValue ?? string.Empty}\r\n",
      RespType.Integer => $":{(value.IntegerValue ?? 0).ToString(CultureInfo.InvariantCulture)}\r\n",
      RespType.BulkString => EncodeBulkString(value.StringValue),
      RespType.Array => EncodeArray(value.ArrayValue),
      _ => throw new InvalidOperationException($"Unsupported RESP type: {value.Type}"),
    };
  }

  private static string EncodeBulkString(string? value)
  {
    if (value == null)
    {
      return "$-1\r\n";
    }

    int length = Encoding.UTF8.GetByteCount(value);
    return $"${length}\r\n{value}\r\n";
  }

  private static string EncodeArray(IReadOnlyList<RespValue>? values)
  {
    if (values == null)
    {
      return "*-1\r\n";
    }

    StringBuilder builder = new();
    builder.Append('*');
    builder.Append(values.Count.ToString(CultureInfo.InvariantCulture));
    builder.Append("\r\n");

    foreach (RespValue value in values)
    {
      builder.Append(Encode(value));
    }

    return builder.ToString();
  }
}
