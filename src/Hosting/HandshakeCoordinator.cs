using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Hosting;

public interface IHandshakeCoordinator
{
  Task SendHandshakeToMasterAsync(int replicaOfPort, CancellationToken cancellationToken);
  Task SendHandshakeToSlavesAsync(CancellationToken cancellationToken);
}

public sealed class HandshakeCoordinator(ServerOptions serverOptions) : IHandshakeCoordinator
{
  private readonly ServerOptions _serverOptions = serverOptions;

  public async Task SendHandshakeToMasterAsync(int replicaOfPort, CancellationToken cancellationToken)
  {
    using TcpClient master = new TcpClient("localhost", replicaOfPort);
    using NetworkStream stream = master.GetStream();
    byte[] buffer = new byte[1024];

    // 1. Send PING and WAIT for "+PONG"
    await SendCommandAsync(stream, ["PING"], cancellationToken);
    _ = await stream.ReadAsync(buffer, cancellationToken);

    // 2. Send REPLCONF listening-port and WAIT for "+OK"
    await SendCommandAsync(stream, ["REPLCONF", "listening-port", _serverOptions.Port.ToString()], cancellationToken);
    _ = await stream.ReadAsync(buffer, cancellationToken);

    // 3. Send REPLCONF capa and WAIT for "+OK"
    await SendCommandAsync(stream, ["REPLCONF", "capa", "psync2"], cancellationToken);
    _ = await stream.ReadAsync(buffer, cancellationToken);
  }

  private static async Task SendCommandAsync(NetworkStream stream, string[] command, CancellationToken ct)
  {
    byte[] data = Encoding.UTF8.GetBytes(CommandHelper.FormatArray(command));
    await stream.WriteAsync(data, ct);
    await stream.FlushAsync(ct);
  }


  public async Task SendHandshakeToSlavesAsync(CancellationToken cancellationToken)
  {
  }
}