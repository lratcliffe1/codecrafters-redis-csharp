using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Bootstrap;
using codecrafters_redis.src.Helpers;
using codecrafters_redis.src.Replication;
using codecrafters_redis.src.Resp;

namespace codecrafters_redis.src.Hosting;

public interface IClientHandler
{
  Task HandleClientAsync(TcpClient client, long clientId, CancellationToken serverCancellationToken, bool suppressResponse = false);
}

public sealed class ClientHandler(
  ICommandEventLoop commandEventLoop,
  IRespParser respParser,
  IServerOptions serverOptions,
  IReplicaConnectionRegistry replicaConnectionRegistry,
  IClientConnectionRegistry clientConnectionRegistry) : IClientHandler
{
  private static readonly HashSet<string> WriteCommands = new(StringComparer.Ordinal)
  {
    "SET",
    "DEL",
    "INCR",
    "LPUSH",
    "RPUSH",
    "LPOP",
    "BLPOP",
    "XADD",
  };

  public async Task HandleClientAsync(TcpClient client, long clientId, CancellationToken serverCancellationToken, bool suppressResponse = false)
  {
    using TcpClient ownedClient = client;
    using CancellationTokenSource connectionCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);
    CancellationToken cancellationToken = connectionCancellationTokenSource.Token;

    byte[] bytes = new byte[4096];
    StringBuilder incomingBuffer = new();
    NetworkStream stream = client.GetStream();
    clientConnectionRegistry.Register(clientId, stream);
    bool shouldTerminateConnection = false;

    try
    {
      while (!shouldTerminateConnection)
      {
        int bytesRead = await stream.ReadAsync(bytes, cancellationToken);
        if (bytesRead == 0)
        {
          break;
        }

        incomingBuffer.Append(Encoding.ASCII.GetString(bytes, 0, bytesRead));

        while (TryReadNextCommand(incomingBuffer, out RespValue? value, out int consumedLength))
        {
          if (value == null)
          {
            continue;
          }

          TryReadCommandName(value, out string command);
          string response = await commandEventLoop.ExecuteAsync(value, clientId, serverOptions.Port, cancellationToken);

          if (suppressResponse)
          {
            serverOptions.AddAckBytes(consumedLength);
          }

          if (ShouldSendResponse(suppressResponse, command, value))
          {
            if (!await clientConnectionRegistry.TryWriteAsync(clientId, response, cancellationToken))
            {
              shouldTerminateConnection = true;
              break;
            }
          }

          if (IsFullResyncResponse(response))
          {
            await SendRDBFileAsync(stream, cancellationToken);
            replicaConnectionRegistry.RegisterReplica(clientId, stream);
            continue;
          }

          if (suppressResponse || response.StartsWith("-ERR", StringComparison.Ordinal))
          {
            continue;
          }

          if (IsWriteCommand(command))
          {
            string encodedCommand = RespEncoder.Encode(value);
            await replicaConnectionRegistry.PropagateAsync(encodedCommand, cancellationToken);
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
      if (!suppressResponse)
      {
        string error = CommandHelper.BuildError($"protocol error: {exception.Message}");
        await clientConnectionRegistry.TryWriteAsync(clientId, error, serverCancellationToken);
      }
    }
    finally
    {
      replicaConnectionRegistry.UnregisterReplica(clientId);
      clientConnectionRegistry.Unregister(clientId);
      await commandEventLoop.NotifyClientDisconnectedAsync(clientId, serverOptions.Port);
    }
  }

  private bool TryReadNextCommand(StringBuilder buffer, out RespValue? value, out int consumedLength)
  {
    string data = buffer.ToString();
    if (!respParser.TryParse(data, out RespValue parsedValue, out consumedLength))
    {
      value = null;
      return false;
    }

    buffer.Remove(0, consumedLength);
    value = parsedValue;
    return true;
  }

  private static async Task SendRDBFileAsync(NetworkStream stream, CancellationToken cancellationToken)
  {
    string hexData = "524544495330303131fa0972656469732d76657205372e322e30fa0a72656469732d62697473c040fa056374696d65c26d08bc65fa08757365642d6d656dc2b0c41000fa08616f662d62617365c000fff06e3bfec0ff5a62";
    byte[] rdbFileInBinary = Convert.FromHexString(hexData);

    string header = $"${rdbFileInBinary.Length}\r\n";
    byte[] headerBytes = Encoding.UTF8.GetBytes(header);

    await stream.WriteAsync(headerBytes, cancellationToken);
    await stream.WriteAsync(rdbFileInBinary, cancellationToken);
    await stream.FlushAsync(cancellationToken);
  }

  private static bool TryReadCommandName(RespValue value, out string command)
  {
    command = string.Empty;
    if (value.Type != RespType.Array || value.ArrayValue == null || value.ArrayValue.Count == 0)
    {
      return false;
    }

    command = value.ArrayValue[0].ToString().ToUpperInvariant();
    return !string.IsNullOrWhiteSpace(command);
  }

  private static bool IsWriteCommand(string command)
  {
    return WriteCommands.Contains(command);
  }

  private static bool ShouldSendResponse(bool suppressResponse, string command, RespValue value)
  {
    if (!suppressResponse)
    {
      return !IsAckResponse(command, value);
    }

    return IsGetAckRequest(command, value);
  }

  private static bool IsGetAckRequest(string command, RespValue value)
  {
    if (!string.Equals(command, "REPLCONF", StringComparison.Ordinal) || value.ArrayValue == null || value.ArrayValue.Count < 2)
    {
      return false;
    }

    return string.Equals(value.ArrayValue[1].ToString(), "GETACK", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsAckResponse(string command, RespValue value)
  {
    if (!string.Equals(command, "REPLCONF", StringComparison.Ordinal) || value.ArrayValue == null || value.ArrayValue.Count < 2)
    {
      return false;
    }

    return string.Equals(value.ArrayValue[1].ToString(), "ACK", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsFullResyncResponse(string response)
  {
    return response.StartsWith("+FULLRESYNC", StringComparison.Ordinal);
  }
}
