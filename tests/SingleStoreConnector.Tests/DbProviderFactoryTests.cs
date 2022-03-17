#if BASELINE
using MySql.Data.MySqlClient;
using SingleStoreConnectorFactory = MySql.Data.MySqlClient.MySqlClientFactory;
#endif
using Xunit;

namespace SingleStoreConnector.Tests;

public class DbProviderFactoryTests
{
	[Fact]
	public void CreatesExpectedTypes()
	{
		Assert.IsType<SingleStoreConnection>(SingleStoreConnectorFactory.Instance.CreateConnection());
		Assert.IsType<SingleStoreConnectionStringBuilder>(SingleStoreConnectorFactory.Instance.CreateConnectionStringBuilder());
		Assert.IsType<SingleStoreCommand>(SingleStoreConnectorFactory.Instance.CreateCommand());
		Assert.IsType<SingleStoreCommandBuilder>(SingleStoreConnectorFactory.Instance.CreateCommandBuilder());
		Assert.IsType<SingleStoreDataAdapter>(SingleStoreConnectorFactory.Instance.CreateDataAdapter());
		Assert.IsType<SingleStoreParameter>(SingleStoreConnectorFactory.Instance.CreateParameter());
	}

	[Fact]
	public void CanCreateDataSourceEnumerator() => Assert.False(SingleStoreConnectorFactory.Instance.CanCreateDataSourceEnumerator);

	[Fact]
	public void Singleton()
	{
		var factory1 = SingleStoreConnectorFactory.Instance;
		var factory2 = SingleStoreConnectorFactory.Instance;
		Assert.True(object.ReferenceEquals(factory1, factory2));
	}
}
