using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class BinarySignedInt16ColumnReader : ColumnReader
{
	public static BinarySignedInt16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static short DoReadValue(ReadOnlySpan<byte> data) =>
		MemoryMarshal.Read<short>(data);
}
