using System;

namespace SingleStoreConnector.Core;

/// <summary>
/// Provides basic validation for BSON binary data format.
/// </summary>
internal static class BsonValidator
{
	/// <summary>
	/// Validates that the given binary data could be valid BSON.
	/// Performs minimal checks: length prefix validation and basic structure.
	/// </summary>
	/// <param name="data">The binary data to validate.</param>
	/// <param name="errorMessage">If validation fails, contains a description of the error.</param>
	/// <returns>True if the data appears to be valid BSON; otherwise, false.</returns>
	public static bool TryValidate(ReadOnlySpan<byte> data, out string? errorMessage)
	{
		// BSON documents must be at least 5 bytes (4-byte length + 1-byte null terminator)
		if (data.Length < 5)
		{
			errorMessage = $"BSON document too short: {data.Length} bytes (minimum is 5 bytes)";
			return false;
		}

		// Read the 32-bit little-endian length prefix
		var declaredLength = data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24);

		// Check declared length is reasonable (must be at least 5 bytes)
		if (declaredLength < 5)
		{
			errorMessage = $"BSON declared length is too small: {declaredLength} bytes";
			return false;
		}

		// The declared length must match the actual data length
		if (declaredLength != data.Length)
		{
			errorMessage = $"BSON length mismatch: declared {declaredLength} bytes, but data is {data.Length} bytes";
			return false;
		}

		// BSON documents must end with a null byte (0x00)
		if (data[^1] != 0x00)
		{
			errorMessage = "BSON document does not end with null terminator (0x00)";
			return false;
		}

		errorMessage = null;
		return true;
	}

	/// <summary>
	/// Validates BSON data, throwing an exception if invalid.
	/// </summary>
	/// <param name="data">The binary data to validate.</param>
	/// <exception cref="FormatException">Thrown if the data is not valid BSON.</exception>
	public static void Validate(ReadOnlySpan<byte> data)
	{
		if (!TryValidate(data, out var errorMessage))
			throw new FormatException($"Invalid BSON data: {errorMessage}");
	}
}
