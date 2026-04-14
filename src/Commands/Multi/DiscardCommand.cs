using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class DiscardCommand(IClientMultiStore clientMultiStore, IClientWatchStore clientWatchStore) : IRedisCommand
{
  public string Name => "DISCARD";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    clientMultiStore.Remove(context.ClientId);
    clientWatchStore.Remove(context.ClientId);
    return CommandHelper.FormatSimpleAsync("OK");
  }
}
