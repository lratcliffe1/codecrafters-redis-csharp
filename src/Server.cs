using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.src.Resp;

TcpListener? server = null;

var DATABASE = new Dictionary<string, string>();

try
{
  server = new TcpListener(IPAddress.Any, 6379);
  server.Start();

  while (true)
  {
    TcpClient client = server.AcceptTcpClient();
    Thread clientThread = new(() => HandleClient(client, DATABASE));
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

static void HandleClient(TcpClient client, Dictionary<string, string> DATABASE)
{
  using TcpClient _ = client;
  byte[] bytes = new byte[256];
  string? data = null;
  NetworkStream stream = client.GetStream();

  int i;
  while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
  {
    data = Encoding.ASCII.GetString(bytes, 0, i);

    RespValue value = RespParser.Parse(data);
    string response = RespExecutor.Execute(value);

    Console.Error.WriteLine(response);

    byte[] msg = Encoding.UTF8.GetBytes(response);
    stream.Write(msg, 0, msg.Length);
    stream.Flush();
  }
}
