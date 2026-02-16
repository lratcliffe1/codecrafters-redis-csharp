using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class ReplconfCommand : IRedisCommand
{
  public string Name => "REPLCONF";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    return Task.FromResult(CommandHelper.FormatSimple("OK"));
  }
}