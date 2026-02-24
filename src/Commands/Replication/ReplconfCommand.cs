using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class ReplconfCommand : IRedisCommand
{
  public string Name => "REPLCONF";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count > 1 && string.Equals(args[1].ToString(), "GETACK", StringComparison.OrdinalIgnoreCase))
    {
      return Task.FromResult(CommandHelper.FormatArray(["REPLCONF", "ACK", "0"]));
    }

    return Task.FromResult(CommandHelper.FormatSimple("OK"));
  }
}
