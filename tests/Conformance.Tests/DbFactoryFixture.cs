using System;
using System.Data.Common;
using AdoNet.Specification.Tests;
using SingleStoreConnector;

namespace Conformance.Tests;

public class DbFactoryFixture : IDbFactoryFixture
{
	public DbFactoryFixture()
	{
		var sqlUserPassword = Environment.GetEnvironmentVariable("SQL_USER_PASSWORD") ?? "pass";
		ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? $"Server=localhost;User Id=root;Password={sqlUserPassword};SSL Mode=None;AllowPublicKeyRetrieval=true";
	}

	public string ConnectionString { get; }
	public DbProviderFactory Factory => SingleStoreConnectorFactory.Instance;
}
