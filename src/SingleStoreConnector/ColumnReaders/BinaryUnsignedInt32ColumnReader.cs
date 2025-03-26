using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class BinaryUnsignedInt32ColumnReader : ColumnReader
{
	public static BinaryUnsignedInt32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		checked((int) DoReadValue(data));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint DoReadValue(ReadOnlySpan<byte> data) =>
		MemoryMarshal.Read<uint>(data);
}
