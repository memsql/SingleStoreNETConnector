namespace SingleStoreConnector.Logging;

/// <summary>
/// Controls logging for SingleStoreConnector.
/// </summary>
public static class SingleStoreConnectorLogManager
{
	/// <summary>
	/// Allows the <see cref="ISingleStoreConnectorLoggerProvider"/> to be set for this library. <see cref="Provider"/> can
	/// be set once, and must be set before any other library methods are used.
	/// </summary>
#pragma warning disable CA1044 // Properties should not be write only
	public static ISingleStoreConnectorLoggerProvider Provider
	{
		internal get
		{
			s_providerRetrieved = true;
			return s_provider;
		}
		set
		{
			if (s_providerRetrieved)
				throw new InvalidOperationException("The logging provider must be set before any SingleStoreConnector methods are called.");

			s_provider = value;
		}
	}

	internal static ISingleStoreConnectorLogger CreateLogger(string name) => Provider.CreateLogger(name);

	private static ISingleStoreConnectorLoggerProvider s_provider = new NoOpLoggerProvider();

	// comment line above and uncomment below to get tracelogs output to console
	// static ISingleStoreConnectorLoggerProvider s_provider = new ConsoleLoggerProvider(SingleStoreConnectorLogLevel.Trace);
	private static bool s_providerRetrieved;
}
