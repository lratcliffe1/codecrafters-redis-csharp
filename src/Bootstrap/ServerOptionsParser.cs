namespace codecrafters_redis.src.Bootstrap;

public interface IServerOptionsParser
{
  ServerOptions Parse(string[] args);
}

public sealed class ServerOptionsParser : IServerOptionsParser
{
  public ServerOptions Parse(string[] args)
  {
    int port = 6379;
    int? replicaOfPort = null;

    for (int index = 0; index < args.Length; index++)
    {
      if (string.Equals(args[index], "--port", StringComparison.InvariantCultureIgnoreCase))
      {
        if (index + 1 < args.Length && int.TryParse(args[index + 1], out int parsedPort))
        {
          port = parsedPort;
          index++;
        }

        continue;
      }

      if (!string.Equals(args[index], "--replicaof", StringComparison.InvariantCultureIgnoreCase))
      {
        continue;
      }

      if (TryParseReplicaOfPort(args, index, out int parsedReplicaPort, out int consumedArgs))
      {
        replicaOfPort = parsedReplicaPort;
        index += consumedArgs;
      }
    }

    return new ServerOptions(port, replicaOfPort);
  }

  private static bool TryParseReplicaOfPort(string[] args, int replicaOfIndex, out int replicaPort, out int consumedArgs)
  {
    replicaPort = 0;
    consumedArgs = 0;

    if (replicaOfIndex + 1 >= args.Length)
    {
      return false;
    }

    if (replicaOfIndex + 2 < args.Length && int.TryParse(args[replicaOfIndex + 2], out int twoArgPort))
    {
      replicaPort = twoArgPort;
      consumedArgs = 2;
      return true;
    }

    string[] parts = args[replicaOfIndex + 1]
      .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (parts.Length == 0)
    {
      return false;
    }

    if (!int.TryParse(parts[^1], out int singleArgPort))
    {
      return false;
    }

    replicaPort = singleArgPort;
    consumedArgs = 1;
    return true;
  }
}
