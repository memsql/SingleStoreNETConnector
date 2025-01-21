using System.Buffers.Text;
using System.Text;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class GuidChar32ColumnReader : ColumnReader
{
	public static GuidChar32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		Utf8Parser.TryParse(data, out Guid guid, out int guid32BytesConsumed, 'N') && guid32BytesConsumed == 32 ?
			guid : throw new FormatException($"Could not parse CHAR(32) value as Guid: {Encoding.UTF8.GetString(data)}");
}
