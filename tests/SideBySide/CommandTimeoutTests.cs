namespace SideBySide;

public class CommandTimeoutTests : IClassFixture<DatabaseFixture>, IDisposable
{
	public CommandTimeoutTests(DatabaseFixture database)
	{
		m_database = database;
		m_connection = new SingleStoreConnection(m_database.Connection.ConnectionString);
		m_connection.Open();
	}

	public void Dispose()
	{
		m_connection.Dispose();
	}

	[Theory]
	[InlineData(3)]
	[InlineData(13)]
	public void DefaultCommandTimeoutIsInherited(int defaultCommandTimeout)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.DefaultCommandTimeout = (uint) defaultCommandTimeout;

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		using var command = connection.CreateCommand();
		Assert.Equal(defaultCommandTimeout, command.CommandTimeout);
	}

	[Fact]
	public void NegativeCommandTimeout()
	{
		using var command = m_connection.CreateCommand();
#if BASELINE
		Assert.Throws<ArgumentException>(() => command.CommandTimeout = -1);
#else
		Assert.Throws<ArgumentOutOfRangeException>(() => command.CommandTimeout = -1);
#endif
	}

	[Fact]
	public void LargeCommandTimeoutIsCoerced()
	{
		using var command = m_connection.CreateCommand();
		command.CommandTimeout = 2_000_000_000;
		Assert.Equal(2_147_483, command.CommandTimeout);
	}

	[Fact]
	public void CommandTimeoutWithSleepSync()
	{
		var connectionState = m_connection.State;
		using (var cmd = new SingleStoreCommand("SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if BASELINE
			var ex = Assert.Throws<SingleStoreException>(cmd.ExecuteReader);
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = cmd.ExecuteReader())
			{
				var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
				Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
			}
#endif
			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public async Task CommandTimeoutWithSleepAsync()
	{
		var connectionState = m_connection.State;
		using (var cmd = new SingleStoreCommand("SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if BASELINE
			var exception = await Assert.ThrowsAsync<SingleStoreException>(cmd.ExecuteReaderAsync);
			Assert.Contains("fatal error", exception.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = cmd.ExecuteReader())
			{
				var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
				Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
			}
#endif
			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 700);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void CommandTimeoutWithStoredProcedureSleepSync(bool pooling)
	{
		using (var setupCmd = new SingleStoreCommand(@"drop procedure if exists sleep_sproc;
create procedure sleep_sproc(seconds INT) as
begin
echo select sleep(seconds);
end;", m_connection))
		{
			setupCmd.ExecuteNonQuery();
		}

		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = pooling;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		using var cmd = new SingleStoreCommand("sleep_sproc", connection);
		connection.Open();
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddWithValue("seconds", 10);
		cmd.CommandTimeout = 2;

		var sw = Stopwatch.StartNew();
#if BASELINE
		var ex = Assert.Throws<SingleStoreException>(cmd.ExecuteReader);
		Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
#else
		using (var reader = cmd.ExecuteReader())
		{
			var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
			Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
		}
#endif
		sw.Stop();
		TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public void MultipleCommandTimeoutWithSleepSync()
	{
		var connectionState = m_connection.State;
		var csb = new SingleStoreConnectionStringBuilder(m_connection.ConnectionString);
		using (var cmd = new SingleStoreCommand("SELECT 1; SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(reader.Read());

#if BASELINE
			var ex = Assert.Throws<SingleStoreException>(() => reader.NextResult());
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			Assert.True(reader.NextResult());
			var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
			Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
#endif

			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public async Task MultipleCommandTimeoutWithSleepAsync()
	{
		var connectionState = m_connection.State;
		using (var cmd = new SingleStoreCommand("SELECT 1; SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
			using var reader = await cmd.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(await reader.ReadAsync());

#if BASELINE
			var ex = await Assert.ThrowsAsync<SingleStoreException>(async () => await reader.NextResultAsync());
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			Assert.True(await reader.NextResultAsync());
			var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
			Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
#endif

			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 550);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.Timeout, Baseline = "https://bugs.mysql.com/bug.php?id=88124")]
	public void CommandTimeoutResetsOnReadSync()
	{
		var csb = new SingleStoreConnectionStringBuilder(m_connection.ConnectionString);
		using (var cmd = new SingleStoreCommand("SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1);", m_connection))
		{
			cmd.CommandTimeout = 3;
			using var reader = cmd.ExecuteReader();

			for (int i = 0; i < 5; i++)
			{
				Assert.True(reader.Read());
				Assert.Equal(0, reader.GetInt32(0));
				Assert.False(reader.Read());
				Assert.Equal(i < 4, reader.NextResult());
			}
		}

		Assert.Equal(ConnectionState.Open, m_connection.State);
	}

	[SkippableFact(ServerFeatures.Timeout, Baseline = "https://bugs.mysql.com/bug.php?id=88124")]
	public async Task CommandTimeoutResetsOnReadAsync()
	{
		var csb = new SingleStoreConnectionStringBuilder(m_connection.ConnectionString);
		using (var cmd = new SingleStoreCommand("SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1);", m_connection))
		{
			cmd.CommandTimeout = 3;
			using var reader = await cmd.ExecuteReaderAsync();

			for (int i = 0; i < 5; i++)
			{
				Assert.True(await reader.ReadAsync());
				Assert.Equal(0, reader.GetInt32(0));
				Assert.False(await reader.ReadAsync());
				Assert.Equal(i < 4, await reader.NextResultAsync());
			}
		}

		Assert.Equal(ConnectionState.Open, m_connection.State);
	}


	[Fact]
	public void TransactionCommandTimeoutWithSleepSync()
	{
		var connectionState = m_connection.State;
		using (var transaction = m_connection.BeginTransaction())
		using (var cmd = new SingleStoreCommand("SELECT SLEEP(120);", m_connection, transaction))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if BASELINE
			var ex = Assert.Throws<SingleStoreException>(cmd.ExecuteReader);
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = cmd.ExecuteReader())
			{
				var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
				Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
			}
#endif
			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public async Task TransactionCommandTimeoutWithSleepAsync()
	{
		var connectionState = m_connection.State;
		using (var transaction = await m_connection.BeginTransactionAsync())
		using (var cmd = new SingleStoreCommand("SELECT SLEEP(120);", m_connection, transaction))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if BASELINE
			var ex = await Assert.ThrowsAsync<SingleStoreException>(cmd.ExecuteReaderAsync);
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = cmd.ExecuteReader())
			{
				var ex = Assert.Throws<SingleStoreException>(() => reader.Read());
				Assert.Equal("The Command Timeout expired before the operation completed.", ex.Message);
			}
#endif
			sw.Stop();
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	readonly DatabaseFixture m_database;
	readonly SingleStoreConnection m_connection;
}
