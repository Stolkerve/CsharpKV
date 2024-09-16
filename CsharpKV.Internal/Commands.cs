using System.Text;
using System.Text.Json;

namespace CsharpKV.Internal;

public enum Commands {
	PING,
	GET,
	SET,
}

public class CommandsEncoder
{
	public static byte[] EncodeCommand(Commands command, List<object> args) {
		switch (command) {
		case Commands.PING:
			if (args.Count() != 0) {
				throw new Exception("none arguments are expected");
			}
			args.Insert(0, "PING");
			break;
		case Commands.GET:
			if (args.Count() != 1) {
				throw new Exception("only one arguments are expected");
			}
			args.Insert(0, "GET");
			break;
		case Commands.SET:
			if (args.Count() != 2) {
				throw new Exception("only two arguments are expected");
			}
			args.Insert(0, "SET");
			break;
		}

		var jsonBuff = JsonSerializer.Serialize<List<object>>(args);

		var sizeBuff = new byte[4];
		sizeBuff[0] = (byte)((jsonBuff.Count() >> 0) & 0xFF);
		sizeBuff[1] = (byte)((jsonBuff.Count() >> 8) & 0xFF);
		sizeBuff[2] = (byte)((jsonBuff.Count() >> 16) & 0xFF);
		sizeBuff[3] = (byte)((jsonBuff.Count() >> 24) & 0xFF);
		var sizeStr = Encoding.ASCII.GetString(sizeBuff);
		
		return Encoding.ASCII.GetBytes(sizeStr + jsonBuff);
	}

	public static int DecodeLittleEndian(byte[] buff) {
		int n = 0;

		n |= (int)buff[0];
		n |= ((int)buff[1]) << 8;
		n |= ((int)buff[2]) << 16;
		n |= ((int)buff[3]) << 24;

		return n;
	}

	public static List<object>? DecodeCommand(byte[] buff) {
		return JsonSerializer.Deserialize<List<object>>(buff);
	}
}
