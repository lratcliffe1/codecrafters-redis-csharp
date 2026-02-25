using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src.Hosting;

public interface IClientConnectionRegistry
{
  void Register(long clientId, NetworkStream stream);
  void Unregister(long clientId);
  Task<bool> TryWriteAsync(long clientId, string payload, CancellationToken cancellationToken);
}

public sealed class ClientConnectionRegistry : IClientConnectionRegistry
{
  private readonly ConcurrentDictionary<long, ClientConnectionState> _connections = [];

  public void Register(long clientId, NetworkStream stream)
  {
    ClientConnectionState next = new(stream);
    if (_connections.TryGetValue(clientId, out ClientConnectionState? previous))
    {
      previous.Dispose();
    }

    _connections[clientId] = next;
  }

  public void Unregister(long clientId)
  {
    if (_connections.TryRemove(clientId, out ClientConnectionState? connection))
    {
      connection.Dispose();
    }
  }

  public async Task<bool> TryWriteAsync(long clientId, string payload, CancellationToken cancellationToken)
  {
    if (!_connections.TryGetValue(clientId, out ClientConnectionState? connection))
    {
      return false;
    }

    byte[] message = Encoding.UTF8.GetBytes(payload);
    await connection.WriteLock.WaitAsync(cancellationToken);
    try
    {
      await connection.Stream.WriteAsync(message, cancellationToken);
      await connection.Stream.FlushAsync(cancellationToken);
      return true;
    }
    catch (Exception exception) when (exception is IOException or ObjectDisposedException)
    {
      Unregister(clientId);
      return false;
    }
    finally
    {
      connection.WriteLock.Release();
    }
  }

  private sealed class ClientConnectionState(NetworkStream stream) : IDisposable
  {
    public NetworkStream Stream { get; } = stream;
    public SemaphoreSlim WriteLock { get; } = new(1, 1);

    public void Dispose()
    {
      WriteLock.Dispose();
    }
  }
}
