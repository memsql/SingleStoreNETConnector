using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class TextTimeColumnReader : ColumnReader
{
	public static TextTimeColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		Utility.ParseTimeSpan(data);
}
