using System.Runtime.CompilerServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class BinarySignedInt8ColumnReader : ColumnReader
{
	public static BinarySignedInt8ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static sbyte DoReadValue(ReadOnlySpan<byte> data) =>
		(sbyte) data[0];
}
