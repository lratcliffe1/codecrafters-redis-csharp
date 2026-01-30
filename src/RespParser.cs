namespace codecrafters_redis.src;

static class RespParser
{
  public static RespValue Parse(string data)
  {
    int index = 0;
    return ParseValue(data, ref index);
  }

  static RespValue ParseValue(string data, ref int index)
  {
    if (index >= data.Length)
    {
      throw new InvalidOperationException("Empty RESP payload.");
    }

    char prefix = data[index++];
    return prefix switch
    {
      '+' => RespValue.Simple(ReadLine(data, ref index)),
      '-' => RespValue.Error(ReadLine(data, ref index)),
      ':' => RespValue.Integer(long.Parse(ReadLine(data, ref index))),
      '$' => ParseBulkString(data, ref index),
      '*' => ParseArray(data, ref index),
      _ => throw new InvalidOperationException($"Unknown RESP type: '{prefix}'.")
    };
  }

  static RespValue ParseBulkString(string data, ref int index)
  {
    int length = int.Parse(ReadLine(data, ref index));
    if (length == -1)
    {
      return RespValue.Bulk(null);
    }

    if (index + length > data.Length)
    {
      throw new InvalidOperationException("Bulk string length exceeds payload.");
    }

    string value = data.Substring(index, length);
    index += length;
    ConsumeCrlf(data, ref index);
    return RespValue.Bulk(value);
  }

  static RespValue ParseArray(string data, ref int index)
  {
    int count = int.Parse(ReadLine(data, ref index));
    if (count == -1)
    {
      return RespValue.Array(null);
    }

    List<RespValue> values = new(count);
    for (int i = 0; i < count; i++)
    {
      values.Add(ParseValue(data, ref index));
    }

    return RespValue.Array(values);
  }

  static string ReadLine(string data, ref int index)
  {
    int start = index;
    while (index + 1 < data.Length)
    {
      if (data[index] == '\r' && data[index + 1] == '\n')
      {
        string line = data[start..index];
        index += 2;
        return line;
      }
      index++;
    }

    throw new InvalidOperationException("RESP line not terminated with CRLF.");
  }

  static void ConsumeCrlf(string data, ref int index)
  {
    if (index + 1 >= data.Length || data[index] != '\r' || data[index + 1] != '\n')
    {
      throw new InvalidOperationException("Expected CRLF.");
    }
    index += 2;
  }
}
