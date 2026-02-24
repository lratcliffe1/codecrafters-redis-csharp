using System.Net;
using System.Net.Sockets;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Hosting;

public interface IRedisServerHost
{
  Task RunAsync(CancellationToken cancellationToken = default);
}

public sealed class RedisServerHost(
  ServerOptions serverOptions,
  ICommandEventLoop commandEventLoop,
  IClientIdAllocator clientIdAllocator,
  IHandshakeCoordinator handshakeCoordinator,
  IClientHandler clientHandler) : IRedisServerHost
{
  private readonly ServerOptions _serverOptions = serverOptions;
  private readonly ICommandEventLoop _commandEventLoop = commandEventLoop;
  private readonly IClientIdAllocator _clientIdAllocator = clientIdAllocator;
  private readonly IHandshakeCoordinator _handshakeCoordinator = handshakeCoordinator;
  private readonly IClientHandler _clientHandler = clientHandler;

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    using CancellationTokenSource shutdownTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    Console.CancelKeyPress += OnCancelKeyPress;

    TcpListener? server = null;
    try
    {
      server = new TcpListener(IPAddress.Any, _serverOptions.Port);
      server.Start();

      await SendHandshakeAsync(shutdownTokenSource.Token);

      while (!shutdownTokenSource.Token.IsCancellationRequested)
      {
        TcpClient client = await server.AcceptTcpClientAsync(shutdownTokenSource.Token);
        _ = _clientHandler.HandleClientAsync(client, _clientIdAllocator.Next(), shutdownTokenSource.Token);
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

      await _commandEventLoop.DisposeAsync().ConfigureAwait(false);
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
    if (_serverOptions.IsReplica)
    {
      await _handshakeCoordinator.SendHandshakeToMasterAsync(_serverOptions.ReplicaOfPort!.Value, cancellationToken);
    }
  }
}
