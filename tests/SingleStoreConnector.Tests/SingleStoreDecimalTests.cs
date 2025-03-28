using System.Globalization;

namespace SingleStoreConnector.Tests;

public class SingleStoreDecimalTests
{
	[Fact]
	public void TestSingleStoreDecimalToString()
	{
		var stringVal = "1.23";
		SingleStoreDecimal decimalVal = new SingleStoreDecimal(stringVal);
		Assert.Equal(stringVal, decimalVal.ToString());
	}

	[Fact]
	public void TestToDouble()
	{
		var doubleVal = 1.23;
		string stringVal = "1.23";
		SingleStoreDecimal mySqlDecimal = new SingleStoreDecimal(stringVal);
		Assert.Equal(doubleVal, mySqlDecimal.ToDouble());
	}

	[Fact]
	public void TestToDecimal()
	{
		decimal doubleVal = 1.2M ;
		string stringVal = "1.2";
		SingleStoreDecimal mySqlDecimal = new SingleStoreDecimal(stringVal);
		Assert.Equal(doubleVal, mySqlDecimal.Value);
	}

	[Fact]
	public void TestValidFormatWithDecimalNegative68Length()
	{
		// If it's valid negative value with . then length should be less than 68
		var invalidValue = "-123456789012345678901234567890123456.012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new SingleStoreDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithDecimalPostive67Length()
	{
		// If it's valid positive value with . then length should be less than 67
		var invalidValue = "123456789012345678901234567890123456.012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new SingleStoreDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithOutDecimalNegative67Length()
	{
		// If it's valid negative value without . then length should be less than 67
		var invalidValue = "-123456789012345678901234567890123456012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new SingleStoreDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithOutDecimalPositive66Length()
	{
		// If it's valid positive value without . then length should be less than 66
		var invalidValue = "123456789012345678901234567890123456012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new SingleStoreDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithOutDecimalNegativeZero()
	{
		var invalidValue = "-0";
		Assert.Throws<FormatException>(() => new SingleStoreDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithDecimalNegativeZero()
	{
		var value = "-0.2342323";
		var decimalVal = new SingleStoreDecimal(value);
		Assert.Equal(value, decimalVal.ToString());
		Assert.Equal(decimal.Parse(value, CultureInfo.InvariantCulture), decimalVal.Value);
	}

	[Fact]
	public void TestValidFormatWithDecimalNegative67Length()
	{
		// valid value with negative and decimal
		var value = "-12345678901234567890123456789012345.012345678901234567890123456789";
		var decimalVal = new SingleStoreDecimal(value);
		Assert.Equal(value, decimalVal.ToString());
	}

	[Theory]
	[InlineData("0")]
	[InlineData("1")]
	[InlineData("-1")]
	[InlineData("0.1")]
	[InlineData("-0.1")]
	[InlineData("1.0")]
	[InlineData("1.23")]
	[InlineData("00")]
	[InlineData("01")]
	[InlineData("0323.323")]
	[InlineData("-0323.323")]
	[InlineData("12345678901234567890123456789012345678901234567890123456789012345")]
	[InlineData("-12345678901234567890123456789012345678901234567890123456789012345")]
	[InlineData("12345678901234567890123456789012345.012345678901234567890123456789")]
	[InlineData("-12345678901234567890123456789012345.012345678901234567890123456789")]
	public void ValidDecimalValues(string input) =>
		Assert.Equal(input, new SingleStoreDecimal(input).ToString());

	[Theory]
	[InlineData("")]
	[InlineData("-0")]
	[InlineData("-0.0")]
	[InlineData("123456789012345678901234567890123456789012345678901234567890123456")]
	[InlineData("-123456789012345678901234567890123456789012345678901234567890123456")]
	[InlineData("123456789012345678901234567890123456.012345678901234567890123456789")]
	[InlineData("-123456789012345678901234567890123456.012345678901234567890123456789")]
	[InlineData("12345678901234567890123456789012345.0123456789012345678901234567891")]
	[InlineData("-12345678901234567890123456789012345.0123456789012345678901234567891")]
	public void InvalidDecimalValues(string input) =>
		Assert.Throws<FormatException>(() => new SingleStoreDecimal(input));
}
