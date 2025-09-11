namespace SingleStoreConnector.Tests.Metrics;

public class ConnectTimeoutTests : MetricsTestsBase
{
	[Fact(Skip = MetricsSkip)]
	public async Task ConnectTimeout()
	{
		var csb = CreateConnectionStringBuilder();
		csb.ConnectionTimeout = 1;
		csb.Server = "www.example.com";
		PoolName = csb.GetConnectionString(includePassword: false);

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<SingleStoreException>(connection.OpenAsync);

		AssertMeasurement("db.client.connections.timeouts", 1);
	}

	[Fact(Skip = MetricsSkip)]
	public async Task DataSourceConnectTimeout()
	{
		var csb = CreateConnectionStringBuilder();
		csb.ConnectionTimeout = 1;
		csb.Server = "www.example.com";

		PoolName = "timeout";
		using var dataSource = new SingleStoreDataSourceBuilder(csb.ConnectionString)
			.UseName(PoolName)
			.Build();

		await Assert.ThrowsAsync<SingleStoreException>(async () => await dataSource.OpenConnectionAsync());

		AssertMeasurement("db.client.connections.timeouts", 1);
	}

	[Fact(Skip = MetricsSkip)]
	public async Task NoPoolConnectTimeout()
	{
		var csb = CreateConnectionStringBuilder();
		csb.ConnectionTimeout = 1;
		csb.Server = "www.example.com";
		csb.Pooling = false;
		PoolName = csb.GetConnectionString(includePassword: false);

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<SingleStoreException>(connection.OpenAsync);

		AssertMeasurement("db.client.connections.timeouts", 1);
	}
}
