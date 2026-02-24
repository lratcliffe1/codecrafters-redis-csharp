using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands;

public interface IRedisCommand
{
  string Name { get; }
  Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context);
}
