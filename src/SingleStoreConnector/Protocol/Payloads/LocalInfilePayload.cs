using System.Text;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Protocol.Payloads;

// See https://dev.mysql.com/doc/internals/en/com-query-response.html#local-infile-request
internal readonly struct LocalInfilePayload
{
	public const byte Signature = 0xFB;

	public string FileName { get; }

	public static LocalInfilePayload Create(ReadOnlySpan<byte> span)
	{
		var reader = new ByteArrayReader(span);
		reader.ReadByte(Signature);
		var fileName = Encoding.UTF8.GetString(reader.ReadByteString(reader.BytesRemaining));
		return new LocalInfilePayload(fileName);
	}

	private LocalInfilePayload(string fileName)
	{
		FileName = fileName;
	}
}
