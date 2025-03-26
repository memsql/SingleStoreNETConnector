using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class BinaryYearColumnReader : ColumnReader
{
	public static BinaryYearColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int DoReadValue(ReadOnlySpan<byte> data) =>
		MemoryMarshal.Read<short>(data);
}
