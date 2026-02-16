using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class PsyncCommand : IRedisCommand
{
  public string Name => "PSYNC";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    return Task.FromResult(CommandHelper.FormatSimple("+FULLRESYNC " + ReplicationID.Get() + " 0"));
  }
}