using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace SingleStoreConnector.Core;

internal static class SingleStoreBinaryValueConverter
{
	public static bool TryInferSpecialSingleStoreDbType(object value, out SingleStoreDbType dbType)
	{
		// Use explicit type checks instead of pattern matching to avoid byte[]/sbyte[] confusion
		var type = value.GetType();

		// byte[] and related types should NOT infer as Vector - they use normal Blob type mapping
		if (type == typeof(byte[]) ||
			type == typeof(ReadOnlyMemory<byte>) ||
			type == typeof(Memory<byte>) ||
			type == typeof(ArraySegment<byte>) ||
			value is MemoryStream)
		{
			dbType = default;
			return false;
		}

		// Numeric array types infer as Vector
		if (type == typeof(float[]) || type == typeof(ReadOnlyMemory<float>) || type == typeof(Memory<float>) ||
			type == typeof(double[]) || type == typeof(ReadOnlyMemory<double>) || type == typeof(Memory<double>) ||
			type == typeof(sbyte[]) || type == typeof(ReadOnlyMemory<sbyte>) || type == typeof(Memory<sbyte>) ||
			type == typeof(short[]) || type == typeof(ReadOnlyMemory<short>) || type == typeof(Memory<short>) ||
			type == typeof(int[]) || type == typeof(ReadOnlyMemory<int>) || type == typeof(Memory<int>) ||
			type == typeof(long[]) || type == typeof(ReadOnlyMemory<long>) || type == typeof(Memory<long>))
		{
			dbType = SingleStoreDbType.Vector;
			return true;
		}

		dbType = default;
		return false;
	}

	public static ReadOnlySpan<byte> GetBsonBytes(object value) =>
		GetRawBytes(value, SingleStoreDbType.Bson);

	public static ReadOnlySpan<byte> GetVectorBytes(object value) =>
		value switch
		{
			float[] x => ConvertFloatsToBytes(x.AsSpan()),
			ReadOnlyMemory<float> x => ConvertFloatsToBytes(x.Span),
			Memory<float> x => ConvertFloatsToBytes(x.Span),

			double[] x => ConvertDoublesToBytes(x.AsSpan()),
			ReadOnlyMemory<double> x => ConvertDoublesToBytes(x.Span),
			Memory<double> x => ConvertDoublesToBytes(x.Span),

			sbyte[] x => MemoryMarshal.AsBytes<sbyte>(x.AsSpan()),
			ReadOnlyMemory<sbyte> x => MemoryMarshal.AsBytes(x.Span),
			Memory<sbyte> x => MemoryMarshal.AsBytes(x.Span),

			short[] x => ConvertInt16ToBytes(x.AsSpan()),
			ReadOnlyMemory<short> x => ConvertInt16ToBytes(x.Span),
			Memory<short> x => ConvertInt16ToBytes(x.Span),

			int[] x => ConvertInt32ToBytes(x.AsSpan()),
			ReadOnlyMemory<int> x => ConvertInt32ToBytes(x.Span),
			Memory<int> x => ConvertInt32ToBytes(x.Span),

			long[] x => ConvertInt64ToBytes(x.AsSpan()),
			ReadOnlyMemory<long> x => ConvertInt64ToBytes(x.Span),
			Memory<long> x => ConvertInt64ToBytes(x.Span),

			byte[] or ReadOnlyMemory<byte> or Memory<byte> or ArraySegment<byte> or MemoryStream
				=> GetRawBytes(value, SingleStoreDbType.Vector),

			_ => throw new NotSupportedException(
				$"Parameter type {value.GetType().Name} is not supported for SingleStoreDbType.Vector."),
		};

	public static ReadOnlySpan<byte> ConvertFloatsToBytes(ReadOnlySpan<float> values)
	{
		if (BitConverter.IsLittleEndian)
		{
			return MemoryMarshal.AsBytes(values);
		}
		else
		{
			// for big-endian platforms, we need to convert each float individually
			var bytes = new byte[values.Length * 4];

			for (var i = 0; i < values.Length; i++)
			{
#if NET5_0_OR_GREATER
				BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), values[i]);
#else
				var floatBytes = BitConverter.GetBytes(values[i]);
				Array.Reverse(floatBytes);
				floatBytes.CopyTo(bytes, i * 4);
#endif
			}

			return bytes;
		}
	}

	private static ReadOnlySpan<byte> ConvertDoublesToBytes(ReadOnlySpan<double> values)
	{
		if (BitConverter.IsLittleEndian)
			return MemoryMarshal.AsBytes(values);

		var bytes = new byte[values.Length * sizeof(double)];
		for (var i = 0; i < values.Length; i++)
		{
			var valueBytes = BitConverter.GetBytes(values[i]);
			Array.Reverse(valueBytes);
			valueBytes.CopyTo(bytes, i * sizeof(double));
		}
		return bytes;
	}

	private static ReadOnlySpan<byte> ConvertInt16ToBytes(ReadOnlySpan<short> values)
	{
		if (BitConverter.IsLittleEndian)
			return MemoryMarshal.AsBytes(values);

		var bytes = new byte[values.Length * sizeof(short)];
		for (var i = 0; i < values.Length; i++)
			BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i * sizeof(short), sizeof(short)), values[i]);
		return bytes;
	}

	private static ReadOnlySpan<byte> ConvertInt32ToBytes(ReadOnlySpan<int> values)
	{
		if (BitConverter.IsLittleEndian)
			return MemoryMarshal.AsBytes(values);

		var bytes = new byte[values.Length * sizeof(int)];
		for (var i = 0; i < values.Length; i++)
			BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * sizeof(int), sizeof(int)), values[i]);
		return bytes;
	}

	private static ReadOnlySpan<byte> ConvertInt64ToBytes(ReadOnlySpan<long> values)
	{
		if (BitConverter.IsLittleEndian)
			return MemoryMarshal.AsBytes(values);

		var bytes = new byte[values.Length * sizeof(long)];
		for (var i = 0; i < values.Length; i++)
			BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(i * sizeof(long), sizeof(long)), values[i]);
		return bytes;
	}

	private static ReadOnlySpan<byte> GetRawBytes(object value, SingleStoreDbType dbType) =>
		value switch
		{
			byte[] x => x,
			ReadOnlyMemory<byte> x => x.Span,
			Memory<byte> x => x.Span,
			ArraySegment<byte> x => x.AsSpan(),
			MemoryStream x => x.TryGetBuffer(out var buffer) ? buffer.AsSpan() : x.ToArray(),
			_ => throw new NotSupportedException(
				$"Parameter type {value.GetType().Name} is not supported for {dbType}."),
		};
}
