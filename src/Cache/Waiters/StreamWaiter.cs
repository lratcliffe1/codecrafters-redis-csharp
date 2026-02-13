namespace codecrafters_redis.src.Cache.Waiters;

public sealed class StreamWaiter
{
  private readonly Dictionary<string, string> _lastSeenIds;

  public StreamWaiter(IReadOnlyList<(string key, string id)> streams)
  {
    Streams = streams;
    Signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    _lastSeenIds = streams.ToDictionary(stream => stream.key, stream => stream.id);
  }

  public IReadOnlyList<(string key, string id)> Streams { get; }
  public TaskCompletionSource<bool> Signal { get; }

  public bool TryGetLastSeenId(string key, out string lastSeenId)
  {
    if (_lastSeenIds.TryGetValue(key, out string? value))
    {
      lastSeenId = value;
      return true;
    }

    lastSeenId = string.Empty;
    return false;
  }
}
