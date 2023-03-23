using System;
using System.Globalization;
using System.Reflection;
using log4net;
using log4net.Core;

namespace SingleStoreConnector.Logging;

public sealed class Log4netLoggerProvider : ISingleStoreConnectorLoggerProvider
{
	public ISingleStoreConnectorLogger CreateLogger(string name) => new Log4netLogger(LogManager.GetLogger(s_loggerAssembly, "SingleStoreConnector." + name));

	private static readonly Assembly s_loggerAssembly = typeof(Log4netLogger).GetTypeInfo().Assembly;
	private static readonly Type s_loggerType = typeof(Log4netLogger);

	private sealed class Log4netLogger : ISingleStoreConnectorLogger
	{
		public Log4netLogger(ILoggerWrapper log) => m_logger = log.Logger;

		public bool IsEnabled(SingleStoreConnectorLogLevel level) => m_logger.IsEnabledFor(GetLevel(level));

		public void Log(SingleStoreConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			if (args is null || args.Length == 0)
				m_logger.Log(s_loggerType, GetLevel(level), message, exception);
			else
				m_logger.Log(s_loggerType, GetLevel(level), string.Format(CultureInfo.InvariantCulture, message, args), exception);
		}

		private static Level GetLevel(SingleStoreConnectorLogLevel level) => level switch
		{
			SingleStoreConnectorLogLevel.Trace => Level.Trace,
			SingleStoreConnectorLogLevel.Debug => Level.Debug,
			SingleStoreConnectorLogLevel.Info => Level.Info,
			SingleStoreConnectorLogLevel.Warn => Level.Warn,
			SingleStoreConnectorLogLevel.Error => Level.Error,
			SingleStoreConnectorLogLevel.Fatal => Level.Fatal,
			_ => throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'."),
		};

		private readonly ILogger m_logger;
	}
}
