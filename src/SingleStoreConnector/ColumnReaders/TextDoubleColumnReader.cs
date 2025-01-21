using System.Buffers.Text;
using System.Text;
using SingleStoreConnector.Protocol.Payloads;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.ColumnReaders;

internal sealed class TextDoubleColumnReader : ColumnReader
{
	public static TextDoubleColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (Utf8Parser.TryParse(data, out double doubleValue, out var doubleBytesConsumed) && doubleBytesConsumed == data.Length)
			return doubleValue;
		if (data.SequenceEqual("-inf"u8))
			return double.NegativeInfinity;
		if (data.SequenceEqual("inf"u8))
			return double.PositiveInfinity;
		if (data.SequenceEqual("nan"u8))
			return double.NaN;
		throw new FormatException($"Couldn't parse value as double: {Encoding.UTF8.GetString(data)}");
	}
}
