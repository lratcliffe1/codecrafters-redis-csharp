using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands;

public sealed record CommandExecutionContext(
  long ClientId,
  int Port,
  RespValue RespValue,
  CancellationToken CancellationToken);
