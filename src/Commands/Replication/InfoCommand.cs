using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class InfoCommand(ServerOptions serverOptions) : IRedisCommand
{
  public string Name => "INFO";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count == 1)
    {
      return CommandHelper.FormatBulkAsync(
        "# Server\r\n" + GetServerInfo() +
        "# Client\r\n" + GetClientInfo() +
        "# Memory\r\n" + GetMemoryInfo() +
        "# Replication\r\n" +
        GetReplicationInfo(serverOptions.IsReplica));
    }

    if (args.Count == 2)
    {
      return args[1].ToString().ToLowerInvariant() switch
      {
        "server" => CommandHelper.FormatBulkAsync(GetServerInfo()),
        "client" => CommandHelper.FormatBulkAsync(GetClientInfo()),
        "memory" => CommandHelper.FormatBulkAsync(GetMemoryInfo()),
        "replication" => CommandHelper.FormatBulkAsync(GetReplicationInfo(serverOptions.IsReplica)),
        _ => CommandHelper.BuildErrorAsync("invalid argument for 'info'"),
      };
    }

    return CommandHelper.BuildErrorAsync("wrong number of arguments for 'info' command");
  }

  private static string GetServerInfo()
  {
    return $"redis_version:7.2.4\r\n";
  }

  private static string GetClientInfo()
  {
    return $"connected_clients:1\r\n";
  }

  private static string GetMemoryInfo()
  {
    return $"used_memory:1000\r\n".ToLowerInvariant();
  }

  private static string GetReplicationInfo(bool isReplica)
  {
    return ($"role:{(isReplica ? "slave" : "master")}\r\n"
      + $"master_replid:{ReplicationID.Get()}\r\n"
      + $"master_repl_offset:0\r\n")
      .ToLowerInvariant();
  }
}
