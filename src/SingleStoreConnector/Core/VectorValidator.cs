using System;

namespace SingleStoreConnector.Core;

/// <summary>
/// Provides validation for VECTOR data dimensions and element types.
/// </summary>
internal static class VectorValidator
{
	/// <summary>
	/// Validates that a VECTOR value matches the expected dimensions and element type.
	/// </summary>
	/// <param name="value">The vector value to validate.</param>
	/// <param name="expectedDimensions">The expected number of dimensions (null to skip check).</param>
	/// <param name="expectedElementType">The expected element type name (null to skip check).</param>
	/// <param name="parameterName">The parameter name for error messages.</param>
	/// <exception cref="ArgumentException">Thrown if validation fails.</exception>
	public static void ValidateDimensions(object value, int? expectedDimensions, string? expectedElementType, string parameterName)
	{
		if (expectedDimensions is null)
			return;

		var actualDimensions = GetDimensionCount(value);
		if (actualDimensions != expectedDimensions.Value)
		{
			throw new ArgumentException(
				$"VECTOR dimension mismatch for parameter '{parameterName}': expected {expectedDimensions} elements, but got {actualDimensions}.",
				parameterName);
		}

		// Optionally validate element type matches
		if (!string.IsNullOrEmpty(expectedElementType))
		{
			var actualElementType = GetElementTypeName(value);
			if (!string.Equals(actualElementType, expectedElementType, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(
					$"VECTOR element type mismatch for parameter '{parameterName}': expected {expectedElementType}, but got {actualElementType}.",
					parameterName);
			}
		}
	}

	/// <summary>
	/// Gets the number of elements (dimensions) in a vector value.
	/// </summary>
	public static int GetDimensionCount(object value)
	{
		return value switch
		{
			float[] x => x.Length,
			ReadOnlyMemory<float> x => x.Length,
			Memory<float> x => x.Length,
			double[] x => x.Length,
			ReadOnlyMemory<double> x => x.Length,
			Memory<double> x => x.Length,
			sbyte[] x => x.Length,
			ReadOnlyMemory<sbyte> x => x.Length,
			Memory<sbyte> x => x.Length,
			short[] x => x.Length,
			ReadOnlyMemory<short> x => x.Length,
			Memory<short> x => x.Length,
			int[] x => x.Length,
			ReadOnlyMemory<int> x => x.Length,
			Memory<int> x => x.Length,
			long[] x => x.Length,
			ReadOnlyMemory<long> x => x.Length,
			Memory<long> x => x.Length,
			byte[] x => x.Length / GetElementSize(value),
			ReadOnlyMemory<byte> x => x.Length / GetElementSize(value),
			Memory<byte> x => x.Length / GetElementSize(value),
			ArraySegment<byte> x => x.Count / GetElementSize(value),
			_ => throw new NotSupportedException($"Cannot determine dimension count for type {value.GetType().Name}"),
		};
	}

	/// <summary>
	/// Gets the element type name for a vector value.
	/// </summary>
	public static string GetElementTypeName(object value)
	{
		return value switch
		{
			float[] or ReadOnlyMemory<float> or Memory<float> => "F32",
			double[] or ReadOnlyMemory<double> or Memory<double> => "F64",
			sbyte[] or ReadOnlyMemory<sbyte> or Memory<sbyte> => "I8",
			short[] or ReadOnlyMemory<short> or Memory<short> => "I16",
			int[] or ReadOnlyMemory<int> or Memory<int> => "I32",
			long[] or ReadOnlyMemory<long> or Memory<long> => "I64",
			byte[] or ReadOnlyMemory<byte> or Memory<byte> or ArraySegment<byte> => "BINARY", // unknown element type for raw bytes
			_ => throw new NotSupportedException($"Cannot determine element type for {value.GetType().Name}"),
		};
	}

	private static int GetElementSize(object value)
	{
		// For raw byte arrays, we can't know the element size without more context
		// This is a limitation when using byte[] for VECTOR parameters
		// We default to 1 byte (I8) but this may not be correct
		return 1;
	}
}
