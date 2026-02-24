namespace codecrafters_redis.src.Bootstrap;

public interface IServerOptions
{
  int Port { get; }
  int? ReplicaOfPort { get; }
  bool IsReplica { get; }
  string? DataDirectory { get; }
  string? Dbfilename { get; }

  long GetAckBytes();
  void AddAckBytes(int bytes);
  void ResetAckBytes();
}

public sealed class ServerOptions(int port, int? replicaOfPort, string? dataDirectory, string? dbfilename) : IServerOptions
{
  private long _ackBytes;

  public int Port { get; } = port;
  public int? ReplicaOfPort { get; } = replicaOfPort;
  public bool IsReplica => ReplicaOfPort is not null;
  public string? DataDirectory { get; } = dataDirectory;
  public string? Dbfilename { get; } = dbfilename;

  public long GetAckBytes() => Interlocked.Read(ref _ackBytes);
  public void AddAckBytes(int bytes)
  {
    if (bytes > 0)
    {
      Interlocked.Add(ref _ackBytes, bytes);
    }
  }
  public void ResetAckBytes() => Interlocked.Exchange(ref _ackBytes, 0);
}
