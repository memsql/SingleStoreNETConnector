using System;
using System.IO;
using System.Data.Common;
using AdoNet.Specification.Tests;
using SingleStoreConnector;

namespace Conformance.Tests;

public class DbFactoryFixture : IDbFactoryFixture
	{
		public DbFactoryFixture()
		{
			var envCs = Environment.GetEnvironmentVariable("CONNECTION_STRING");
			if (!string.IsNullOrEmpty(envCs))
			{
				ConnectionString = envCs;
				return;
			}

			var sqlUserPassword = Environment.GetEnvironmentVariable("SQL_USER_PASSWORD") ?? "pass";
			var homeDir = Environment.GetEnvironmentVariable("HOMEPATH")
			              ?? Environment.GetEnvironmentVariable("HOME")
			              ?? "";
			var connFile = Path.Combine(homeDir, "CONNECTION_STRING");

			if (File.Exists(connFile))
			{
				try
				{
					var fileCs = File.ReadAllText(connFile).Trim();
					if (!string.IsNullOrEmpty(fileCs))
					{
						ConnectionString = fileCs;
						return;
					}
				}
				catch
				{
					// ignore and fall back
				}
			}

			ConnectionString = $"Server=localhost;Port=3306;User Id=root;Password={sqlUserPassword};SSL Mode=None";
		}

		public string ConnectionString { get; }
		public DbProviderFactory Factory => SingleStoreConnectorFactory.Instance;
	}
