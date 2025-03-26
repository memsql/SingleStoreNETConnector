using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SingleStoreConnector.Logging;

internal sealed class SingleStoreConnectorLoggingConfiguration(ILoggerFactory loggerFactory)
{
	public ILogger DataSourceLogger { get; } = loggerFactory.CreateLogger("SingleStoreConnector.SingleStoreDataSource");
	public ILogger ConnectionLogger { get; } = loggerFactory.CreateLogger("SingleStoreConnector.SingleStoreConnection");
	public ILogger CommandLogger { get; } = loggerFactory.CreateLogger("SingleStoreConnector.SingleStoreCommand");
	public ILogger PoolLogger { get; } = loggerFactory.CreateLogger("SingleStoreConnector.ConnectionPool");
	public ILogger BulkCopyLogger { get; } = loggerFactory.CreateLogger("SingleStoreConnector.SingleStoreBulkCopy");

	public static SingleStoreConnectorLoggingConfiguration NullConfiguration { get; } = new SingleStoreConnectorLoggingConfiguration(NullLoggerFactory.Instance);
	public static SingleStoreConnectorLoggingConfiguration GlobalConfiguration { get; set; } = NullConfiguration;
}
