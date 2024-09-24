using System.Net;
using System.Collections.Concurrent;
using System.Net.Sockets;
using CsharpKV.Internal;

namespace CsharpKV.Server;

class Server {
    ConcurrentDictionary<string, CommandValue> Cache = new();

    public async Task Start() {
        try {
            
            var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 6969);
            listener.Start();

            while (true) {
                var client = await listener.AcceptTcpClientAsync();

                _ = this.HandleNewConection(client);
            }
        } catch(Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    async Task SendErrMsg(NetworkStream stream, string msg) {
        var _errBuff = CommandEncoder.EncodeCommandValue(new CommandValue(CommandValueType.STRING, $"ERROR: {msg}"));
        await stream.WriteAsync(_errBuff);
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
                    await SendErrMsg(stream, "expected array");
                    return;
                }
                // Console.WriteLine($"Argumentos recibidos: {command.ToString()}");

                var args = (List<CommandValue>)command.Value!;
                if (args.Count() == 0) {
                    await SendErrMsg(stream, "expected array with arguments");
                    return;
                }

                if (args[0].Type != CommandValueType.STRING) {
                    await SendErrMsg(stream, "expected command name as string");
                    return;
                }
                var commandName = (string)args[0].Value!;

                switch (commandName) {
                case "PING":
                    var pongRespondCommandBuff = CommandEncoder.EncodeCommandValue(new CommandValue(CommandValueType.STRING, "Pong"));
                    await stream.WriteAsync(pongRespondCommandBuff);
                    break;
                case "GET":
                    if (args.Count() != 2) {
                        await SendErrMsg(stream, $"expected 1 arguments got {args.Count()}");
                        return;
                    }
                    if (args[1].Type != CommandValueType.STRING) {
                        await SendErrMsg(stream, "expected key argument as string");
                        return;
                    }
                    var getKeyStr = (String)args[1].Value!;
                    CommandValue value;
                    if (Cache.TryGetValue(getKeyStr, out value!)) {
                        var getRespondCommandBuff = CommandEncoder.EncodeCommandValue(value);
                        await stream.WriteAsync(getRespondCommandBuff);
                    }
                    await stream.WriteAsync(CommandEncoder.EncodeCommandValue(new CommandValue(CommandValueType.NULL, "null")));
                    break;
                case "SET":
                    if (args.Count() != 3) {
                        await SendErrMsg(stream, $"expected 2 arguments got {args.Count()}");
                        return;
                    }
                    if (args[1].Type != CommandValueType.STRING) {
                        await SendErrMsg(stream, "expected key argument as string");
                        return;
                    }
                    var setKeyStr = (String)args[1].Value!;
                    var setValue = args[2];
                    Cache[setKeyStr] = setValue;
                    var setRespondCommandBuff = CommandEncoder.EncodeCommandValue(new CommandValue(CommandValueType.STRING, "ok"));
                    await stream.WriteAsync(setRespondCommandBuff);
                    break;
                default:
                    await SendErrMsg(stream, $"invalid command {commandName}");
                    return;
                }

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
