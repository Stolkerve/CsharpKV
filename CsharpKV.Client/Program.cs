using System.Net.Sockets;
using CsharpKV.Internal;

namespace CsharpKV.Client;

class Client {
    public async Task Start() {
        var client = new TcpClient("127.0.0.1", 6969);
        var stream = client.GetStream();

        var command = CommandEncoder.EncodeCommand(Command.PING, new List<CommandValue>());
        await stream.WriteAsync(command);

        var respondSizeBuff = new Byte[4];
        await stream.ReadAsync(respondSizeBuff, 0, respondSizeBuff.Length);
        var respondSize = CommandEncoder.DecodeLittleEndian(respondSizeBuff);
        var respondBuff = new byte[respondSize];
        await stream.ReadAsync(respondBuff);

        var respond = CommandEncoder.DecodeCommand(respondBuff);
        if (respond != null) {
            Console.WriteLine(respond.ToString());
        }
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
