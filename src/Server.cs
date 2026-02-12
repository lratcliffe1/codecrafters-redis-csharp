using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Resp;

TcpListener? server = null;
CancellationTokenSource shutdownTokenSource = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
  eventArgs.Cancel = true;
  shutdownTokenSource.Cancel();
};

try
{
  server = new TcpListener(IPAddress.Any, 6379);
  server.Start();

  while (!shutdownTokenSource.Token.IsCancellationRequested)
  {
    TcpClient client = await server.AcceptTcpClientAsync(shutdownTokenSource.Token);
    _ = HandleClientAsync(client, shutdownTokenSource.Token);
  }
}
catch (OperationCanceledException)
{
  // Graceful shutdown.
}
catch (SocketException e)
{
  Console.WriteLine("SocketException: {0}", e);
}
finally
{
  server?.Stop();
  shutdownTokenSource.Dispose();
}

static async Task HandleClientAsync(TcpClient client, CancellationToken serverCancellationToken)
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
        string response = await CommandEventLoop.ExecuteAsync(value!, cancellationToken);
        byte[] message = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(message, cancellationToken);
        await stream.FlushAsync(cancellationToken);
      }
    }
  }
  catch (OperationCanceledException)
  {
    // Connection was terminated.
  }
  catch (InvalidOperationException exception)
  {
    string error = $"-ERR protocol error: {exception.Message}\r\n";
    byte[] message = Encoding.UTF8.GetBytes(error);
    await stream.WriteAsync(message, serverCancellationToken);
    await stream.FlushAsync(serverCancellationToken);
  }
}

static bool TryReadNextCommand(StringBuilder buffer, out RespValue? value)
{
  string data = buffer.ToString();
  if (!RespParser.TryParse(data, out RespValue parsedValue, out int consumedLength))
  {
    value = null;
    return false;
  }

  buffer.Remove(0, consumedLength);
  value = parsedValue;
  return true;
}
