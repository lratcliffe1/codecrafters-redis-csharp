using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class DiscardCommand(IClientMultiStore clientMultiStore) : IRedisCommand
{
  public string Name => "MULTI";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    clientMultiStore.Remove(context.ClientId);
    return CommandHelper.FormatSimpleAsync("OK");
  }
}
