using System.Net.Sockets;
using System.Text;

namespace CsharpKV.Client;

class Client {
    public async Task Start() {
        var client = new TcpClient("127.0.0.1", 6969);
        var stream = client.GetStream();
        var msg = Encoding.ASCII.GetBytes("PING");
        await stream.WriteAsync(msg);

        var readBuff = new Byte[1024];
        await stream.ReadAsync(readBuff, 0, readBuff.Length);
        var data = Encoding.ASCII.GetString(readBuff);

        Console.WriteLine(data);
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var client = new Client();
        await client.Start();
    }
}
