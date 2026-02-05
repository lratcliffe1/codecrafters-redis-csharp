namespace codecrafters_redis.src.Cache;

public enum CacheValueType
{
  None = 0,
  String = 1,
  List = 2,
  Set = 3,
  ZSet = 4,
  Hash = 5,
  Stream = 6,
  VectorSet = 7,
  StreamEntries = 8,
}

public sealed record CacheValue(CacheValueType Type, object Value)
{
  public static CacheValue String(string value) => new(CacheValueType.String, value);

  public static CacheValue List(List<string> value) => new(CacheValueType.List, value);

  public static CacheValue Set(HashSet<string> value) => new(CacheValueType.Set, value);

  public static CacheValue ZSet(List<ZSetEntry> value) => new(CacheValueType.ZSet, value);

  public static CacheValue Hash(Dictionary<string, string> value) => new(CacheValueType.Hash, value);

  public static CacheValue Stream(string key, List<StreamEntry> value)
    => new(CacheValueType.Stream, new StreamReadResult(key, value));

  public static CacheValue StreamEntries(List<StreamEntry> value)
    => new(CacheValueType.StreamEntries, value);

  public static CacheValue VectorSet(List<VectorSetEntry> value) => new(CacheValueType.VectorSet, value);

  public bool TryGetString(out string value)
  {
    if (Type == CacheValueType.String && Value is string stringValue)
    {
      value = stringValue;
      return true;
    }

    value = string.Empty;
    return false;
  }

  public bool TryGetList(out List<string> value)
  {
    if (Type == CacheValueType.List && Value is List<string> listValue)
    {
      value = listValue;
      return true;
    }

    value = [];
    return false;
  }

  public bool TryGetSet(out HashSet<string> value)
  {
    if (Type == CacheValueType.Set && Value is HashSet<string> setValue)
    {
      value = setValue;
      return true;
    }

    value = [];
    return false;
  }

  public bool TryGetZSet(out List<ZSetEntry> value)
  {
    if (Type == CacheValueType.ZSet && Value is List<ZSetEntry> zsetValue)
    {
      value = zsetValue;
      return true;
    }

    value = [];
    return false;
  }

  public bool TryGetHash(out Dictionary<string, string> value)
  {
    if (Type == CacheValueType.Hash && Value is Dictionary<string, string> hashValue)
    {
      value = hashValue;
      return true;
    }

    value = [];
    return false;
  }

  public bool TryGetStream(out List<StreamEntry> value)
  {
    if (Type == CacheValueType.StreamEntries && Value is List<StreamEntry> streamValue)
    {
      value = streamValue;
      return true;
    }

    value = [];
    return false;
  }

  public bool TryGetStreamReadResult(out StreamReadResult value)
  {
    if (Type == CacheValueType.Stream && Value is StreamReadResult streamValue)
    {
      value = streamValue;
      return true;
    }

    value = new StreamReadResult(string.Empty, []);
    return false;
  }

  public bool TryGetVectorSet(out List<VectorSetEntry> value)
  {
    if (Type == CacheValueType.VectorSet && Value is List<VectorSetEntry> vectorSetValue)
    {
      value = vectorSetValue;
      return true;
    }

    value = [];
    return false;
  }
}

public sealed record ZSetEntry(string Member, double Score);

public sealed record StreamEntry(string Id, Dictionary<string, string> Fields);

public sealed record StreamReadResult(string Key, List<StreamEntry> Entries);

public sealed record VectorSetEntry(string Member, IReadOnlyList<float> Vector);
