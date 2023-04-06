#if BASELINE
using SingleStoreConnectorFactory = MySql.Data.MySqlClient.MySqlClientFactory;
#endif

namespace SideBySide;

public class ClientFactoryTests
{
	[Fact]
	public void CreateCommand()
	{
		Assert.IsType<SingleStoreCommand>(SingleStoreConnectorFactory.Instance.CreateCommand());
	}

	[Fact]
	public void CreateConnection()
	{
		Assert.IsType<SingleStoreConnection>(SingleStoreConnectorFactory.Instance.CreateConnection());
	}

	[Fact]
	public void CreateConnectionStringBuilder()
	{
		Assert.IsType<SingleStoreConnectionStringBuilder>(SingleStoreConnectorFactory.Instance.CreateConnectionStringBuilder());
	}


	[Fact]
	public void CreateParameter()
	{
		Assert.IsType<SingleStoreParameter>(SingleStoreConnectorFactory.Instance.CreateParameter());
	}

	[Fact]
	public void CreateCommandBuilder()
	{
		Assert.IsType<SingleStoreCommandBuilder>(SingleStoreConnectorFactory.Instance.CreateCommandBuilder());
	}

	[Fact]
	public void CreateDataAdapter()
	{
		Assert.IsType<SingleStoreDataAdapter>(SingleStoreConnectorFactory.Instance.CreateDataAdapter());
	}

	[Fact]
	public void DbProviderFactoriesGetFactory()
	{
#if BASELINE
		var providerInvariantName = "MySql.Data.MySqlClient";
#else
		var providerInvariantName = "SingleStoreConnector";
#endif
#if !NET462 && !NET472
		DbProviderFactories.RegisterFactory(providerInvariantName, SingleStoreConnectorFactory.Instance);
#endif
		var factory = DbProviderFactories.GetFactory(providerInvariantName);
		Assert.NotNull(factory);
		Assert.Same(SingleStoreConnectorFactory.Instance, factory);

		using (var connection = new SingleStoreConnection())
		{
			factory = System.Data.Common.DbProviderFactories.GetFactory(connection);
			Assert.NotNull(factory);
			Assert.Same(SingleStoreConnectorFactory.Instance, factory);
		}
	}
}
