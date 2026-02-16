using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Cache;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Hosting;

public interface IRedisServerHost
{
  Task RunAsync(CancellationToken cancellationToken = default);
}

public sealed class RedisServerHost(
  ServerOptions serverOptions,
  IReplicaStore replicaStore,
  ICommandEventLoop commandEventLoop,
  IRespParser respParser,
  IClientIdAllocator clientIdAllocator,
  IHandshakeCoordinator handshakeCoordinator) : IRedisServerHost
{
  private readonly ServerOptions _serverOptions = serverOptions;
  private readonly IReplicaStore _replicaStore = replicaStore;
  private readonly ICommandEventLoop _commandEventLoop = commandEventLoop;
  private readonly IRespParser _respParser = respParser;
  private readonly IClientIdAllocator _clientIdAllocator = clientIdAllocator;
  private readonly IHandshakeCoordinator _handshakeCoordinator = handshakeCoordinator;

  public async Task RunAsync(CancellationToken cancellationToken = default)
  {
    InitializeReplicaState();

    using CancellationTokenSource shutdownTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    Console.CancelKeyPress += OnCancelKeyPress;

    TcpListener? server = null;
    try
    {
      server = new TcpListener(IPAddress.Any, _serverOptions.Port);
      server.Start();

      await SendHandshakeAsync(cancellationToken);

      while (!shutdownTokenSource.Token.IsCancellationRequested)
      {
        TcpClient client = await server.AcceptTcpClientAsync(shutdownTokenSource.Token);
        _ = HandleClientAsync(client, _clientIdAllocator.Next(), shutdownTokenSource.Token);
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

  private void InitializeReplicaState()
  {
    _replicaStore.Set(_serverOptions.Port, _serverOptions.IsReplica ? ReplicaType.Slave : ReplicaType.Master);
    if (_serverOptions.ReplicaOfPort.HasValue)
    {
      _replicaStore.AddSlave(_serverOptions.Port, _serverOptions.ReplicaOfPort.Value);
    }
  }

  private async Task HandleClientAsync(TcpClient client, long clientId, CancellationToken serverCancellationToken)
  {
    using TcpClient _ = client;
    using CancellationTokenSource connectionCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
    CancellationToken cancellationToken = connectionCancellationTokenSource.Token;

    byte[] bytes = new byte[4096];
    StringBuilder incomingBuffer = new();
    NetworkStream stream = client.GetStream();

    try
    {
      while (true)
      {
        int bytesRead = await stream.ReadAsync(bytes, cancellationToken);
        if (bytesRead == 0)
        {
          break;
        }

        incomingBuffer.Append(Encoding.ASCII.GetString(bytes, 0, bytesRead));

        while (TryReadNextCommand(incomingBuffer, out RespValue? value))
        {
          string response = await _commandEventLoop.ExecuteAsync(value!, clientId, _serverOptions.Port, cancellationToken);
          byte[] message = Encoding.UTF8.GetBytes(response);
          await stream.WriteAsync(message, cancellationToken);
          await stream.FlushAsync(cancellationToken);

          if (response.StartsWith("+FULLRESYNC"))
          {
            await SendRDBFileAsync(stream, cancellationToken);
          }
        }
      }
    }
    catch (OperationCanceledException)
    {
      // Connection was terminated.
    }
    catch (InvalidOperationException exception)
    {
      string error = CommandHelper.BuildError($"protocol error: {exception.Message}");
      byte[] message = Encoding.UTF8.GetBytes(error);
      await stream.WriteAsync(message, serverCancellationToken);
      await stream.FlushAsync(serverCancellationToken);
    }
    finally
    {
      await _commandEventLoop.NotifyClientDisconnectedAsync(clientId, _serverOptions.Port);
    }
  }

  private bool TryReadNextCommand(StringBuilder buffer, out RespValue? value)
  {
    string data = buffer.ToString();
    if (!_respParser.TryParse(data, out RespValue parsedValue, out int consumedLength))
    {
      value = null;
      return false;
    }

    buffer.Remove(0, consumedLength);
    value = parsedValue;
    return true;
  }

  private async Task SendRDBFileAsync(NetworkStream stream, CancellationToken cancellationToken)
  {
    string hexData = "524544495330303131fa0972656469732d76657205372e322e30fa0a72656469732d62697473c040fa056374696d65c26d08bc65fa08757365642d6d656dc2b0c41000fa08616f662d62617365c000fff06e3bfec0ff5a62";
    byte[] rdbFileInBinary = Convert.FromHexString(hexData);

    string header = $"${rdbFileInBinary.Length}\r\n";
    byte[] headerBytes = Encoding.UTF8.GetBytes(header);

    await stream.WriteAsync(headerBytes, cancellationToken);
    await stream.WriteAsync(rdbFileInBinary, cancellationToken);
    await stream.FlushAsync(cancellationToken);
  }
}
