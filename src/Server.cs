using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener? server = null;

try
{
  server = new TcpListener(IPAddress.Any, 6379);
  server.Start();

  while (true)
  {
    TcpClient client = server.AcceptTcpClient();
    Thread clientThread = new(() => HandleClient(client));
    clientThread.Start();
  }
}
catch (SocketException e)
{
  Console.WriteLine("SocketException: {0}", e);
}
finally
{
  server?.Stop();
}

static void HandleClient(TcpClient client)
{
  using TcpClient _ = client;
  byte[] bytes = new byte[256];
  string? data = null;
  NetworkStream stream = client.GetStream();

  int i;
  while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
  {
    data = Encoding.ASCII.GetString(bytes, 0, i);

    Console.Error.WriteLine(data);

    byte[] msg = Encoding.UTF8.GetBytes("+PONG\r\n");
    stream.Write(msg, 0, msg.Length);
    stream.Flush();
  }
}

// *2\r\n$4\r\nECHO\r\n$3\r\nhey\r\n
static List<string> ParseRESP(string data)
{
  int numberOfComands = int.Parse(data[1..].Split("\\").First());

  Console.Error.WriteLine(numberOfComands);

  return [];
}