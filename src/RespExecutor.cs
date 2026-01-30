namespace codecrafters_redis;

static class RespExecutor
{
  public static string Execute(RespValue value)
  {
    if (value.Type != RespType.Array || value.ArrayValue == null || value.ArrayValue.Count == 0)
    {
      return "-ERR expected array command\r\n";
    }

    string command = ReadCommandName(value.ArrayValue[0]);
    if (string.IsNullOrEmpty(command))
    {
      return "-ERR invalid command\r\n";
    }

    command = command.ToUpperInvariant();
    if (command == "PING")
    {
      if (value.ArrayValue.Count == 1)
      {
        return "+PONG\r\n";
      }

      if (value.ArrayValue.Count == 2)
      {
        string? payload = ReadBulkOrSimple(value.ArrayValue[1]);
        return payload == null ? "$-1\r\n" : FormatBulk(payload);
      }

      return "-ERR wrong number of arguments for 'ping'\r\n";
    }

    if (command == "ECHO")
    {
      if (value.ArrayValue.Count != 2)
      {
        return "-ERR wrong number of arguments for 'echo'\r\n";
      }

      string? payload = ReadBulkOrSimple(value.ArrayValue[1]);
      return payload == null ? "$-1\r\n" : FormatBulk(payload);
    }

    return "-ERR unknown command\r\n";
  }

  static string ReadCommandName(RespValue value)
  {
    string? payload = ReadBulkOrSimple(value);
    return payload ?? string.Empty;
  }

  static string? ReadBulkOrSimple(RespValue value)
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

  static string FormatBulk(string value)
  {
    return $"${value.Length}\r\n{value}\r\n";
  }
}
