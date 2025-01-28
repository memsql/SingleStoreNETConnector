using System.Runtime.Serialization;

namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreConversionException"/> is thrown when a SingleStore value can't be converted to another type.
/// </summary>
[Serializable]
public sealed class SingleStoreConversionException : Exception
{
	/// <summary>
	/// Initializes a new instance of <see cref="SingleStoreConversionException"/>.
	/// </summary>
	/// <param name="message">The exception message.</param>
	internal SingleStoreConversionException(string message)
		: base(message)
	{
	}

#if NET8_0_OR_GREATER
	[Obsolete(DiagnosticId = "SYSLIB0051")]
#endif
	private SingleStoreConversionException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}
}
