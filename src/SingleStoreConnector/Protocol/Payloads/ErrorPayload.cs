using System.Text;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Protocol.Payloads;

// See https://dev.mysql.com/doc/internals/en/packet-ERR_Packet.html
internal readonly struct ErrorPayload
{
	public int ErrorCode { get; }
	public string State { get; }
	public string Message { get; }

	public SingleStoreException ToException() => new SingleStoreException((SingleStoreErrorCode) ErrorCode, State, Message);

	public static ErrorPayload Create(ReadOnlySpan<byte> span)
	{
		var reader = new ByteArrayReader(span);
		reader.ReadByte(Signature);

		var errorCode = reader.ReadUInt16();
		var stateMarker = Encoding.ASCII.GetString(reader.ReadByteString(1));
		string state, message;
		if (stateMarker == "#")
		{
			state = Encoding.ASCII.GetString(reader.ReadByteString(5));
			message = Encoding.UTF8.GetString(reader.ReadByteString(span.Length - 9));
		}
		else
		{
			state = "HY000";
			message = stateMarker + Encoding.UTF8.GetString(reader.ReadByteString(span.Length - 4));
		}
		return new ErrorPayload(errorCode, state, message);
	}

	public const byte Signature = 0xFF;

	private ErrorPayload(int errorCode, string state, string message)
	{
		ErrorCode = errorCode;
		State = state;
		Message = message;
	}
}
