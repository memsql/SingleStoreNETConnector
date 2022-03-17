namespace SingleStoreConnector.Logging;

/// <summary>
/// Implementations of <see cref="ISingleStoreConnectorLoggerProvider"/> create logger instances.
/// </summary>
public interface ISingleStoreConnectorLoggerProvider
{
	/// <summary>
	/// Creates a logger with the specified name. This method may be called from multiple threads and must be thread-safe.
	/// </summary>
	ISingleStoreConnectorLogger CreateLogger(string name);
}
