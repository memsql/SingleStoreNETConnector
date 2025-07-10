using System;
using System.Data.Common;
using AdoNet.Specification.Tests;
using SingleStoreConnector;

namespace Conformance.Tests;

public class DbFactoryFixture : IDbFactoryFixture
	{
		public DbFactoryFixture()
		{
			String sqlUserPassword = Environment.GetEnvironmentVariable("SQL_USER_PASSWORD") ?? "pass";

			String home = Environment.GetEnvironmentVariable("HOMEPATH") ?? "~";
			String connectionStringFile = System.IO.Path.Join(home, "CONNECTION_STRING");

			string connectionString;
			try
			{
				connectionString = System.IO.File.ReadAllText(connectionStringFile);
			}
			catch (System.Exception)
			{
				connectionString = "";
			}

			ConnectionString = connectionString.Length > 0 ? connectionString : String.Format("Server=localhost;Port=3306;User Id=root;Password={0};SSL Mode=None", sqlUserPassword);
		}

		public string ConnectionString { get; }
		public DbProviderFactory Factory => SingleStoreConnectorFactory.Instance;
	}
