using System.Runtime.CompilerServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class TextBooleanColumnReader : ColumnReader
{
	public static TextBooleanColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data) ? 1 : 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool DoReadValue(ReadOnlySpan<byte> data) =>
		data[0] != (byte) '0';
}
