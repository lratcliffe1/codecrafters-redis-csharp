using System.Globalization;
using System.Text;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Helpers;

public static class CommandHepler
{
  public static string FormatSimple(string value)
  {
    return $"+{value}\r\n";
  }

  public static string BuildError(string value)
  {
    return $"-ERR {value}\r\n";
  }

  public static string FormatInteger(int value)
  {
    return $":{value}\r\n";
  }

  public static string FormatBulk(string value)
  {
    return $"${value.Length}\r\n{value}\r\n";
  }

  public static string FormatNull(RespType respType)
  {
    return respType switch
    {
      RespType.Integer => ":-1\r\n",
      RespType.Array => "*-1\r\n",
      _ => "$-1\r\n",
    };
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

  public static string FormatStreamEntries(IReadOnlyList<StreamEntry> streamValue)
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

  public static string FormatStreamReadResult(string key, IReadOnlyList<StreamEntry> streamValue)
  {
    string entriesResp = FormatStreamEntries(streamValue);
    string streamResp = FormatArrayOfResp([FormatBulk(key), entriesResp]);
    return FormatArrayOfResp([streamResp]);
  }

  public static string FormatValue(CacheValue value)
  {
    return value.Type switch
    {
      CacheValueType.String when value.TryGetString(out string stringValue) => FormatBulk(stringValue),
      CacheValueType.List when value.TryGetList(out List<string> listValue) => FormatArray(listValue),
      CacheValueType.Set when value.TryGetSet(out HashSet<string> setValue) => FormatSetValue(setValue),
      CacheValueType.ZSet when value.TryGetZSet(out List<ZSetEntry> zsetValue) => FormatZSetValue(zsetValue),
      CacheValueType.Hash when value.TryGetHash(out Dictionary<string, string> hashValue) => FormatHashValue(hashValue),
      CacheValueType.Stream when value.TryGetStreamReadResult(out StreamReadResult streamValue) => FormatStreamReadResult(streamValue.Key, streamValue.Entries),
      CacheValueType.StreamEntries when value.TryGetStream(out List<StreamEntry> streamEntriesValue) => FormatStreamEntries(streamEntriesValue),
      CacheValueType.VectorSet when value.TryGetVectorSet(out List<VectorSetEntry> vectorSetValue) => FormatVectorSetValue(vectorSetValue),
      _ => "$-1\r\n"
    };
  }

  private static string FormatSetValue(HashSet<string> values)
  {
    return FormatArray(values.OrderBy(item => item).ToList());
  }

  private static string FormatZSetValue(List<ZSetEntry> entries)
  {
    List<string> flattened = [];
    foreach (ZSetEntry entry in entries.OrderBy(entry => entry.Score).ThenBy(entry => entry.Member))
    {
      flattened.Add(entry.Member);
      flattened.Add(entry.Score.ToString(CultureInfo.InvariantCulture));
    }

    return FormatArray(flattened);
  }

  private static string FormatHashValue(Dictionary<string, string> values)
  {
    List<string> flattened = [];
    foreach (KeyValuePair<string, string> entry in values)
    {
      flattened.Add(entry.Key);
      flattened.Add(entry.Value);
    }

    return FormatArray(flattened);
  }

  private static string FormatVectorSetValue(List<VectorSetEntry> values)
  {
    List<string> entries = [];
    foreach (VectorSetEntry entry in values)
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
}