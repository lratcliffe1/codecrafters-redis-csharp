using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener? server = null;

try
{
  server = new TcpListener(IPAddress.Any, 6379);
  server.Start();

  byte[] bytes = new byte[256];
  string? data = null;

  while (true)
  {
    using TcpClient client = server.AcceptTcpClient();

    data = null;

    NetworkStream stream = client.GetStream();

    int i;
    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
    {
      data = Encoding.ASCII.GetString(bytes, 0, i);

      byte[] msg = Encoding.UTF8.GetBytes("+PONG\r\n");
      stream.Write(msg, 0, msg.Length);
      stream.Flush();
    }
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