using System.Globalization;
using System.Text;

namespace SingleStoreConnector.Logging;

public class ConsoleLoggerProvider : ISingleStoreConnectorLoggerProvider
{
	public ConsoleLoggerProvider(SingleStoreConnectorLogLevel minimumLevel = SingleStoreConnectorLogLevel.Info, bool isColored = true)
	{
		if (minimumLevel < SingleStoreConnectorLogLevel.Trace || minimumLevel > SingleStoreConnectorLogLevel.Fatal)
			throw new ArgumentOutOfRangeException(nameof(minimumLevel), "minimumLevel must be between Trace and Fatal");

		m_minimumLevel = minimumLevel;
		m_isColored = isColored;
	}

	public ISingleStoreConnectorLogger CreateLogger(string name) => new ConsoleLogger(this, name);

	private sealed class ConsoleLogger : ISingleStoreConnectorLogger
	{
		public ConsoleLogger(ConsoleLoggerProvider provider, string name)
		{
			m_provider = provider;
			m_name = name;
		}

		public bool IsEnabled(SingleStoreConnectorLogLevel level) => level >= m_provider.m_minimumLevel && level <= SingleStoreConnectorLogLevel.Fatal;

		public void Log(SingleStoreConnectorLogLevel level, string message, object?[]? args = null, Exception? exception = null)
		{
			if (!IsEnabled(level))
				return;

			var sb = new StringBuilder();
			sb.Append(s_levels[(int) level]);
			sb.Append('\t');
			sb.Append(m_name);
			sb.Append('\t');

			if (args is null || args.Length == 0)
				sb.Append(message);
			else
				sb.AppendFormat(CultureInfo.InvariantCulture, message, args);
			sb.AppendLine();

			if (exception is not null)
				sb.AppendLine(exception.ToString());

			if (m_provider.m_isColored)
			{
				lock (m_provider)
				{
					var oldColor = Console.ForegroundColor;
					Console.ForegroundColor = s_colors[(int) level];
					Console.Write(sb.ToString());
					Console.ForegroundColor = oldColor;
				}
			}
			else
			{
				Console.Write(sb.ToString());
			}
		}

		private static readonly string[] s_levels =
		{
			"",
			"[TRACE]",
			"[DEBUG]",
			"[INFO]",
			"[WARN]",
			"[ERROR]",
			"[FATAL]",
		};

		private static readonly ConsoleColor[] s_colors =
		{
			ConsoleColor.Black,
			ConsoleColor.DarkGray,
			ConsoleColor.Gray,
			ConsoleColor.White,
			ConsoleColor.Yellow,
			ConsoleColor.Red,
			ConsoleColor.Red,
		};

		private readonly ConsoleLoggerProvider m_provider;
		private readonly string m_name;
	}

	private readonly SingleStoreConnectorLogLevel m_minimumLevel;
	private readonly bool m_isColored;
}
