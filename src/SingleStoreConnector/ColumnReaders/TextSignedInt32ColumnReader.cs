using System.Buffers.Text;
using System.Runtime.CompilerServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class TextSignedInt32ColumnReader : ColumnReader
{
	public static TextSignedInt32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int DoReadValue(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
}
