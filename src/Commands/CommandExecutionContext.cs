using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands;

public enum CommandMode
{
  None = 0,
  Command = 1,
  Multi = 2,
  PubSub = 3,
}

public sealed record CommandExecutionContext(
  long ClientId,
  int Port,
  RespValue RespValue,
  CommandMode Mode,
  CancellationToken CancellationToken);
