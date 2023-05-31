namespace SideBySide;

public class ConnectionTests : IClassFixture<DatabaseFixture>
{
	public ConnectionTests(DatabaseFixture database)
	{
	}

	[Fact]
	public void GotInfoMessageForNonExistentTable()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		var gotError = false;

		try
		{
			connection.Execute(@"drop table table_does_not_exist");
		}
		catch (Exception ex)
		{
			Assert.Contains("Unknown table", ex.Message);
			gotError = true;
		}
		Assert.True(gotError);
	}

	[Fact]
	public void GotInfoMessageForNonExistentTableInTransaction()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var transaction = connection.BeginTransaction();
		var gotError = false;

		try
		{
			connection.Execute(@"drop table table_does_not_exist;", transaction: transaction);
		}
		catch (Exception ex)
		{
			Assert.Contains("Unknown table", ex.Message);
			gotError = true;
		}
		Assert.True(gotError);
	}

	[Fact]
	public async void MultipleConnectionsSanity()
	{
		for (int i = 0; i < 10; i++)
		{
			using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
			connection.Open();

			using var command = connection.CreateCommand();

			command.CommandText = $"create table if not exists t (a int)";
			var reader = await command.ExecuteReaderAsync();
			reader.Close();

			command.CommandText = $"select a from t;";
			reader = await command.ExecuteReaderAsync();
			reader.Close();
		}
	}

	[Fact]
	public void NoInfoMessageWhenNotLastStatementInBatch()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();

		var gotEvent = false;
		connection.InfoMessage += (s, a) =>
		{
			gotEvent = true;

#if BASELINE
			// seeming bug in Connector/NET raises an event with no errors
			Assert.Empty(a.errors);
#endif
		};

		connection.Execute(@"drop table if exists table_does_not_exist; select 1;");
#if BASELINE
		Assert.True(gotEvent);
#else
		Assert.False(gotEvent);
#endif
	}

	[Fact]
	public void DefaultConnectionStringIsEmpty()
	{
		using var connection = new SingleStoreConnection();
		Assert.Equal("", connection.ConnectionString);
	}

	[Fact]
	public void InitializeWithNullConnectionString()
	{
		using var connection = new SingleStoreConnection(default(string));
		Assert.Equal("", connection.ConnectionString);
	}

	[Fact]
	public void SetConnectionStringToNull()
	{
		using var connection = new SingleStoreConnection();
		connection.ConnectionString = null;
		Assert.Equal("", connection.ConnectionString);
	}

	[Fact]
	public void SetConnectionStringToEmptyString()
	{
		using var connection = new SingleStoreConnection();
		connection.ConnectionString = "";
		Assert.Equal("", connection.ConnectionString);
	}

	[SkippableFact(Baseline = "Throws NullReferenceException")]
	public void ServerVersionThrows()
	{
		using var connection = new SingleStoreConnection();
		Assert.Throws<InvalidOperationException>(() => connection.ServerVersion);
	}

	[SkippableFact(Baseline = "Throws NullReferenceException")]
	public void ServerThreadThrows()
	{
		using var connection = new SingleStoreConnection();
		Assert.Throws<InvalidOperationException>(() => connection.ServerThread);
	}

	[Fact]
	public void DatabaseIsEmptyString()
	{
		using var connection = new SingleStoreConnection();
		Assert.Equal("", connection.Database);
	}

	[Fact]
	public void DataSourceIsEmptyString()
	{
		using var connection = new SingleStoreConnection();
		Assert.Equal("", connection.DataSource);
	}

	[Fact]
	public void ConnectionTimeoutDefaultValue()
	{
		using var connection = new SingleStoreConnection();
		Assert.Equal(15, connection.ConnectionTimeout);
	}

	[Fact]
	public void ConnectionTimeoutDefaultValueAfterOpen()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		Assert.Equal(15, connection.ConnectionTimeout);
	}

	[Fact]
	public void ConnectionTimeoutExplicitValue()
	{
		using var connection = new SingleStoreConnection("Connection Timeout=30");
		Assert.Equal(30, connection.ConnectionTimeout);
	}

	[Fact]
	public void ConnectionTimeoutExplicitValueAfterOpen()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.ConnectionTimeout = 30;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();
		Assert.Equal(30, connection.ConnectionTimeout);
	}

	[Fact]
	public void CloneClonesConnectionString()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		using var connection2 = (SingleStoreConnection) connection.Clone();
		Assert.Equal(connection.ConnectionString, connection2.ConnectionString);
#if !BASELINE
		Assert.Equal(AppConfig.ConnectionString, connection2.ConnectionString);
