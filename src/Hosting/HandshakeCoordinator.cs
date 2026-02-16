using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Hosting;

public interface IHandshakeCoordinator
{
  Task SendHandshakeToMasterAsync(int replicaOfPort, CancellationToken cancellationToken);
  Task SendHandshakeToSlavesAsync(CancellationToken cancellationToken);
}

public sealed class HandshakeCoordinator(IReplicaStore replicaStore) : IHandshakeCoordinator
{
  private readonly IReplicaStore _replicaStore = replicaStore;

  public async Task SendHandshakeToMasterAsync(int replicaOfPort, CancellationToken cancellationToken)
  {
    using TcpClient master = new TcpClient("localhost", replicaOfPort);
    using NetworkStream stream = master.GetStream();
    await stream.WriteAsync(Encoding.UTF8.GetBytes(CommandHelper.FormatArray(["PING"])), cancellationToken);
    await stream.FlushAsync(cancellationToken);
  }

  public async Task SendHandshakeToSlavesAsync(CancellationToken cancellationToken)
  {
  }
}