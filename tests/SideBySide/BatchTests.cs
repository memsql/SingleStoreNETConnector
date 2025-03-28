#if !BASELINE

namespace SideBySide;

public class BatchTests : IClassFixture<DatabaseFixture>
{
	public BatchTests(DatabaseFixture database)
	{
	}

	[Fact]
	public void CanCreateParameter()
	{
		Assert.True(new SingleStoreBatchCommand().CanCreateParameter);
	}

	[Fact]
	public void CreateParameter()
	{
		Assert.IsType<SingleStoreParameter>(new SingleStoreBatchCommand().CreateParameter());
	}

	[Fact]
	public void NeedsConnection()
	{
		using var batch = new SingleStoreBatch
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT 1;"),
			},
		};
		Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
	}

	[Fact]
	public void NeedsOpenConnection()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT 1;"),
			},
		};
		Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
	}

	[Fact]
	public void NeedsCommands()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection);
		Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
	}

	[Fact]
	public void NeedsNonNullCommands()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands = { null },
		};
		Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
	}

	[Fact]
	public void NeedsCommandsWithCommandText()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands = { new SingleStoreBatchCommand() },
		};
		Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
	}

	[Fact]
	public void NotDisposed()
	{
		using var batch = new SingleStoreBatch();
		batch.Dispose();
		Assert.Throws<ObjectDisposedException>(() => batch.ExecuteNonQuery());
	}

	[Fact]
	public void CloseConnection()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT 1;"),
			},
		};
		using (var reader = batch.ExecuteReader(CommandBehavior.CloseConnection))
		{
			while (reader.Read())
			{
			}

			Assert.Equal(ConnectionState.Open, connection.State);
		}
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[Fact]
	public void CreateBatchDoesNotSetTransaction()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var transaction = connection.BeginTransaction();
		using var batch = connection.CreateBatch();
		Assert.Null(batch.Transaction);
	}

	[Fact]
	public void BatchTransactionMustBeSet()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var transaction = connection.BeginTransaction();
		using var batch = connection.CreateBatch();
		batch.BatchCommands.Add(new SingleStoreBatchCommand("SELECT 1;"));
		Assert.Throws<InvalidOperationException>(() => batch.ExecuteScalar());

		batch.Transaction = transaction;
		TestUtilities.AssertIsOne(batch.ExecuteScalar());
	}

	[Fact]
	public void IgnoreBatchTransactionIgnoresNull()
	{
		using var connection = new SingleStoreConnection(GetIgnoreCommandTransactionConnectionString());
		connection.Open();
		using var transaction = connection.BeginTransaction();
		using var batch = connection.CreateBatch();
		batch.BatchCommands.Add(new SingleStoreBatchCommand("SELECT 1;"));
		TestUtilities.AssertIsOne(batch.ExecuteScalar());
	}

	[Fact]
	public void IgnoreCommandTransactionIgnoresDisposedTransaction()
	{
		using var connection = new SingleStoreConnection(GetIgnoreCommandTransactionConnectionString());
		connection.Open();

		var transaction = connection.BeginTransaction();
		transaction.Commit();
		transaction.Dispose();

		using var batch = connection.CreateBatch();
		batch.BatchCommands.Add(new SingleStoreBatchCommand("SELECT 1;"));
		batch.Transaction = transaction;
		TestUtilities.AssertIsOne(batch.ExecuteScalar());
	}

	[Fact]
	public void IgnoreCommandTransactionIgnoresDifferentTransaction()
	{
		using var connection1 = new SingleStoreConnection(AppConfig.ConnectionString);
		using var connection2 = new SingleStoreConnection(GetIgnoreCommandTransactionConnectionString());
		connection1.Open();
		connection2.Open();
		using var transaction1 = connection1.BeginTransaction();
		using var batch2 = connection2.CreateBatch();
		batch2.Transaction = transaction1;
		batch2.BatchCommands.Add(new SingleStoreBatchCommand("SELECT 1;"));
		TestUtilities.AssertIsOne(batch2.ExecuteScalar());
	}

	[Theory]
	[InlineData("")]
	[InlineData("\n")]
	[InlineData(";")]
	[InlineData(";\n")]
	[InlineData("; -- ")]
	// [InlineData(" -- ")]  TODO: uncomment if DB-53659 is done
	[InlineData(" # ")]
	public void ExecuteBatch(string suffix)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT 10; SELECT 1" + suffix),
				new SingleStoreBatchCommand("SELECT 2" + suffix),
				new SingleStoreBatchCommand("SELECT 3" + suffix),
			},
		};
		using var reader = batch.ExecuteReader();
		var total = 0;

		Assert.True(reader.Read());
		total += reader.GetInt32(0);
		Assert.False(reader.Read());
		Assert.True(reader.NextResult());

		Assert.True(reader.Read());
		total += reader.GetInt32(0);
		Assert.False(reader.Read());
		Assert.True(reader.NextResult());

		Assert.True(reader.Read());
		total += reader.GetInt32(0);
		Assert.False(reader.Read());
		Assert.True(reader.NextResult());

		Assert.True(reader.Read());
		total += reader.GetInt32(0);
		Assert.False(reader.Read());
		Assert.False(reader.NextResult());

		Assert.Equal(16, total);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SingleRow(bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using (var command = new SingleStoreCommand(@"drop table if exists batch_single_row;
create table batch_single_row(id integer not null primary key);
insert into batch_single_row(id) values(1),(2),(3);", connection))
		{
			command.ExecuteNonQuery();
		}

		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT id FROM batch_single_row ORDER BY id; SELECT 10"),
				new SingleStoreBatchCommand("SELECT id FROM batch_single_row ORDER BY id"),
			},
		};

		if (prepare)
			batch.Prepare();

		using (var reader = batch.ExecuteReader(CommandBehavior.SingleRow))
		{
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(reader.Read());
			Assert.False(reader.NextResult());
		}

		using (var reader = batch.ExecuteReader())
		{
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.True(reader.Read());
			Assert.Equal(2, reader.GetInt32(0));
			Assert.True(reader.Read());
			Assert.Equal(3, reader.GetInt32(0));
			Assert.False(reader.Read());
			Assert.True(reader.NextResult());
			Assert.True(reader.Read());
			Assert.Equal(10, reader.GetInt32(0));
			Assert.True(reader.NextResult());
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.True(reader.Read());
			Assert.Equal(2, reader.GetInt32(0));
			Assert.True(reader.Read());
			Assert.Equal(3, reader.GetInt32(0));
			Assert.False(reader.Read());
		}
	}

	[Fact]
	public void PrepareNeedsConnection()
	{
		using var batch = new SingleStoreBatch
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT 1;"),
			},
		};
		Assert.Throws<InvalidOperationException>(() => batch.Prepare());
	}

	[Fact]
	public void PrepareNeedsOpenConnection()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands =
			{
				new SingleStoreBatchCommand("SELECT 1;"),
			},
		};
		Assert.Throws<InvalidOperationException>(() => batch.Prepare());
	}

	[Fact]
	public void PrepareNeedsCommands()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection);
		Assert.Throws<InvalidOperationException>(() => batch.Prepare());
	}

	[Fact]
	public void PrepareNeedsNonNullCommands()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands = { null },
		};
		Assert.Throws<InvalidOperationException>(() => batch.Prepare());
	}

	[Fact]
	public void PrepareNeedsCommandsWithText()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		using var batch = new SingleStoreBatch(connection)
		{
			BatchCommands = { new SingleStoreBatchCommand() },
		};
		Assert.Throws<InvalidOperationException>(() => batch.Prepare());
	}

	private static string GetIgnoreCommandTransactionConnectionString() =>
		new SingleStoreConnectionStringBuilder(AppConfig.ConnectionString)
		{
			IgnoreCommandTransaction = true
		}.ConnectionString;
}
#endif
