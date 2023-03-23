using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace SingleStoreConnector.Logging;

/// <summary>
/// Implements SingleStoreConnector logging using the Microsoft.Extensions.Logging abstraction.
/// </summary>
public sealed class MicrosoftExtensionsLoggingLoggerProvider : ISingleStoreConnectorLoggerProvider
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MicrosoftExtensionsLoggingLoggerProvider"/>.
	/// </summary>
	/// <param name="loggerFactory">The logging factory to use.</param>
	public MicrosoftExtensionsLoggingLoggerProvider(ILoggerFactory loggerFactory)
		: this(loggerFactory, false)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MicrosoftExtensionsLoggingLoggerProvider"/>.
	/// </summary>
	/// <param name="loggerFactory">The logging factory to use.</param>
	/// <param name="omitMySqlConnectorPrefix">True to omit the "SingleStoreConnector." prefix from logger names; this matches the default behavior prior to v2.1.0.</param>
	public MicrosoftExtensionsLoggingLoggerProvider(ILoggerFactory loggerFactory, bool omitMySqlConnectorPrefix)
	{
		m_loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
		m_prefix = omitMySqlConnectorPrefix ? "" : "SingleStoreConnector.";
	}

	/// <summary>
	/// Creates a new <see cref="ISingleStoreConnectorLogger"/> with the specified name.
	/// </summary>
	/// <param name="name">The logger name.</param>
	/// <returns>A <see cref="ISingleStoreConnectorLogger"/> that logs with the specified logger name.</returns>
	public ISingleStoreConnectorLogger CreateLogger(string name) => new MicrosoftExtensionsLoggingLogger(m_loggerFactory.CreateLogger(m_prefix + name));

	private sealed class MicrosoftExtensionsLoggingLogger : ISingleStoreConnectorLogger
	{
		public MicrosoftExtensionsLoggingLogger(ILogger logger) => m_logger = logger;

		public bool IsEnabled(SingleStoreConnectorLogLevel level) => m_logger.IsEnabled(GetLevel(level));

		public void Log(SingleStoreConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			if (args is null || args.Length == 0)
				m_logger.Log(GetLevel(level), 0, message, exception, s_getMessage);
			else
				m_logger.Log(GetLevel(level), 0, (message, args), exception, s_messageFormatter);
		}

		private static LogLevel GetLevel(SingleStoreConnectorLogLevel level) => level switch
		{
			SingleStoreConnectorLogLevel.Trace => LogLevel.Trace,
			SingleStoreConnectorLogLevel.Debug => LogLevel.Debug,
			SingleStoreConnectorLogLevel.Info => LogLevel.Information,
			SingleStoreConnectorLogLevel.Warn => LogLevel.Warning,
			SingleStoreConnectorLogLevel.Error => LogLevel.Error,
			SingleStoreConnectorLogLevel.Fatal => LogLevel.Critical,
			_ => throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'."),
		};

		private static readonly Func<string, Exception, string> s_getMessage = static (s, e) => s;
		private static readonly Func<(string Message, object?[] Args), Exception, string> s_messageFormatter = static (s, e) => string.Format(CultureInfo.InvariantCulture, s.Message, s.Args);

		private readonly ILogger m_logger;
	}

	private readonly ILoggerFactory m_loggerFactory;
	private readonly string m_prefix;
}
