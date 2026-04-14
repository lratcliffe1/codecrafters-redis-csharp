using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Multi;

public class DiscardCommand(ITransactionStateCleaner transactionStateCleaner) : IRedisCommand
{
  public string Name => "DISCARD";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    transactionStateCleaner.Clear(context.ClientId);
    return CommandHelper.FormatSimpleAsync("OK");
  }
}
