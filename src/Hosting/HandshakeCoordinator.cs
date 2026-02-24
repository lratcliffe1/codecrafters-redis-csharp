using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;

namespace codecrafters_redis.src.Hosting;

public interface IHandshakeCoordinator
{
  Task SendHandshakeToMasterAsync(int replicaOfPort, CancellationToken cancellationToken);
}

public sealed class HandshakeCoordinator(ServerOptions serverOptions, IClientHandler clientHandler, IClientIdAllocator clientIdAllocator) : IHandshakeCoordinator
{
  private readonly ServerOptions _serverOptions = serverOptions;
  private readonly IClientHandler _clientHandler = clientHandler;
  private readonly IClientIdAllocator _clientIdAllocator = clientIdAllocator;

  public async Task SendHandshakeToMasterAsync(int replicaOfPort, CancellationToken cancellationToken)
  {
    TcpClient master = new("localhost", replicaOfPort);
    NetworkStream stream = master.GetStream();

    // 1. Send PING and WAIT for "+PONG"
    await SendCommandAsync(stream, ["PING"], cancellationToken);
    _ = await ReadLineAsync(stream, cancellationToken);

    // 2. Send REPLCONF listening-port and WAIT for "+OK"
    await SendCommandAsync(stream, ["REPLCONF", "listening-port", _serverOptions.Port.ToString()], cancellationToken);
    _ = await ReadLineAsync(stream, cancellationToken);

    // 3. Send REPLCONF capa and WAIT for "+OK"
    await SendCommandAsync(stream, ["REPLCONF", "capa", "psync2"], cancellationToken);
    _ = await ReadLineAsync(stream, cancellationToken);

    // 4. Send PSYNC and WAIT for "+FULLRESYNC <REPL_ID> 0\r\n"
    await SendCommandAsync(stream, ["PSYNC", "?", "-1"], cancellationToken);
    _ = await ReadLineAsync(stream, cancellationToken);

    // 5. Receive RDB file body.
    int rdbLength = await ReadBulkLengthAsync(stream, cancellationToken);
    _ = await ReadExactlyAsync(stream, rdbLength, cancellationToken);

    // 6. Continue reading commands from the same replication connection.
    _ = _clientHandler.HandleClientAsync(master, _clientIdAllocator.Next(), cancellationToken, suppressResponse: true);
  }

  private static async Task SendCommandAsync(NetworkStream stream, string[] command, CancellationToken ct)
  {
    byte[] data = Encoding.UTF8.GetBytes(CommandHelper.FormatArray(command));
    await stream.WriteAsync(data, ct);
    await stream.FlushAsync(ct);
  }

  private static async Task<string> ReadLineAsync(NetworkStream stream, CancellationToken cancellationToken)
  {
    List<byte> bytes = [];
    byte[] chunk = new byte[1];

    while (true)
    {
      int bytesRead = await stream.ReadAsync(chunk, cancellationToken);
      if (bytesRead == 0)
      {
        throw new IOException("Connection closed while waiting for handshake response.");
      }

      bytes.Add(chunk[0]);
      int length = bytes.Count;
      if (length >= 2 && bytes[length - 2] == '\r' && bytes[length - 1] == '\n')
      {
        return Encoding.UTF8.GetString(bytes.Take(length - 2).ToArray());
      }
    }
  }

  private static async Task<int> ReadBulkLengthAsync(NetworkStream stream, CancellationToken cancellationToken)
  {
    string header = await ReadLineAsync(stream, cancellationToken);
    if (!header.StartsWith('$') || !int.TryParse(header[1..], out int rdbLength))
    {
      throw new InvalidOperationException("Invalid RDB bulk length from master.");
    }

    return rdbLength;
  }

  private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
  {
    byte[] data = new byte[length];
    int offset = 0;

    while (offset < length)
    {
      int bytesRead = await stream.ReadAsync(data.AsMemory(offset, length - offset), cancellationToken);
      if (bytesRead == 0)
      {
        throw new IOException("Connection closed while receiving RDB payload.");
      }

      offset += bytesRead;
    }

    return data;
  }
}
