using System.Collections.Concurrent;

namespace codecrafters_redis.src.Cache;

public interface IReplicaStore
{
  void Set(int key, ReplicaType type);
  bool TryGetValue(int key, out ReplicaCacheEntry? value);
  void AddSlave(int key, int slaveKey);
}

public sealed class ReplicaStore : IReplicaStore
{
  private readonly ConcurrentDictionary<int, ReplicaCacheEntry> _replicaCache = new();

  public void Set(int key, ReplicaType type)
  {
    _replicaCache[key] = new ReplicaCacheEntry(key, type);
  }

  public bool TryGetValue(int key, out ReplicaCacheEntry? value)
  {
    return _replicaCache.TryGetValue(key, out value);
  }

  public void AddSlave(int key, int slaveKey)
  {
    if (!_replicaCache.TryGetValue(key, out var entry))
    {
      throw new InvalidOperationException($"Replica cache entry not found for key: {key}");
    }

    entry.Slaves.Add(slaveKey);
  }
}

public class ReplicaCacheEntry(int port, ReplicaType type)
{
  public int Port { get; set; } = port;
  public ReplicaType Type { get; set; } = type;
  public HashSet<int> Slaves { get; set; } = [];
}

public enum ReplicaType
{
  Master,
  Slave,
}
