namespace codecrafters_redis.src;

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