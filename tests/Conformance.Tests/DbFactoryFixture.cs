using System;
using System.Data.Common;
using System.IO;
using AdoNet.Specification.Tests;
using SingleStoreConnector;

namespace Conformance.Tests;

public class DbFactoryFixture : IDbFactoryFixture
	{
		public DbFactoryFixture()
		{
			// 1) Try a full connection string from env-var
			var envCs = Environment.GetEnvironmentVariable("CONNECTION_STRING");
			if (!string.IsNullOrEmpty(envCs))
			{
				ConnectionString = envCs;
				return;
			}

			// 2) Otherwise try reading the file from the user's home directory
			var sqlUserPassword = Environment.GetEnvironmentVariable("SQL_USER_PASSWORD") ?? "pass";
			// On Windows HOMEPATH is like "\Users\runneradmin"; on Unix HOME is "/home/runner"
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

			// 3) Fallback to localhost
			ConnectionString =
				$"Server=localhost;Port=3306;User Id=root;Password={sqlUserPassword};SSL Mode=None";
		}
		/*public DbFactoryFixture()
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
		}*/

		public string ConnectionString { get; }
		public DbProviderFactory Factory => SingleStoreConnectorFactory.Instance;
	}
