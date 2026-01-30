using System.Net;
using System.Net.Sockets;
using System.Text;

TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

while (true)
{
  Socket client = server.AcceptSocket();
  client.Send(Encoding.UTF8.GetBytes("+PONG\r\n"));
  client.Shutdown(SocketShutdown.Both);
  client.Close();
}