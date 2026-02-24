using System.Net;
using System.Net.Sockets;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Hosting;

public interface IRedisServerHost
{
  Task RunAsync(CancellationToken cancellationToken = default);
}

public sealed class RedisServerHost(
  IServerOptions serverOptions,
  ICommandEventLoop commandEventLoop,
  IClientIdAllocator clientIdAllocator,
  IHandshakeCoordinator handshakeCoordinator,
  IClientHandler clientHandler,
  IRDBFileReader rdbFileReader) : IRedisServerHost
{

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    await rdbFileReader.ReadAsync(cancellationToken);

    using CancellationTokenSource shutdownTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    Console.CancelKeyPress += OnCancelKeyPress;

    TcpListener? server = null;
    try
    {
      server = new TcpListener(IPAddress.Any, serverOptions.Port);
      server.Start();

      await SendHandshakeAsync(shutdownTokenSource.Token);

      while (!shutdownTokenSource.Token.IsCancellationRequested)
      {
        TcpClient client = await server.AcceptTcpClientAsync(shutdownTokenSource.Token);
        _ = clientHandler.HandleClientAsync(client, clientIdAllocator.Next(), shutdownTokenSource.Token);
      }
    }
    catch (OperationCanceledException)
    {
      // Graceful shutdown.
    }
    catch (SocketException exception)
    {
      Console.WriteLine("SocketException: {0}", exception);
    }
    finally
    {
      server?.Stop();
      Console.CancelKeyPress -= OnCancelKeyPress;

      await commandEventLoop.DisposeAsync().ConfigureAwait(false);
    }

    return;

    void OnCancelKeyPress(object? _, ConsoleCancelEventArgs eventArgs)
    {
      eventArgs.Cancel = true;
      shutdownTokenSource.Cancel();
    }
  }

  private async Task SendHandshakeAsync(CancellationToken cancellationToken)
  {
    if (serverOptions.IsReplica)
    {
      await handshakeCoordinator.SendHandshakeToMasterAsync(serverOptions.ReplicaOfPort!.Value, cancellationToken);
    }
  }
}
