namespace codecrafters_redis;

enum RespType
{
  SimpleString,
  Error,
  Integer,
  BulkString,
  Array
}

sealed class RespValue
{
  public RespType Type { get; }
  public string? StringValue { get; }
  public long? IntegerValue { get; }
  public List<RespValue>? ArrayValue { get; }

  private RespValue(RespType type, string? stringValue, long? integerValue, List<RespValue>? arrayValue)
  {
    Type = type;
    StringValue = stringValue;
    IntegerValue = integerValue;
    ArrayValue = arrayValue;
  }

  public static RespValue Simple(string value) => new(RespType.SimpleString, value, null, null);
  public static RespValue Error(string value) => new(RespType.Error, value, null, null);
  public static RespValue Integer(long value) => new(RespType.Integer, null, value, null);
  public static RespValue Bulk(string? value) => new(RespType.BulkString, value, null, null);
  public static RespValue Array(List<RespValue>? value) => new(RespType.Array, null, null, value);

  public override string ToString()
  {
    return Type switch
    {
      RespType.SimpleString => $"+{StringValue}",
      RespType.Error => $"-{StringValue}",
      RespType.Integer => $":{IntegerValue}",
      RespType.BulkString => StringValue == null ? "$-1" : $"${StringValue.Length}:{StringValue}",
      RespType.Array => ArrayValue == null ? "*-1" : $"*{ArrayValue.Count}",
      _ => Type.ToString()
    };
  }
}

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
