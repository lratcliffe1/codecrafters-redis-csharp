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
    string? dataDirectory = null;
    string? dbfilename = null;

    int index = 0;
    while (index < args.Length)
    {
      int consumedArgs = 0;
      string option = args[index].ToLowerInvariant();

      switch (option)
      {
        case "--port":
          port = TryParseInt(args, index, out consumedArgs) ?? 6379;
          break;
        case "--replicaof":
          replicaOfPort = TryParseReplicaOfPort(args, index, out consumedArgs);
          break;
        case "--dir":
          dataDirectory = TryParseString(args, index, out consumedArgs);
          break;
        case "--dbfilename":
          dbfilename = TryParseString(args, index, out consumedArgs);
          break;
        default:
          break;
      }

      index += consumedArgs + 1;
    }

    return new ServerOptions(port, replicaOfPort, dataDirectory, dbfilename);
  }

  private static int? TryParseInt(string[] args, int index, out int consumedArgs)
  {
    consumedArgs = 0;

    if (index + 1 < args.Length && int.TryParse(args[index + 1], out int parsedInt))
    {
      consumedArgs = 1;
      return parsedInt;
    }
    return null;
  }

  private static string? TryParseString(string[] args, int index, out int consumedArgs)
  {
    consumedArgs = 0;

    if (index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1]) && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
    {
      consumedArgs = 1;
      return args[index + 1];
    }
    return null;
  }

  private static int? TryParseReplicaOfPort(string[] args, int replicaOfIndex, out int consumedArgs)
  {
    consumedArgs = 0;

    if (replicaOfIndex + 1 >= args.Length)
    {
      return null;
    }

    if (replicaOfIndex + 2 < args.Length && int.TryParse(args[replicaOfIndex + 2], out int twoArgPort))
    {
      consumedArgs = 2;
      return twoArgPort;
    }

    string[] parts = args[replicaOfIndex + 1].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (parts.Length == 0)
    {
      return null;
    }

    if (!int.TryParse(parts[^1], out int singleArgPort))
    {
      return null;
    }

    consumedArgs = 1;
    return singleArgPort;
  }
}
