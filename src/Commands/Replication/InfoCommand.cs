using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public sealed class InfoCommand(IReplicaStore replicaStore) : IRedisCommand
{
  public string Name => "MULTI";
  public Task<string> ExecuteAsync(List<RespValue> args, CommandExecutionContext context)
  {
    if (args.Count == 1)
    {
      return CommandHelper.FormatBulkAsync(
        "# Server\r\n" + GetServerInfo() +
        "# Client\r\n" + GetClientInfo() +
        "# Memory\r\n" + GetMemoryInfo() +
        "# Replication\r\n" +
        GetReplicationInfo(context.Port));
    }

    if (args.Count == 2)
    {
      return args[1].ToString().ToLowerInvariant() switch
      {
        "server" => CommandHelper.FormatBulkAsync(GetServerInfo()),
        "client" => CommandHelper.FormatBulkAsync(GetClientInfo()),
        "memory" => CommandHelper.FormatBulkAsync(GetMemoryInfo()),
        "replication" => CommandHelper.FormatBulkAsync(GetReplicationInfo(context.Port)),
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

  private string GetReplicationInfo(int port)
  {
    if (replicaStore.TryGetValue(port, out var entry) && entry != null)
    {
      return ($"role:{entry.Type}\r\n"
        + $"master_replid:{ReplicationID.Get()}\r\n"
        + $"master_repl_offset:0\r\n")
      .ToLowerInvariant();
    }

    return ("role:master\r\n"
      + $"master_replid:{ReplicationID.Get()}\r\n"
      + $"master_repl_offset:0\r\n")
      .ToLowerInvariant();
  }
}