#endif
	}

	[Fact]
	public void CloneIsClosed()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var connection2 = (SingleStoreConnection) connection.Clone();
		Assert.Equal(ConnectionState.Closed, connection2.State);
	}

	[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=97473")]
	public void CloneDoesNotDisclosePassword()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var connection2 = (SingleStoreConnection) connection.Clone();
		Assert.Equal(connection.ConnectionString, connection2.ConnectionString);
		Assert.DoesNotContain("password", connection2.ConnectionString, StringComparison.OrdinalIgnoreCase);
	}

#if !BASELINE
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CloneWithUsesNewConnectionString(bool openConnection)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		if (openConnection)
			connection.Open();
		using var connection2 = connection.CloneWith("user=root;password=pass;server=example.com;database=test");
		Assert.Equal("Server=example.com;User ID=root;Password=pass;Database=test", connection2.ConnectionString);
	}

	[Fact]
	public void CloneWithUsesExistingPassword()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		var newConnectionString = "user=root;server=example.com;database=test";
		using var connection2 = connection.CloneWith(newConnectionString);

		var builder = new SingleStoreConnectionStringBuilder(newConnectionString);
		builder.Password = AppConfig.CreateConnectionStringBuilder().Password;
		Assert.Equal(builder.ConnectionString, connection2.ConnectionString);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CloneWithDoesNotDiscloseExistingPassword(bool persistSecurityInfo)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();

		var newConnectionString = "user=root;server=example.com;database=test;Persist Security Info=" + persistSecurityInfo;
		using var connection2 = connection.CloneWith(newConnectionString);

		var builder = new SingleStoreConnectionStringBuilder(newConnectionString);
		Assert.Equal(builder.ConnectionString, connection2.ConnectionString);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void CloneWithDoesDiscloseExistingPasswordIfPersistSecurityInfo(bool persistSecurityInfo)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString + ";Persist Security Info=true");
		connection.Open();

		var newConnectionString = "user=root;server=example.com;database=test;Persist Security Info=" + persistSecurityInfo;
		using var connection2 = connection.CloneWith(newConnectionString);

		var builder = new SingleStoreConnectionStringBuilder(newConnectionString);
		builder.Password = AppConfig.CreateConnectionStringBuilder().Password;
		Assert.Equal(builder.ConnectionString, connection2.ConnectionString);
	}

	[Fact]
	public void CloneWithCopiesExistingPassword()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();

		var builder = AppConfig.CreateConnectionStringBuilder();
		builder.Password = "";
		using var connection2 = connection.CloneWith(builder.ConnectionString);
		connection2.Open();
		Assert.Equal(ConnectionState.Open, connection2.State);
	}

	[Fact]
	public void ConnectionAttributes()
	{
		var attrs = "ConnectionAttributes=my_key_1:my_val_1,my_key_2:my_val_2";
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString + ";" + attrs);
		connection.Open();

		if (connection.Session.S2ServerVersion.Version.CompareTo(new Version(8, 1, 0)) >= 0)
		{
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "SELECT ATTRIBUTE_NAME, ATTRIBUTE_VALUE FROM information_schema.mv_connection_attributes;";

			using var reader = cmd.ExecuteReader();
			HashSet<Tuple<string, string>> dbConnAttrs = new HashSet<Tuple<string, string>>();
			while (reader.Read())
			{
				dbConnAttrs.Add(new Tuple<string, string>(reader.GetString(0), reader.GetString(1)));
			}
			Tuple<string, string> expectedPair = new Tuple<string, string>("my_key_1", "my_val_1");
			Assert.Contains(expectedPair, dbConnAttrs);
			expectedPair = new Tuple<string, string>("my_key_2", "my_val_2");
			Assert.Contains(expectedPair, dbConnAttrs);
			expectedPair = new Tuple<string, string>("_client_name", "SingleStore .NET Connector");
			Assert.Contains(expectedPair, dbConnAttrs);
		}
	}

	[Fact]
	public async Task ResetConnectionThrowsIfNotOpen()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		await Assert.ThrowsAsync<InvalidOperationException>(async () => await connection.ResetConnectionAsync());
	}

	[SkippableFact(ServerFeatures.ResetConnection)]
	public async Task ResetConnectionClearsUserVariables()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.AllowUserVariables = true;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await connection.OpenAsync();

		Version resetSupportVersion = new(7, 5, 0);
		if (connection.Session.S2ServerVersion.Version.CompareTo(resetSupportVersion) < 0)
			return;

		connection.Execute("select 1 into @temp_var;");
		var tempVar = connection.ExecuteScalar<int?>("select @temp_var;");
		Assert.Equal(1, tempVar);

		bool resetSuccess = false;
		await connection.ResetConnectionAsync();

		try
		{
			tempVar = connection.ExecuteScalar<int?>("select @temp_var;");
		}
		catch (SingleStoreConnector.SingleStoreException ex)
		{
			// if connection has been reset, select @temp_var results in an error
			resetSuccess = true;
			Assert.Contains("Unknown user-defined variable", ex.Message);
		}
		Assert.True(resetSuccess);
	}
#endif
}
