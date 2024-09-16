using System.Net.Sockets;
using CsharpKV.Internal;

namespace CsharpKV.Client;

class Client {
    public async Task Start(string[] args) {
        if (args.Length < 1) {
            Console.WriteLine("expected one command");
            return;
        }

        var commandName = args[0];

        var client = new TcpClient("127.0.0.1", 6969);
        var stream = client.GetStream();

        switch (commandName.ToUpper()) {
        case "PING":
            var commandPing = CommandEncoder.EncodeCommand(Command.PING, new List<CommandValue>());
            await stream.WriteAsync(commandPing);
            break;
        case "GET":
            if (args.Length < 2) {
                Console.WriteLine("expected the key argument");
                return;
            }

            var getKey = args[1];
            if (getKey.All(char.IsDigit)) {
                Console.WriteLine("the key must by a string");
                return;
            }

            var commandGet = CommandEncoder.EncodeCommand(Command.GET, new List<CommandValue>(
                new CommandValue[]{new CommandValue(CommandValueType.STRING, getKey)}
            ));
            await stream.WriteAsync(commandGet);
            break;
        case "SET":
            if (args.Length < 3) {
                Console.WriteLine("expected the key and value argument");
                return;
            }

            var setKey = args[1];
            if (setKey.All(char.IsDigit)) {
                Console.WriteLine("the key must by a string");
                return;
            }

            var value = args[2];

            var commandSet = CommandEncoder.EncodeCommand(Command.SET, new List<CommandValue>(
                new CommandValue[]{new CommandValue(CommandValueType.STRING, setKey), new CommandValue(CommandValueType.STRING, value)}
            ));
            await stream.WriteAsync(commandSet);
            break;
        default:
        return;
        }

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
        await client.Start(args);
    }
}
