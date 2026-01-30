namespace codecrafters_redis.src;

static class RespExecutor
{
  public static string Execute(RespValue value, Dictionary<string, string> DATABASE)
  {
    if (value.Type != RespType.Array || value.ArrayValue == null || value.ArrayValue.Count == 0)
    {
      return BuildError("expected array command");
    }

    string command = ReadCommandName(value.ArrayValue[0]);
    if (string.IsNullOrEmpty(command))
    {
      return BuildError("invalid command");
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
      return BuildError("wrong number of arguments for 'ping'");
    }

    if (command == "ECHO")
    {
      if (value.ArrayValue.Count != 2)
      {
        return BuildError("wrong number of arguments for 'echo'");
      }

      string? payload = ReadBulkOrSimple(value.ArrayValue[1]);
      return payload == null ? "$-1\r\n" : FormatBulk(payload);
    }

    if (command == "SET")
    {
      if (value.ArrayValue.Count != 3)
      {
        return BuildError("wrong number of arguments for 'set'");
      }

      string? key = ReadBulkOrSimple(value.ArrayValue[1]);
      string? val = ReadBulkOrSimple(value.ArrayValue[2]);

      if (string.IsNullOrEmpty(key))
      {
        return BuildError("invalid key for 'set'");
      }
      if (string.IsNullOrEmpty(val))
      {
        return BuildError("invalid value for 'set'");
      }

      DATABASE[key] = val;
      return "+OK\r\n";
    }

    if (command == "GET")
    {
      if (value.ArrayValue.Count != 2)
      {
        return BuildError("wrong number of arguments for 'get'");
      }

      string? key = ReadBulkOrSimple(value.ArrayValue[1]);

      if (string.IsNullOrEmpty(key))
      {
        return BuildError("invalid of key for 'get'");
      }

      if (!DATABASE.TryGetValue(key, out var val))
      {
        return "$-1\r\n";
      }

      return FormatBulk(val);
    }

    return BuildError("unknown command'");
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

  static string BuildError(string value)
  {
    return $"-ERR {value}\r\n";
  }
}
