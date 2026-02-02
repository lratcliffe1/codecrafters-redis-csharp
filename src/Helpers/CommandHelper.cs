using System.Globalization;
using System.Text;
using codecrafters_redis.src.Cache;
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

  public static string FormatSimple(string value)
  {
    return $"+{value}\r\n";
  }

  public static string FormatArray(IReadOnlyList<string> values)
  {
    List<string> encoded = values.Select(FormatBulk).ToList();
    return FormatArrayOfResp(encoded);
  }

  public static string FormatArrayOfResp(IReadOnlyList<string> respValues)
  {
    StringBuilder builder = new();
    builder.Append($"*{respValues.Count}\r\n");

    foreach (string value in respValues)
    {
      builder.Append(value);
    }

    return builder.ToString();
  }

  public static string FormatValue(CacheValue value)
  {
    switch (value.Type)
    {
      case CacheValueType.String:
        if (value.TryGetString(out string stringValue))
        {
          return FormatBulk(stringValue);
        }
        break;
      case CacheValueType.List:
        if (value.TryGetList(out List<string> listValue))
        {
          return FormatArray(listValue);
        }
        break;
      case CacheValueType.Set:
        if (value.TryGetSet(out HashSet<string> setValue))
        {
          return FormatArray(setValue.OrderBy(item => item).ToList());
        }
        break;
      case CacheValueType.ZSet:
        if (value.TryGetZSet(out List<ZSetEntry> zsetValue))
        {
          List<string> flattened = [];
          foreach (ZSetEntry entry in zsetValue.OrderBy(entry => entry.Score).ThenBy(entry => entry.Member))
          {
            flattened.Add(entry.Member);
            flattened.Add(entry.Score.ToString(CultureInfo.InvariantCulture));
          }
          return FormatArray(flattened);
        }
        break;
      case CacheValueType.Hash:
        if (value.TryGetHash(out Dictionary<string, string> hashValue))
        {
          List<string> flattened = [];
          foreach (KeyValuePair<string, string> entry in hashValue)
          {
            flattened.Add(entry.Key);
            flattened.Add(entry.Value);
          }
          return FormatArray(flattened);
        }
        break;
      case CacheValueType.Stream:
        if (value.TryGetStream(out List<StreamEntry> streamValue))
        {
          List<string> entries = [];
          foreach (StreamEntry entry in streamValue)
          {
            List<string> fields = [];
            foreach (KeyValuePair<string, string> field in entry.Fields)
            {
              fields.Add(field.Key);
              fields.Add(field.Value);
            }

            string fieldsResp = FormatArray(fields);
            string entryResp = FormatArrayOfResp([FormatBulk(entry.Id), fieldsResp]);
            entries.Add(entryResp);
          }

          return FormatArrayOfResp(entries);
        }
        break;
      case CacheValueType.VectorSet:
        if (value.TryGetVectorSet(out List<VectorSetEntry> vectorSetValue))
        {
          List<string> entries = [];
          foreach (VectorSetEntry entry in vectorSetValue)
          {
            List<string> vector = entry.Vector
              .Select(component => component.ToString(CultureInfo.InvariantCulture))
              .ToList();
            string vectorResp = FormatArray(vector);
            string entryResp = FormatArrayOfResp([FormatBulk(entry.Member), vectorResp]);
            entries.Add(entryResp);
          }

          return FormatArrayOfResp(entries);
        }
        break;
    }

    return "$-1\r\n";
  }

  public static string BuildError(string value)
  {
    return $"-ERR {value}\r\n";
  }
}