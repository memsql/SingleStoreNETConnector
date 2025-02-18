#nullable enable

namespace SingleStoreConnector.Tests.Metrics;

public class ConnectionsUsageTests : MetricsTestsBase
{
    [Fact(Skip = MetricsSkip)]
	public void NamedDataSource()
    {
		PoolName = "metrics-test";
		using var dataSource = new SingleStoreDataSourceBuilder(CreateConnectionStringBuilder().ConnectionString)
			.UseName(PoolName)
			.Build();

		// no connections at beginning of test
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection creates a 'used' connection
		using (var connection = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
			Assert.Equal(1, Server.ActiveConnections);
		}

		// closing it creates an 'idle' connection
		AssertMeasurement("db.client.connections.usage", 1);
		AssertMeasurement("db.client.connections.usage|idle", 1);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(1, Server.ActiveConnections);

		// reopening the connection transitions it back to 'used'
		using (var connection = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
		}
		Assert.Equal(1, Server.ActiveConnections);

		// opening a second connection creates a net new 'used' connection
		using (var connection = dataSource.OpenConnection())
		using (var connection2 = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 2);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 2);
			Assert.Equal(2, Server.ActiveConnections);
		}

		AssertMeasurement("db.client.connections.usage", 2);
		AssertMeasurement("db.client.connections.usage|idle", 2);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(2, Server.ActiveConnections);
	}

	[Fact(Skip = MetricsSkip)]
	public void NamedDataSourceWithMinPoolSize()
	{
		var csb = CreateConnectionStringBuilder();
		csb.MinimumPoolSize = 3;

		PoolName = "minimum-pool-size";
		using var dataSource = new SingleStoreDataSourceBuilder(csb.ConnectionString)
			.UseName(PoolName)
			.Build();

		// minimum pool size is created lazily when the first connection is opened
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection creates the minimum connections then takes an idle one from the pool
		using (var connection = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 3);
			AssertMeasurement("db.client.connections.usage|idle", 2);
			AssertMeasurement("db.client.connections.usage|used", 1);
			Assert.Equal(3, Server.ActiveConnections);
		}

		// closing puts it back to idle
		AssertMeasurement("db.client.connections.usage", 3);
		AssertMeasurement("db.client.connections.usage|idle", 3);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(3, Server.ActiveConnections);
	}

	[Fact(Skip = MetricsSkip)]
	public void UnnamedDataSource()
	{
		var csb = CreateConnectionStringBuilder();

		// NOTE: pool "name" is connection string (without password)
		PoolName = csb.GetConnectionString(includePassword: false);

		using var dataSource = new SingleStoreDataSourceBuilder(csb.ConnectionString)
			.Build();

		// no connections at beginning of test
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection creates a 'used' connection
		using (var connection = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
			Assert.Equal(1, Server.ActiveConnections);
		}

		// closing it creates an 'idle' connection
		AssertMeasurement("db.client.connections.usage", 1);
		AssertMeasurement("db.client.connections.usage|idle", 1);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(1, Server.ActiveConnections);

		// reopening the connection transitions it back to 'used'
		using (var connection = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
		}
		Assert.Equal(1, Server.ActiveConnections);

		// opening a second connection creates a net new 'used' connection
		using (var connection = dataSource.OpenConnection())
		using (var connection2 = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 2);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 2);
			Assert.Equal(2, Server.ActiveConnections);
		}

		AssertMeasurement("db.client.connections.usage", 2);
		AssertMeasurement("db.client.connections.usage|idle", 2);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(2, Server.ActiveConnections);
	}

	[Fact(Skip = MetricsSkip)]
	public void NoDataSource()
	{
		var csb = CreateConnectionStringBuilder();

		// NOTE: pool "name" is connection string (without password)
		PoolName = csb.GetConnectionString(includePassword: false);

		// no connections at beginning of test
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection creates a 'used' connection
		using (var connection = new SingleStoreConnection(csb.ConnectionString))
		{
			connection.Open();
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
			Assert.Equal(1, Server.ActiveConnections);
		}

		// closing it creates an 'idle' connection
		AssertMeasurement("db.client.connections.usage", 1);
		AssertMeasurement("db.client.connections.usage|idle", 1);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(1, Server.ActiveConnections);

		// reopening the connection transitions it back to 'used'
		using (var connection = new SingleStoreConnection(csb.ConnectionString))
		{
			connection.Open();
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
		}
		Assert.Equal(1, Server.ActiveConnections);

		// opening a second connection creates a net new 'used' connection
		using (var connection = new SingleStoreConnection(csb.ConnectionString))
		using (var connection2 = new SingleStoreConnection(csb.ConnectionString))
		{
			connection.Open();
			connection2.Open();
			AssertMeasurement("db.client.connections.usage", 2);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 2);
			Assert.Equal(2, Server.ActiveConnections);
		}

		AssertMeasurement("db.client.connections.usage", 2);
		AssertMeasurement("db.client.connections.usage|idle", 2);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(2, Server.ActiveConnections);
	}

	[Fact(Skip = MetricsSkip)]
	public async Task PendingRequestForCreation()
	{
		var csb = CreateConnectionStringBuilder();
		PoolName = csb.GetConnectionString(includePassword: false);
		Server.ConnectDelay = TimeSpan.FromSeconds(0.5);

		AssertMeasurement("db.client.connections.pending_requests", 0);

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		var openTask = connection.OpenAsync();
		AssertMeasurement("db.client.connections.pending_requests", 1);
		await openTask;

		AssertMeasurement("db.client.connections.pending_requests", 0);
	}

	[Fact(Skip = MetricsSkip)]
	public async Task PendingRequestForOpenFromPool()
	{
		var csb = CreateConnectionStringBuilder();
		PoolName = csb.GetConnectionString(includePassword: false);
		Server.ResetDelay = TimeSpan.FromSeconds(0.5);

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await connection.OpenAsync();
		connection.Close();

		AssertMeasurement("db.client.connections.pending_requests", 0);

		var openTask = connection.OpenAsync();
		AssertMeasurement("db.client.connections.pending_requests", 1);
		await openTask;

		AssertMeasurement("db.client.connections.pending_requests", 0);
	}
}
