using System.Buffers.Text;
using System.Text;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class GuidChar36ColumnReader : ColumnReader
{
	public static GuidChar36ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		Utf8Parser.TryParse(data, out Guid guid, out int guid36BytesConsumed, 'D') && guid36BytesConsumed == 36 ?
			guid : throw new FormatException($"Could not parse CHAR(36) value as Guid: {Encoding.UTF8.GetString(data)}");
}
