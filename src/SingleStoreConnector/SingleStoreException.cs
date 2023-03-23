using System.Collections;
using System.Runtime.Serialization;

namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreException"/> is thrown when SingleStore Server returns an error code, or there is a
/// communication error with the server.
/// </summary>
[Serializable]
public sealed class SingleStoreException : DbException
{
	/// <summary>
	/// A <see cref="SingleStoreErrorCode"/> value identifying the kind of error. Prefer to use the <see cref="ErrorCode"/> property.
	/// </summary>
	public int Number { get; }

	/// <summary>
	/// A <see cref="SingleStoreErrorCode"/> value identifying the kind of error.
	/// </summary>
	public new SingleStoreErrorCode ErrorCode { get; }

	/// <summary>
	/// A <c>SQLSTATE</c> code identifying the kind of error.
	/// </summary>
	/// <remarks>See <a href="https://en.wikipedia.org/wiki/SQLSTATE">SQLSTATE</a> for more information.</remarks>
#if NET5_0_OR_GREATER
	public override string? SqlState { get; }
#else
	public string? SqlState { get; }
#endif

	/// <summary>
	/// Returns <c>true</c> if this exception could indicate a transient error condition (that could succeed if retried); otherwise, <c>false</c>.
	/// </summary>
#if NET5_0_OR_GREATER
	public override bool IsTransient => IsErrorTransient(ErrorCode);
#else
	public bool IsTransient => IsErrorTransient(ErrorCode);
#endif

	private SingleStoreException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
		Number = info.GetInt32("Number");
		ErrorCode = (SingleStoreErrorCode) Number;
		SqlState = info.GetString("SqlState");
	}

	/// <summary>
	/// Sets the <see cref="SerializationInfo"/> with information about the exception.
	/// </summary>
	/// <param name="info">The <see cref="SerializationInfo"/> that will be set.</param>
	/// <param name="context">The context.</param>
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		base.GetObjectData(info, context);
		info.AddValue("Number", Number);
		info.AddValue("SqlState", SqlState);
	}

	/// <summary>
	/// Gets a collection of key/value pairs that provide additional information about the exception.
	/// </summary>
	public override IDictionary Data
	{
		get
		{
			if (m_data is null)
			{
				m_data = base.Data;
				m_data["Server Error Code"] = Number;
				m_data["SqlState"] = SqlState;
			}
			return m_data;
		}
	}

	internal SingleStoreException(string message)
		: this(message, null)
	{
	}

	internal SingleStoreException(string message, Exception? innerException)
		: this(default, null, message, innerException)
	{
	}

	internal SingleStoreException(SingleStoreErrorCode errorCode, string message)
		: this(errorCode, null, message, null)
	{
	}

	internal SingleStoreException(SingleStoreErrorCode errorCode, string message, Exception? innerException)
		: this(errorCode, null, message, innerException)
	{
	}

	internal SingleStoreException(SingleStoreErrorCode errorCode, string sqlState, string message)
		: this(errorCode, sqlState, message, null)
	{
	}

	internal SingleStoreException(SingleStoreErrorCode errorCode, string? sqlState, string message, Exception? innerException)
		: base(message, innerException)
	{
		ErrorCode = errorCode;
		Number = (int) errorCode;
		SqlState = sqlState;
	}

	internal static SingleStoreException CreateForTimeout() => CreateForTimeout(null);

	internal static SingleStoreException CreateForTimeout(Exception? innerException) =>
		new(SingleStoreErrorCode.CommandTimeoutExpired, "The Command Timeout expired before the operation completed.", innerException);

	private static bool IsErrorTransient(SingleStoreErrorCode errorCode) => errorCode
		is SingleStoreErrorCode.ConnectionCountError
		or SingleStoreErrorCode.LockDeadlock
		or SingleStoreErrorCode.LockWaitTimeout
		or SingleStoreErrorCode.UnableToConnectToHost
		or SingleStoreErrorCode.XARBDeadlock;

	private IDictionary? m_data;
}
