using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class MultiCommand(IClientMultiStore clientMultiStore) : IRedisCommand
{
  public string Name => "MULTI";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    var command = args[0].ToString();

    clientMultiStore.Set(context.ClientId, command == Name ? null : context.RespValue);

    return CommandHelper.FormatSimpleAsync(command == Name ? "OK" : "QUEUED");
  }

  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context, RespValue RespValue)
  {
    throw new NotImplementedException();
  }
}
