using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands;

public sealed record CommandExecutionContext(long ClientId, int Port, CancellationToken CancellationToken, RespValue RespValue);

public interface IRedisCommand
{
  string Name { get; }
  Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context);
}
