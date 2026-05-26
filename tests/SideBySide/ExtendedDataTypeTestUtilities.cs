using System.Runtime.InteropServices;

namespace SideBySide;

internal static class ExtendedDataTypeTestUtilities
{
	public static Type GetVectorDataColumnType(string dataType) =>
		dataType switch
		{
			"byte[]" => typeof(byte[]),
			"float[]" => typeof(float[]),
			"double[]" => typeof(double[]),
			"sbyte[]" => typeof(sbyte[]),
			"short[]" => typeof(short[]),
			"int[]" => typeof(int[]),
			"long[]" => typeof(long[]),
			_ => throw new ArgumentOutOfRangeException(nameof(dataType)),
		};

	public static object GetVectorDataRowValue(float[] data, string dataType) =>
		dataType switch
		{
			"byte[]" => MemoryMarshal.Cast<float, byte>(data).ToArray(),
			"float[]" => data,
			"double[]" => data.Select(x => (double) x).ToArray(),
			"sbyte[]" => data.Select(x => (sbyte) x).ToArray(),
			"short[]" => data.Select(x => (short) x).ToArray(),
			"int[]" => data.Select(x => (int) x).ToArray(),
			"long[]" => data.Select(x => (long) x).ToArray(),
			_ => throw new ArgumentOutOfRangeException(nameof(dataType)),
		};

	public static object GetVectorParameterValue(string elementType) =>
		elementType switch
		{
			"F32" => new float[] { 1, 2, 3 },
			"F64" => new double[] { 1, 2, 3 },
			"I8" => new sbyte[] { 1, 2, 3 },
			"I16" => new short[] { 1, 2, 3 },
			"I32" => new int[] { 1, 2, 3 },
			"I64" => new long[] { 1, 2, 3 },
			_ => throw new ArgumentOutOfRangeException(nameof(elementType)),
		};

	public static void AssertVectorEquals(SingleStoreDataReader reader, int ordinal, string dataType, float[] expected)
	{
		switch (dataType)
		{
			case "byte[]":
			case "float[]":
				Assert.Equal(expected, GetVectorArray<float>(reader, ordinal));
				break;

			case "double[]":
				Assert.Equal(expected.Select(x => (double) x).ToArray(), GetVectorArray<double>(reader, ordinal));
				break;

			case "sbyte[]":
				Assert.Equal(expected.Select(x => (sbyte) x).ToArray(), GetVectorArray<sbyte>(reader, ordinal));
				break;

			case "short[]":
				Assert.Equal(expected.Select(x => (short) x).ToArray(), GetVectorArray<short>(reader, ordinal));
				break;

			case "int[]":
				Assert.Equal(expected.Select(x => (int) x).ToArray(), GetVectorArray<int>(reader, ordinal));
				break;

			case "long[]":
				Assert.Equal(expected.Select(x => (long) x).ToArray(), GetVectorArray<long>(reader, ordinal));
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(dataType));
		}
	}

	public static void AssertVectorEquals(object actual, string elementType, int[] expected)
	{
		switch (elementType)
		{
			case "F32":
				Assert.Equal(expected.Select(x => (float) x).ToArray(), Assert.IsType<ReadOnlyMemory<float>>(actual).ToArray());
				break;

			case "F64":
				Assert.Equal(expected.Select(x => (double) x).ToArray(), Assert.IsType<ReadOnlyMemory<double>>(actual).ToArray());
				break;

			case "I8":
				Assert.Equal(expected.Select(x => (sbyte) x).ToArray(), Assert.IsType<ReadOnlyMemory<sbyte>>(actual).ToArray());
				break;

			case "I16":
				Assert.Equal(expected.Select(x => (short) x).ToArray(), Assert.IsType<ReadOnlyMemory<short>>(actual).ToArray());
				break;

			case "I32":
				Assert.Equal(expected, Assert.IsType<ReadOnlyMemory<int>>(actual).ToArray());
				break;

			case "I64":
				Assert.Equal(expected.Select(x => (long) x).ToArray(), Assert.IsType<ReadOnlyMemory<long>>(actual).ToArray());
				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(elementType));
		}
	}

	public static void AssertVectorEquals(object actual, string elementType)
	{
		AssertVectorEquals(actual, elementType, new[] { 1, 2, 3 });
	}

	private static T[] GetVectorArray<T>(SingleStoreDataReader reader, int ordinal)
		where T : unmanaged
	{
		return reader.GetValue(ordinal) switch
		{
			ReadOnlyMemory<T> memory => memory.ToArray(),
			byte[] bytes => MemoryMarshal.Cast<byte, T>(bytes).ToArray(),
			{ } value => throw new NotSupportedException(value.GetType().Name),
		};
	}
}
