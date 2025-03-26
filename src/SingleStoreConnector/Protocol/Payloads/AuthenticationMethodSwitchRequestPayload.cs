using System.Text;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Protocol.Payloads;

internal readonly struct AuthenticationMethodSwitchRequestPayload
{
	public string Name { get; }
	public byte[] Data { get; }

	public const byte Signature = 0xFE;

	public static AuthenticationMethodSwitchRequestPayload Create(ReadOnlySpan<byte> span)
	{
		var reader = new ByteArrayReader(span);
		reader.ReadByte(Signature);
		string name;
		byte[] data;
		if (span.Length == 1)
		{
			// if the packet is just the header byte (0xFE), it's an "Old Authentication Method Switch Request Packet"
			// (possibly sent by a server that doesn't support CLIENT_PLUGIN_AUTH)
			name = "mysql_old_password";
			data = [];
		}
		else
		{
			name = Encoding.UTF8.GetString(reader.ReadNullTerminatedByteString());
			data = reader.ReadByteString(reader.BytesRemaining).ToArray();
		}
		return new AuthenticationMethodSwitchRequestPayload(name, data);
	}

	private AuthenticationMethodSwitchRequestPayload(string name, byte[] data)
	{
		Name = name;
		Data = data;
	}
}
