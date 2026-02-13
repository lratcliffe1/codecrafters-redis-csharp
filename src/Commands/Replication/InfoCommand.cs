using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Commands.Replication;

public static class InfoCommand
{
  public static Task<string> ProcessAsync(List<RespValue> args)
  {
    if (args.Count == 1)
    {
      return args[0].ToString() switch {
        "server" => CommandHepler.FormatBulkAsync(GetServerInfo()),
        "client" => CommandHepler.FormatBulkAsync(GetClientInfo()),
        "memory" => CommandHepler.FormatBulkAsync(GetMemoryInfo()),
        "replication" => CommandHepler.FormatBulkAsync(GetReplicationInfo()),
        _ => CommandHepler.BuildErrorAsync("invalid argument for 'info'"),
      };
    }

    return CommandHepler.FormatBulkAsync(
      "# Server\r\n" + GetServerInfo() +
      "# Client\r\n" + GetClientInfo() +
      "# Memory\r\n" + GetMemoryInfo() +
      "# Replication\r\n" + GetReplicationInfo());
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
    return $"used_memory:1000\r\n";
  }

  private static string GetReplicationInfo()
  {
    return $"role:master\r\n";
  }
}