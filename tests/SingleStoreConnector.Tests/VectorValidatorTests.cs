using SingleStoreConnector.Core;
using Xunit;

namespace SingleStoreConnector.Tests;

public class VectorValidatorTests
{
	[Fact]
	public void GetDimensionCount_FloatArray_ReturnsCorrectCount()
	{
		var vector = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };

		var count = VectorValidator.GetDimensionCount(vector);

		Assert.Equal(4, count);
	}

	[Fact]
	public void GetDimensionCount_DoubleArray_ReturnsCorrectCount()
	{
		var vector = new double[] { 1.0, 2.0, 3.0 };

		var count = VectorValidator.GetDimensionCount(vector);

		Assert.Equal(3, count);
	}

	[Fact]
	public void GetDimensionCount_IntArray_ReturnsCorrectCount()
	{
		var vector = new int[] { 1, 2, 3, 4, 5 };

		var count = VectorValidator.GetDimensionCount(vector);

		Assert.Equal(5, count);
	}

	[Fact]
	public void GetDimensionCount_ReadOnlyMemoryFloat_ReturnsCorrectCount()
	{
		var vector = new ReadOnlyMemory<float>(new float[] { 1.0f, 2.0f });

		var count = VectorValidator.GetDimensionCount(vector);

		Assert.Equal(2, count);
	}

	[Fact]
	public void GetElementTypeName_FloatArray_ReturnsF32()
	{
		var vector = new float[] { 1.0f, 2.0f };

		var typeName = VectorValidator.GetElementTypeName(vector);

		Assert.Equal("F32", typeName);
	}

	[Fact]
	public void GetElementTypeName_DoubleArray_ReturnsF64()
	{
		var vector = new double[] { 1.0, 2.0 };

		var typeName = VectorValidator.GetElementTypeName(vector);

		Assert.Equal("F64", typeName);
	}

	[Fact]
	public void GetElementTypeName_IntArray_ReturnsI32()
	{
		var vector = new int[] { 1, 2 };

		var typeName = VectorValidator.GetElementTypeName(vector);

		Assert.Equal("I32", typeName);
	}

	[Fact]
	public void GetElementTypeName_LongArray_ReturnsI64()
	{
		var vector = new long[] { 1L, 2L };

		var typeName = VectorValidator.GetElementTypeName(vector);

		Assert.Equal("I64", typeName);
	}

	[Fact]
	public void ValidateDimensions_MatchingDimensions_Succeeds()
	{
		var vector = new float[] { 1.0f, 2.0f, 3.0f };

		// Should not throw
		VectorValidator.ValidateDimensions(vector, expectedDimensions: 3, expectedElementType: "F32", parameterName: "test");
	}

	[Fact]
	public void ValidateDimensions_MismatchedDimensions_ThrowsArgumentException()
	{
		var vector = new float[] { 1.0f, 2.0f };

		var ex = Assert.Throws<ArgumentException>(() =>
			VectorValidator.ValidateDimensions(vector, expectedDimensions: 3, expectedElementType: "F32", parameterName: "testParam"));

		Assert.Contains("dimension mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("testParam", ex.Message);
		Assert.Contains("expected 3", ex.Message);
		Assert.Contains("got 2", ex.Message);
	}

	[Fact]
	public void ValidateDimensions_MismatchedElementType_ThrowsArgumentException()
	{
		var vector = new float[] { 1.0f, 2.0f };

		var ex = Assert.Throws<ArgumentException>(() =>
			VectorValidator.ValidateDimensions(vector, expectedDimensions: 2, expectedElementType: "F64", parameterName: "testParam"));

		Assert.Contains("element type mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("testParam", ex.Message);
		Assert.Contains("expected F64", ex.Message);
		Assert.Contains("got F32", ex.Message);
	}

	[Fact]
	public void ValidateDimensions_NullExpectedDimensions_SkipsValidation()
	{
		var vector = new float[] { 1.0f, 2.0f };

		// Should not throw even though dimensions don't match
		VectorValidator.ValidateDimensions(vector, expectedDimensions: null, expectedElementType: null, parameterName: "test");
	}

	[Fact]
	public void ValidateDimensions_NullElementType_SkipsElementTypeValidation()
	{
		var vector = new float[] { 1.0f, 2.0f };

		// Should not throw even though element type doesn't match
		VectorValidator.ValidateDimensions(vector, expectedDimensions: 2, expectedElementType: null, parameterName: "test");
	}

	[Theory]
	[InlineData(new[] { 1.0f, 2.0f, 3.0f }, 3)]
	[InlineData(new[] { 1.0f }, 1)]
	[InlineData(new float[] { }, 0)]
	public void GetDimensionCount_VariousSizes_ReturnsCorrectCount(float[] vector, int expectedCount)
	{
		var count = VectorValidator.GetDimensionCount(vector);

		Assert.Equal(expectedCount, count);
	}
}
