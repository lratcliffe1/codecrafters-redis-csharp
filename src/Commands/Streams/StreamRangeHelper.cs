namespace codecrafters_redis.src.Commands.Streams;

using codecrafters_redis.src.Cache;

internal static class StreamRangeHelper
{
  public static List<StreamEntry> FilterEntries(List<StreamEntry> entries, string startValue, string endValue)
  {
    if (entries.Count == 0)
    {
      return [];
    }

    var start = ParseId(startValue, isStart: true);
    var end = ParseId(endValue, isStart: false);
    List<StreamEntry> result = [];

    foreach (var entry in entries)
    {
      var id = ParseId(entry.Id, isStart: true);
      if (IsInRange(id, start, end))
      {
        result.Add(entry);
      }
    }

    return result;
  }

  private static (long milliseconds, long sequence) ParseId(string value, bool isStart)
  {
    if (isStart && value == "-")
    {
      value = "0-0";
    }

    if (!isStart && value == "+")
    {
      value = $"{long.MaxValue}-{long.MaxValue}";
    }

    var parts = value.Split("-");
    return (long.Parse(parts[0]), long.Parse(parts[1]));
  }

  private static bool IsInRange(
    (long milliseconds, long sequence) id,
    (long milliseconds, long sequence) start,
    (long milliseconds, long sequence) end)
  {
    if (id.milliseconds < start.milliseconds)
      return false;

    if (id.milliseconds > end.milliseconds)
      return false;

    if (id.milliseconds == start.milliseconds && id.sequence < start.sequence)
      return false;

    if (id.milliseconds == end.milliseconds && id.sequence > end.sequence)
      return false;

    return true;
  }
}
