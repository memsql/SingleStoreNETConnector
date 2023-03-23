using System;
using System.Globalization;
using NLog;

namespace SingleStoreConnector.Logging;

public sealed class NLogLoggerProvider : ISingleStoreConnectorLoggerProvider
{
	public ISingleStoreConnectorLogger CreateLogger(string name) => new NLogLogger(LogManager.GetLogger("SingleStoreConnector." + name));

	private static readonly Type s_loggerType = typeof(NLogLogger);

	private sealed class NLogLogger : ISingleStoreConnectorLogger
	{
		public NLogLogger(Logger logger) => m_logger = logger;

		public bool IsEnabled(SingleStoreConnectorLogLevel level) => m_logger.IsEnabled(GetLevel(level));

		public void Log(SingleStoreConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			LogLevel logLevel = GetLevel(level);
			if (m_logger.IsEnabled(logLevel))
			{
				m_logger.Log(s_loggerType, LogEventInfo.Create(logLevel, m_logger.Name, exception, CultureInfo.InvariantCulture, message, args));
			}
		}

		private static LogLevel GetLevel(SingleStoreConnectorLogLevel level) => level switch
		{
			SingleStoreConnectorLogLevel.Trace => LogLevel.Trace,
			SingleStoreConnectorLogLevel.Debug => LogLevel.Debug,
			SingleStoreConnectorLogLevel.Info => LogLevel.Info,
			SingleStoreConnectorLogLevel.Warn => LogLevel.Warn,
			SingleStoreConnectorLogLevel.Error => LogLevel.Error,
			SingleStoreConnectorLogLevel.Fatal => LogLevel.Fatal,
			_ => throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'."),
		};

		private readonly Logger m_logger;
	}
}
