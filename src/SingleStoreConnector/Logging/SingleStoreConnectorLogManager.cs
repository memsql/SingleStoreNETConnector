using Microsoft.Extensions.Logging;

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
		set
		{
			SingleStoreConnectorLoggingConfiguration.GlobalConfiguration = new(new SingleStoreConnectorLoggerFactory(value));
		}
	}

	// A helper class that adapts ILoggerFactory to the old-style ISingleStoreConnectorLoggerProvider interface.
	private sealed class SingleStoreConnectorLoggerFactory(ISingleStoreConnectorLoggerProvider loggerProvider) : ILoggerFactory
	{
		public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

		public ILogger CreateLogger(string categoryName)
		{
			// assume all logger names start with "SingleStoreConnector." but the old API didn't expect that prefix
			return new SingleStoreConnectorLogger(loggerProvider.CreateLogger(categoryName[15..]));
		}

		public void Dispose()
		{
		}
	}

	// A helper class that adapts ILogger to the old-style ISingleStoreConnectorLogger interface.
	private sealed class SingleStoreConnectorLogger(ISingleStoreConnectorLogger logger) : ILogger
	{
		public IDisposable BeginScope<TState>(TState state)
			where TState : notnull
			=> throw new NotSupportedException();

		public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(ConvertLogLevel(logLevel));

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			logger.Log(ConvertLogLevel(logLevel), formatter(state, exception), exception: exception);

		private static SingleStoreConnectorLogLevel ConvertLogLevel(LogLevel logLevel) =>
			logLevel switch
			{
				LogLevel.Trace => SingleStoreConnectorLogLevel.Trace,
				LogLevel.Debug => SingleStoreConnectorLogLevel.Debug,
				LogLevel.Information => SingleStoreConnectorLogLevel.Info,
				LogLevel.Warning => SingleStoreConnectorLogLevel.Warn,
				LogLevel.Error => SingleStoreConnectorLogLevel.Error,
				LogLevel.Critical => SingleStoreConnectorLogLevel.Fatal,
				_ => SingleStoreConnectorLogLevel.Info,
			};
	}
}
