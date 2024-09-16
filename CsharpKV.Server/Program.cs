using System.Net;
using System.Net.Sockets;
using CsharpKV.Internal;

namespace CsharpKV.Server;

class Server {
    public async Task Start() {
        try {
            
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6969);
            listener.Start();

            while (true) {
                var client = await listener.AcceptTcpClientAsync();

                _ = this.HandleNewConection(client);
            }
        } catch(Exception e) {
            Console.WriteLine("Execp: ", e.ToString());
        }
    }

    async Task HandleNewConection(TcpClient client) {
        Byte[] commandSizeBuffer = new Byte[4];
        Console.WriteLine($"Nueva coneccion desde: {client.Client.RemoteEndPoint}");
        var stream = client.GetStream();
        while (true) {
            for (int i = 0; i < commandSizeBuffer.Length; i++) {
                commandSizeBuffer[i] = 0;
            }
            stream.ReadTimeout = 1000;
            if (await stream.ReadAsync(commandSizeBuffer,0,commandSizeBuffer.Length) == 0) {
                return;
            }
            var commandSize = CommandEncoder.DecodeLittleEndian(commandSizeBuffer);
            var commandBuff = new byte[commandSize];
            if (await stream.ReadAsync(commandBuff,0,commandBuff.Length) == 0) {
                return;
            }
            try {
                CommandValue command = CommandEncoder.DecodeCommand(commandBuff);
                if (command.Type != CommandValueType.ARRAY) {
                    Console.WriteLine($"Cliente {client.Client.RemoteEndPoint} se desconecto");
                    var errBuff = CommandEncoder.EncodeCommandValue(new CommandValue(CommandValueType.STRING, "ERROR: expected array"));
                    await stream.WriteAsync(errBuff);
                    break;
                }

                var args = (List<CommandValue>)command.ValueExtractor!;
                var respondCommandBuff = CommandEncoder.EncodeCommandValue(new CommandValue(CommandValueType.STRING, "Pong"));
                await stream.WriteAsync(respondCommandBuff);
            } catch (Exception e) {
                Console.WriteLine(e);
                return;
            }
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
