namespace SingleStoreConnector.DependencyInjection.Tests;

public class DependencyInjectionTests
{
	[Fact]
	public async Task SingleStoreDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleStoreDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredService<SingleStoreDataSource>();
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task SingleStoreConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleStoreDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredService<SingleStoreConnection>();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task DbConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleStoreDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredService<DbConnection>();
		Assert.IsAssignableFrom<SingleStoreConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task DbDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleStoreDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var dataSource = serviceProvider.GetRequiredService<DbDataSource>();
		Assert.IsAssignableFrom<SingleStoreDataSource>(dataSource);
		await using var connection = dataSource.CreateConnection();
		Assert.IsAssignableFrom<SingleStoreConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task SingleStoreDataSourceCanSetName()
	{
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddSingleStoreDataSource(c_connectionString, builder =>
		{
			builder.UseName("MyName");
		});

		await using var serviceProvider = serviceCollection.BuildServiceProvider();
		var dataSource = serviceProvider.GetRequiredService<SingleStoreDataSource>();
		Assert.Equal("MyName", dataSource.Name);
	}

	[Fact]
	public async Task SingleStoreDataSourceCanSetNameFromServiceProvider()
	{
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddSingleton("MyName");
		serviceCollection.AddSingleStoreDataSource(c_connectionString, (sp, builder) =>
		{
			builder.UseName(sp.GetRequiredService<string>());
		});

		await using var serviceProvider = serviceCollection.BuildServiceProvider();
		var dataSource = serviceProvider.GetRequiredService<SingleStoreDataSource>();
		Assert.Equal("MyName", dataSource.Name);
	}

	[Fact]
	public async Task KeyedSingleStoreDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource(KeyedService.AnyKey, c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<SingleStoreDataSource>(new object());
		Assert.Null(dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task StringKeyedSingleStoreDataSourceHasNameSet()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<SingleStoreDataSource>("key");
		Assert.Equal("key", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedSingleStoreDataSourceCanSetName()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource("key", c_connectionString, builder => builder.UseName("MyName"));

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<SingleStoreDataSource>("key");
		Assert.Equal("MyName", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedSingleStoreDataSourceCanSetNameFromServiceProvider()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton("MyName");
		serviceCollection.AddKeyedSingleStoreDataSource("key", c_connectionString, (sp, builder) => builder.UseName(sp.GetRequiredService<string>()));

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<SingleStoreDataSource>("key");
		Assert.Equal("MyName", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedSingleStoreDataSourceRetrievedWithStringKeyHasName()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource(KeyedService.AnyKey, c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<SingleStoreDataSource>("key");
		Assert.Equal("key", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedSingleStoreConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredKeyedService<SingleStoreConnection>("key");
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task TwoKeyedSingleStoreDataConnectionsAreRegistered()
	{
		const string c_connectionString2 = c_connectionString + ";Database=test";

		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource(KeyedService.AnyKey, c_connectionString);
		serviceCollection.AddKeyedSingleStoreDataSource("key2", c_connectionString2);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection1 = serviceProvider.GetRequiredKeyedService<SingleStoreConnection>("key");
		Assert.Equal(c_connectionString, connection1.ConnectionString);

		await using var connection2 = serviceProvider.GetRequiredKeyedService<SingleStoreConnection>("key2");
		Assert.Equal(c_connectionString2, connection2.ConnectionString);
	}

	[Fact]
	public async Task KeyedDbConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredKeyedService<DbConnection>("key");
		Assert.IsAssignableFrom<SingleStoreConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedDbDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedSingleStoreDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var dataSource = serviceProvider.GetRequiredKeyedService<DbDataSource>("key");
		Assert.IsAssignableFrom<SingleStoreDataSource>(dataSource);
		await using var connection = dataSource.CreateConnection();
		Assert.IsAssignableFrom<SingleStoreConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	const string c_connectionString = "Server=localhost;User ID=root;Password=pass";
}
