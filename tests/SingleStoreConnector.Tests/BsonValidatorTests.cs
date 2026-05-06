using SingleStoreConnector.Core;
using Xunit;

namespace SingleStoreConnector.Tests;

public class BsonValidatorTests
{
	[Fact]
	public void ValidateBson_ValidDocument_Succeeds()
	{
		// Valid minimal BSON document: 5 bytes total, ending with null terminator
		// {0x05, 0x00, 0x00, 0x00} = 5 bytes length
		// {0x00} = null terminator
		var validBson = new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00 };

		var result = BsonValidator.TryValidate(validBson, out var errorMessage);

		Assert.True(result);
		Assert.Null(errorMessage);
	}

	[Fact]
	public void ValidateBson_ValidDocumentWithContent_Succeeds()
	{
		// BSON document with a boolean field: {"x": true}
		// Length: 9 bytes
		var validBson = new byte[]
		{
			0x09, 0x00, 0x00, 0x00, // 9 bytes total
			0x08,                   // boolean type
			0x78,                   // field name "x"
			0x00,                   // null terminator for field name
			0x01,                   // true value
			0x00,                   // document terminator
		};

		var result = BsonValidator.TryValidate(validBson, out var errorMessage);

		Assert.True(result);
		Assert.Null(errorMessage);
	}

	[Fact]
	public void ValidateBson_TooShort_Fails()
	{
		var invalidBson = new byte[] { 0x05, 0x00, 0x00 }; // Only 3 bytes

		var result = BsonValidator.TryValidate(invalidBson, out var errorMessage);

		Assert.False(result);
		Assert.Contains("too short", errorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ValidateBson_LengthMismatch_Fails()
	{
		// Claims to be 10 bytes but is only 5 bytes
		var invalidBson = new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x00 };

		var result = BsonValidator.TryValidate(invalidBson, out var errorMessage);

		Assert.False(result);
		Assert.Contains("length mismatch", errorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ValidateBson_MissingNullTerminator_Fails()
	{
		// 5 bytes total but doesn't end with null terminator
		var invalidBson = new byte[] { 0x05, 0x00, 0x00, 0x00, 0xFF };

		var result = BsonValidator.TryValidate(invalidBson, out var errorMessage);

		Assert.False(result);
		Assert.Contains("null terminator", errorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void ValidateBson_DeclaredLengthTooSmall_Fails()
	{
		// Claims to be only 2 bytes (impossible for valid BSON)
		var invalidBson = new byte[] { 0x02, 0x00, 0x00, 0x00, 0x00 };

		var result = BsonValidator.TryValidate(invalidBson, out var errorMessage);

		Assert.False(result);
		Assert.Contains("too small", errorMessage, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void Validate_ThrowsFormatException_WhenInvalid()
	{
		var invalidBson = new byte[] { 0x05, 0x00, 0x00 };

		Assert.Throws<FormatException>(() => BsonValidator.Validate(invalidBson));
	}

	[Fact]
	public void Validate_DoesNotThrow_WhenValid()
	{
		var validBson = new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00 };

		BsonValidator.Validate(validBson); // Should not throw
	}
}
