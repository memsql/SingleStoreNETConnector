#nullable enable

namespace SingleStoreConnector.Tests.Metrics;

public interface IConnectionCreator : IDisposable
{
	string PoolName { get; }
	SingleStoreConnection OpenConnection();
}

internal sealed class DataSourceConnectionCreator : IConnectionCreator
{
	public DataSourceConnectionCreator(bool usePooling, string? poolName, string? applicationName, SingleStoreConnectionStringBuilder connectionStringBuilder)
	{
		connectionStringBuilder.Pooling = usePooling;
		connectionStringBuilder.ApplicationName = applicationName;
		m_dataSource = new SingleStoreDataSourceBuilder(connectionStringBuilder.ConnectionString)
			.UseName(poolName)
			.Build();
		PoolName = poolName ?? applicationName ?? connectionStringBuilder.GetConnectionString(includePassword: false);
	}

	public SingleStoreConnection OpenConnection() => m_dataSource.OpenConnection();
	public string PoolName { get; }
	public void Dispose() => m_dataSource.Dispose();

	private readonly SingleStoreDataSource m_dataSource;
}

internal sealed class PlainConnectionCreator : IConnectionCreator
{
	public PlainConnectionCreator(bool usePooling, string? applicationName, SingleStoreConnectionStringBuilder connectionStringBuilder)
	{
		connectionStringBuilder.Pooling = usePooling;
		connectionStringBuilder.ApplicationName = applicationName;
		m_connectionString = connectionStringBuilder.ConnectionString;
		PoolName = applicationName ?? connectionStringBuilder.GetConnectionString(includePassword: false);
	}

	public SingleStoreConnection OpenConnection()
	{
		var connection = new SingleStoreConnection(m_connectionString);
		connection.Open();
		return connection;
	}

	public string PoolName { get; }

	public void Dispose()
	{
	}

	private readonly string m_connectionString;
}
