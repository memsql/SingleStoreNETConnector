using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector.Protocol.Payloads;

internal readonly struct AuthenticationMoreDataPayload
{
	public byte[] Data { get; }

	public const byte Signature = 0x01;

	public static AuthenticationMoreDataPayload Create(ReadOnlySpan<byte> span)
	{
		var reader = new ByteArrayReader(span);
		reader.ReadByte(Signature);
		return new AuthenticationMoreDataPayload(reader.ReadByteString(reader.BytesRemaining).ToArray());
	}

	private AuthenticationMoreDataPayload(byte[] data) => Data = data;
}
