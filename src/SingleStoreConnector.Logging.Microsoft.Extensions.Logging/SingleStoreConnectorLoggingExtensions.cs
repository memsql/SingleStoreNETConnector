using System;
using Microsoft.Extensions.Logging;

namespace SingleStoreConnector.Logging;

public static class SingleStoreConnectorLoggingExtensions
{
	[Obsolete("Use UseLoggerFactory or AddSingleStoreDataSource instead.")]
	public static IServiceProvider UseSingleStoreConnectorLogging(this IServiceProvider services)
	{
		var loggerFactory = (ILoggerFactory) services.GetService(typeof(ILoggerFactory));
		if (loggerFactory is null)
			throw new InvalidOperationException("No ILoggerFactory service has been registered.");
		SingleStoreConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
		return services;
	}
}
