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

                _ = this.HandleNewConection(client);
            }
        } catch(SocketException e) {
            Console.WriteLine(e);
        }
    }

    async Task HandleNewConection(TcpClient client) {
        Byte[] buffer = new Byte[1024];

        while (true) {
            var stream = client.GetStream();
            if (await stream.ReadAsync(buffer,0,buffer.Length) == 0) {
                break;
            }

            var data = Encoding.ASCII.GetString(buffer);

            Console.WriteLine(data);

            var msg = Encoding.ASCII.GetBytes("PONG");
            await stream.WriteAsync(msg);
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
