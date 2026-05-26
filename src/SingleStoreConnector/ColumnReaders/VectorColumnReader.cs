using System.Buffers.Binary;
using System.Runtime.InteropServices;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.ColumnReaders;

internal abstract class VectorColumnReaderBase : ColumnReader
{
	protected static void ValidateLength(ColumnDefinitionPayload columnDefinition, int dataLength, int elementSize, string elementTypeName)
	{
		if (dataLength % elementSize != 0)
		{
			throw new FormatException(
				$"Expected VECTOR({elementTypeName}) payload length to be a multiple of {elementSize}, but got {dataLength}.");
		}

		if (columnDefinition.VectorDimensions is { } dimensions)
		{
			var expectedLength = checked((ulong) dimensions * (ulong) elementSize);
			if ((ulong) dataLength != expectedLength)
			{
				throw new FormatException(
					$"Expected VECTOR({dimensions}, {elementTypeName}) payload length to be {expectedLength} bytes, but got {dataLength}.");
			}
		}
	}
}

internal sealed class VectorInt8ColumnReader : VectorColumnReaderBase
{
	public static VectorInt8ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		ValidateLength(columnDefinition, data.Length, sizeof(sbyte), "I8");
		return new ReadOnlyMemory<sbyte>(MemoryMarshal.Cast<byte, sbyte>(data).ToArray());
	}
}

internal sealed class VectorInt16ColumnReader : VectorColumnReaderBase
{
	public static VectorInt16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		ValidateLength(columnDefinition, data.Length, sizeof(short), "I16");

		if (BitConverter.IsLittleEndian)
			return new ReadOnlyMemory<short>(MemoryMarshal.Cast<byte, short>(data).ToArray());

		var values = new short[data.Length / sizeof(short)];
		for (var i = 0; i < values.Length; i++)
			values[i] = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(i * sizeof(short), sizeof(short)));

		return new ReadOnlyMemory<short>(values);
	}
}

internal sealed class VectorInt32ColumnReader : VectorColumnReaderBase
{
	public static VectorInt32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		ValidateLength(columnDefinition, data.Length, sizeof(int), "I32");

		if (BitConverter.IsLittleEndian)
			return new ReadOnlyMemory<int>(MemoryMarshal.Cast<byte, int>(data).ToArray());

		var values = new int[data.Length / sizeof(int)];
		for (var i = 0; i < values.Length; i++)
			values[i] = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(i * sizeof(int), sizeof(int)));

		return new ReadOnlyMemory<int>(values);
	}
}

internal sealed class VectorInt64ColumnReader : VectorColumnReaderBase
{
	public static VectorInt64ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		ValidateLength(columnDefinition, data.Length, sizeof(long), "I64");

		if (BitConverter.IsLittleEndian)
			return new ReadOnlyMemory<long>(MemoryMarshal.Cast<byte, long>(data).ToArray());

		var values = new long[data.Length / sizeof(long)];
		for (var i = 0; i < values.Length; i++)
			values[i] = BinaryPrimitives.ReadInt64LittleEndian(data.Slice(i * sizeof(long), sizeof(long)));

		return new ReadOnlyMemory<long>(values);
	}
}

internal sealed class VectorFloat32ColumnReader : VectorColumnReaderBase
{
	public static VectorFloat32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		ValidateLength(columnDefinition, data.Length, sizeof(float), "F32");

		if (BitConverter.IsLittleEndian)
			return new ReadOnlyMemory<float>(MemoryMarshal.Cast<byte, float>(data).ToArray());

		var values = new float[data.Length / sizeof(float)];

#if NET5_0_OR_GREATER
		for (var i = 0; i < values.Length; i++)
			values[i] = BinaryPrimitives.ReadSingleLittleEndian(data.Slice(i * sizeof(float), sizeof(float)));
#else
		var bytes = data.ToArray();
		for (var i = 0; i < values.Length; i++)
		{
			Array.Reverse(bytes, i * sizeof(float), sizeof(float));
			values[i] = BitConverter.ToSingle(bytes, i * sizeof(float));
		}
#endif

		return new ReadOnlyMemory<float>(values);
	}
}

internal sealed class VectorFloat64ColumnReader : VectorColumnReaderBase
{
	public static VectorFloat64ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		ValidateLength(columnDefinition, data.Length, sizeof(double), "F64");

		if (BitConverter.IsLittleEndian)
			return new ReadOnlyMemory<double>(MemoryMarshal.Cast<byte, double>(data).ToArray());

		var values = new double[data.Length / sizeof(double)];

#if NET5_0_OR_GREATER
		for (var i = 0; i < values.Length; i++)
			values[i] = BinaryPrimitives.ReadDoubleLittleEndian(data.Slice(i * sizeof(double), sizeof(double)));
#else
		var bytes = data.ToArray();
		for (var i = 0; i < values.Length; i++)
		{
			Array.Reverse(bytes, i * sizeof(double), sizeof(double));
			values[i] = BitConverter.ToDouble(bytes, i * sizeof(double));
		}
#endif

		return new ReadOnlyMemory<double>(values);
	}
}
