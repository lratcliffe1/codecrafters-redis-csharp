using codecrafters_redis.src.Cache;

namespace codecrafters_redis.src.Commands.Multi;

public interface ITransactionGuard
{
  bool WatchedKeyWasModified(long clientId);
}

public sealed class TransactionGuard(IClientWatchStore clientWatchStore, ICacheStore cacheStore) : ITransactionGuard
{
  public bool WatchedKeyWasModified(long clientId)
  {
    if (!clientWatchStore.TryGetValue(clientId, out Dictionary<string, long>? watchedKeyVersions) || watchedKeyVersions == null)
    {
      return false;
    }

    foreach ((string key, long watchedVersion) in watchedKeyVersions)
    {
      if (cacheStore.GetKeyVersion(key) != watchedVersion)
      {
        return true;
      }
    }

    return false;
  }
}
