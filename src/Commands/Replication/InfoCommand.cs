using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public static class InfoCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args, int port)
  {
    if (args.Count == 1)
    {
      return CommandHepler.FormatBulkAsync(
        "# Server\r\n" + GetServerInfo() +
        "# Client\r\n" + GetClientInfo() +
        "# Memory\r\n" + GetMemoryInfo() +
        "# Replication\r\n" +
        GetReplicationInfo(port));
    }

    if (args.Count == 2)
    {
      return args[1].ToString().ToLowerInvariant() switch {
        "server" => CommandHepler.FormatBulkAsync(GetServerInfo()),
        "client" => CommandHepler.FormatBulkAsync(GetClientInfo()),
        "memory" => CommandHepler.FormatBulkAsync(GetMemoryInfo()),
        "replication" => CommandHepler.FormatBulkAsync(GetReplicationInfo(port)),
        _ => CommandHepler.BuildErrorAsync("invalid argument for 'info'"),
      };
    }

    return CommandHepler.BuildErrorAsync("wrong number of arguments for 'info' command");
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

  private static string GetReplicationInfo(int port)
  {
    if (ReplicaCache.TryGetValue(port, out var entry) && entry != null)
    {
      return $"role:{entry.Type}\r\n".ToLowerInvariant();
    }

    return "role:master\r\n";
  }
}