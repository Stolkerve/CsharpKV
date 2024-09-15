using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server;

class Server {
    public async Task Start() {
        try {
            
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6969);
            listener.Start();

            while (true) {
                var client = await listener.AcceptTcpClientAsync();
                Byte[] buffer = new Byte[1024];

                var stream = client.GetStream();
                await stream.ReadAsync(buffer,0,buffer.Length);

                var data = Encoding.ASCII.GetString(buffer);

                Console.WriteLine(data);

                var msg = Encoding.ASCII.GetBytes("PONG");
                await stream.WriteAsync(msg);
            }
        } catch(SocketException e) {
            Console.WriteLine(e);
        }
    }

}

class Program
{
    static async Task Main(string[] args)
    {
        var server = new Server();
        await server.Start();
    }
}
