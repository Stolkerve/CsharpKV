using System.Text;
using System.Text.Json;

namespace CsharpKV.Internal;

public enum CommandValueType {
	INT,
	STRING,
	NULL,
	ARRAY,
}

public enum Command {
	PING,
	GET,
	SET,
}

public class CommandValue {
	public CommandValue() {
	}
	public CommandValue(CommandValueType t, object v) {
		if (t == CommandValueType.INT && !(v is int)) {
			throw new Exception("int value expected an int value");
		} else if (t == CommandValueType.STRING && !(v is string)) {
			throw new Exception("int value expected a string value");
		} else if (t == CommandValueType.ARRAY && !(v is List<CommandValue>)) {
			throw new Exception("int value expected a list of CommandValue");
		}
		this.Type = t;
		this.Value = v;
	}

	public CommandValueType Type { get; set; }

	public Object? Value { get;set; }

	public override string ToString() {
		switch (this.Type) {
		case CommandValueType.ARRAY:
			var arr = (List<CommandValue>)this.Value!;
			return "[" + string.Join(", ",  arr) + "]";
		case CommandValueType.INT:
			return ((int)this.Value!).ToString();
		case CommandValueType.STRING:
			return (string)this.Value!;
		case CommandValueType.NULL:
			return "null";
		}
		return "";
	}

	public static void CastJsonTypesToNormalTypes (CommandValue c)  {
			switch (c.Type) {
			case CommandValueType.ARRAY:
				c.Value = ((JsonElement)c.Value!).Deserialize<List<CommandValue>>()!;
				var value = (List<CommandValue>)c.Value; 
				for (int i = 0; i < value.Count(); i++) {
					CastJsonTypesToNormalTypes(value[i]);
				}
				break;
			case CommandValueType.INT:
				c.Value = ((JsonElement)c.Value!).GetInt32();
				break;
			case CommandValueType.STRING:
				c.Value =((JsonElement)c.Value!).GetString();
				break;
			case CommandValueType.NULL:
				c.Value = null;
				break;
			}
	}
}

public class CommandEncoder
{
	public static byte[] EncodeCommand(Command command, List<CommandValue> args) {
		switch (command) {
		case Command.PING:
			if (args.Count() != 0) {
				throw new Exception("none arguments are expected");
			}
			args.Insert(0, new CommandValue(CommandValueType.STRING, "PING"));
			break;
		case Command.GET:
			if (args.Count() != 1) {
				throw new Exception("only one arguments are expected");
			}
			args.Insert(0, new CommandValue(CommandValueType.STRING, "GET"));
			break;
		case Command.SET:
			if (args.Count() != 2) {
				throw new Exception("only two arguments are expected");
			}
			args.Insert(0, new CommandValue(CommandValueType.STRING, "SET"));
			break;
		}
		var commandValue = new CommandValue(CommandValueType.ARRAY, args);

		var jsonBuff = JsonSerializer.Serialize<CommandValue>(commandValue);

		var sizeBuff = new byte[4];
		sizeBuff[0] = (byte)(jsonBuff.Count() & 0xFF);
		sizeBuff[1] = (byte)((jsonBuff.Count() >> 8) & 0xFF);
		sizeBuff[2] = (byte)((jsonBuff.Count() >> 16) & 0xFF);
		sizeBuff[3] = (byte)((jsonBuff.Count() >> 24) & 0xFF);
		var sizeStr = Encoding.ASCII.GetString(sizeBuff);
		
		return Encoding.ASCII.GetBytes(sizeStr + jsonBuff);
	}

	public static byte[] EncodeCommandValue(CommandValue v) {
		var jsonBuff = JsonSerializer.Serialize<CommandValue>(v);

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

	public static CommandValue DecodeCommand(byte[] buff) {
		var cmd = JsonSerializer.Deserialize<CommandValue>(buff)!;

		var castJsonTypesToNormalTypes = (CommandValue c) => {
			switch (c.Type) {
			case CommandValueType.ARRAY:
				c.Value = ((JsonElement)c.Value!).Deserialize<List<CommandValue>>()!;
				var value = (List<CommandValue>)c.Value; 
				for (int i = 0; i < value.Count(); i++) {
					CommandValue.CastJsonTypesToNormalTypes(value[i]);
				}
				break;
			case CommandValueType.INT:
				c.Value = ((JsonElement)c.Value!).GetInt32();
				break;
			case CommandValueType.STRING:
				c.Value =((JsonElement)c.Value!).GetString();
				break;
			case CommandValueType.NULL:
				c.Value = null;
				break;
			}
		}; 

		castJsonTypesToNormalTypes(cmd);

		return cmd;
	}
}
