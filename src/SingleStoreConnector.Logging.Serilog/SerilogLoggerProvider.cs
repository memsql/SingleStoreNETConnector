using System;
using System.Text.RegularExpressions;
using Serilog;
using Serilog.Events;

namespace SingleStoreConnector.Logging;

public sealed class SerilogLoggerProvider : ISingleStoreConnectorLoggerProvider
{
	public SerilogLoggerProvider()
	{
	}

	public ISingleStoreConnectorLogger CreateLogger(string name) => new SerilogLogger(name);

	private sealed class SerilogLogger : ISingleStoreConnectorLogger
	{
		public SerilogLogger(string name) => m_logger = Serilog.Log.ForContext("SourceContext", "SingleStoreConnector." + name);

		public bool IsEnabled(SingleStoreConnectorLogLevel level) => m_logger.IsEnabled(GetLevel(level));

		public void Log(SingleStoreConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			if (args is null || args.Length == 0)
			{
				m_logger.Write(GetLevel(level), exception, message);
			}
			else
			{
				// rewrite message as template
				var template = tokenReplacer.Replace(message, "$1{MySql$2$3}$4");
				m_logger.Write(GetLevel(level), exception, template, args);
			}
		}

		private static LogEventLevel GetLevel(SingleStoreConnectorLogLevel level) => level switch
		{
			SingleStoreConnectorLogLevel.Trace => LogEventLevel.Verbose,
			SingleStoreConnectorLogLevel.Debug => LogEventLevel.Debug,
			SingleStoreConnectorLogLevel.Info => LogEventLevel.Information,
			SingleStoreConnectorLogLevel.Warn => LogEventLevel.Warning,
			SingleStoreConnectorLogLevel.Error => LogEventLevel.Error,
			SingleStoreConnectorLogLevel.Fatal => LogEventLevel.Fatal,
			_ => throw new ArgumentOutOfRangeException(nameof(level), level, "Invalid value for 'level'."),
		};

		private static readonly Regex tokenReplacer = new(@"((\w+)?\s?(?:=|:)?\s?'?)\{(?:\d+)(\:\w+)?\}('?)", RegexOptions.Compiled);

		private readonly ILogger m_logger;
	}
}
