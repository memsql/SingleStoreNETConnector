using System;
using System.Data.Common;
using AdoNet.Specification.Tests;
using SingleStoreConnector;

namespace Conformance.Tests;

public class DbFactoryFixture : IDbFactoryFixture
	{
		public DbFactoryFixture()
		{
			String rootPassword = Environment.GetEnvironmentVariable("SQL_USER_PASSWORD") ?? "pass";
			ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? String.Format("Server=localhost;User Id=root;Password={0};SSL Mode=None", rootPassword);
		}

		public string ConnectionString { get; }
		public DbProviderFactory Factory => SingleStoreConnectorFactory.Instance;
	}
