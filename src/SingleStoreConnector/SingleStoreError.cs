namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreError"/> represents an error or warning that occurred during the execution of a SQL statement.
/// </summary>
public sealed class SingleStoreError
{
	internal SingleStoreError(string level, int code, string message)
	{
		Level = level;
#pragma warning disable 618
		Code = code;
#pragma warning restore
		ErrorCode = (SingleStoreErrorCode) code;
		Message = message;
	}

	/// <summary>
	/// The error level. This comes from the SingleStore Server. Possible values include <c>Note</c>, <c>Warning</c>, and <c>Error</c>.
	/// </summary>
	public string Level { get; }

	/// <summary>
	/// The numeric error code. Prefer to use <see cref="ErrorCode"/>.
	/// </summary>
	[Obsolete("Use ErrorCode")]
	public int Code { get; }

	/// <summary>
	/// The <see cref="SingleStoreErrorCode"/> for the error or warning.
	/// </summary>
	public SingleStoreErrorCode ErrorCode { get; }

	/// <summary>
	/// A human-readable description of the error or warning.
	/// </summary>
	public string Message { get; }
}
