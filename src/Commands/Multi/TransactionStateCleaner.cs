using codecrafters_redis.src.Cache;

namespace codecrafters_redis.src.Commands.Multi;

public interface ITransactionStateCleaner
{
  void Clear(long clientId);
}

public sealed class TransactionStateCleaner(
  IClientMultiStore clientMultiStore,
  IClientWatchStore clientWatchStore) : ITransactionStateCleaner
{
  public void Clear(long clientId)
  {
    clientMultiStore.Remove(clientId);
    clientWatchStore.Remove(clientId);
  }
}
