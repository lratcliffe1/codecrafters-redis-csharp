namespace codecrafters_redis.src.Helpers;

public static class StreamIdHelper
{
  public static bool IsGreaterThan(string candidateId, string thresholdId)
  {
    var candidate = ParseId(candidateId);
    var threshold = ParseId(thresholdId);

    if (candidate.milliseconds > threshold.milliseconds)
    {
      return true;
    }

    return candidate.milliseconds == threshold.milliseconds && candidate.sequence > threshold.sequence;
  }

  private static (long milliseconds, long sequence) ParseId(string id)
  {
    var parts = id.Split("-");
    return (long.Parse(parts[0]), long.Parse(parts[1]));
  }
}
