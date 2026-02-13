namespace codecrafters_redis.src.Resp;

public interface IRespParser
{
  bool TryParse(string data, out RespValue value, out int consumedLength);
}

public class RespParser : IRespParser
{
  public bool TryParse(string data, out RespValue value, out int consumedLength)
  {
    int index = 0;

    try
    {
      value = ParseValue(data, ref index);
      consumedLength = index;
      return true;
    }
    catch (RespIncompleteException)
    {
      value = RespValue.Error("incomplete");
      consumedLength = 0;
      return false;
    }
  }

  static RespValue ParseValue(string data, ref int index)
  {
    if (index >= data.Length)
    {
      throw new RespIncompleteException("Empty RESP payload.");
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
      throw new RespIncompleteException("Bulk string length exceeds payload.");
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

    throw new RespIncompleteException("RESP line not terminated with CRLF.");
  }

  static void ConsumeCrlf(string data, ref int index)
  {
    if (index + 1 >= data.Length || data[index] != '\r' || data[index + 1] != '\n')
    {
      throw new RespIncompleteException("Expected CRLF.");
    }
    index += 2;
  }

  private sealed class RespIncompleteException(string message) : Exception(message);
}
