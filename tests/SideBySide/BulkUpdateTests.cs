namespace SideBySide;

public class BulkUpdateTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdatesMatchingRowsWithSingleKey(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_basic;
create table bulk_update_basic(id int primary key, name varchar(100), status varchar(50));
insert into bulk_update_basic values (1, 'Alice', 'active'), (2, 'Bob', 'active'), (3, 'Charlie', 'inactive');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("status", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "inactive" },
				new object[] { 2, "inactive" },
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_basic",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "status"),
			},
		};

		var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);

		Assert.Equal(2, result.RowsStaged);
		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select status from bulk_update_basic order by id;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal("inactive", reader.GetString(0));
		Assert.True(await reader.ReadAsync());
		Assert.Equal("inactive", reader.GetString(0));
		Assert.True(await reader.ReadAsync());
		Assert.Equal("inactive", reader.GetString(0)); // unchanged (was already inactive)
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdatesMatchingRowsWithCompositeKey(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_composite;
create table bulk_update_composite(tenant_id int, user_id int, email varchar(100), primary key (tenant_id, user_id));
insert into bulk_update_composite values (1, 100, 'user100@tenant1.com'), (1, 101, 'user101@tenant1.com'), (2, 100, 'user100@tenant2.com');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("tenant_id", typeof(int)),
				new DataColumn("user_id", typeof(int)),
				new DataColumn("email", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, 100, "new100@tenant1.com" },
				new object[] { 2, 100, "new100@tenant2.com" },
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_composite",
			KeyColumns = { "tenant_id", "user_id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "tenant_id"),
				new SingleStoreBulkCopyColumnMapping(1, "user_id"),
				new SingleStoreBulkCopyColumnMapping(2, "email"),
			},
		};

		var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);

		Assert.Equal(2, result.RowsStaged);
		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select email from bulk_update_composite where tenant_id = 1 and user_id = 100;", connection);
		Assert.Equal("new100@tenant1.com", await selectCommand.ExecuteScalarAsync());

		// The (1, 101) row shares tenant_id with an updated row but was not in the source: it must be untouched.
		using var untouchedCommand = new SingleStoreCommand("select email from bulk_update_composite where tenant_id = 1 and user_id = 101;", connection);
		Assert.Equal("user101@tenant1.com", await untouchedCommand.ExecuteScalarAsync());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdatesNothingWhenNoKeysMatch(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_nomatch;
create table bulk_update_nomatch(id int primary key, value varchar(100));
insert into bulk_update_nomatch values (1, 'original');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 999, "new" },
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_nomatch",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);

		Assert.Equal(1, result.RowsStaged);
		Assert.Equal(0, result.RowsMatched);
		Assert.Equal(0, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_nomatch where id = 1;", connection);
		Assert.Equal("original", await selectCommand.ExecuteScalarAsync());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ThrowsOnDuplicateSourceKeys(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_dup;
create table bulk_update_dup(id int primary key, value varchar(100));
insert into bulk_update_dup values (1, 'original');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "value1" },
				new object[] { 1, "value2" }, // duplicate key violates the staging table's primary key
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_dup",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<SingleStoreException>(async () => await WriteToServerAsync(bulkUpdate, dataTable, isAsync));
		Assert.Equal(SingleStoreErrorCode.DuplicateKeyEntry, exception.ErrorCode);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RollbackLeavesDataUnchanged(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_txn;
create table bulk_update_txn(id int primary key, value varchar(100));
insert into bulk_update_txn values (1, 'original');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "updated" },
			},
		};

		using (var transaction = await connection.BeginTransactionAsync())
		{
			var bulkUpdate = new SingleStoreBulkUpdate(connection, transaction)
			{
				DestinationTableName = "bulk_update_txn",
				KeyColumns = { "id" },
				ColumnMappings =
				{
					new SingleStoreBulkCopyColumnMapping(0, "id"),
					new SingleStoreBulkCopyColumnMapping(1, "value"),
				},
			};

			var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);
			Assert.Equal(1, result.RowsUpdated);

			await transaction.RollbackAsync();
		}

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_txn where id = 1;", connection);
		Assert.Equal("original", await selectCommand.ExecuteScalarAsync());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task EmptyInputReturnsZeroCounts(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_empty;
create table bulk_update_empty(id int primary key, value varchar(100));", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_empty",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);

		Assert.Equal(0, result.RowsStaged);
		Assert.Equal(0, result.RowsMatched);
		Assert.Equal(0, result.RowsUpdated);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CommitPersistsChanges(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_commit;
create table bulk_update_commit(id int primary key, value varchar(100));
insert into bulk_update_commit values (1, 'original');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "updated" },
			},
		};

		using (var transaction = await connection.BeginTransactionAsync())
		{
			var bulkUpdate = new SingleStoreBulkUpdate(connection, transaction)
			{
				DestinationTableName = "bulk_update_commit",
				KeyColumns = { "id" },
				ColumnMappings =
				{
					new SingleStoreBulkCopyColumnMapping(0, "id"),
					new SingleStoreBulkCopyColumnMapping(1, "value"),
				},
			};

			var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);
			Assert.Equal(1, result.RowsUpdated);

			await transaction.CommitAsync();
		}

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_commit where id = 1;", connection);
		Assert.Equal("updated", await selectCommand.ExecuteScalarAsync());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OpensAndClosesConnectionWhenInitiallyClosed(bool isAsync)
	{
		using (var setupConnection = new SingleStoreConnection(GetLocalConnectionString(database)))
		{
			await setupConnection.OpenAsync();
			using var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_autoopen;
create table bulk_update_autoopen(id int primary key, value varchar(100));
insert into bulk_update_autoopen values (1, 'original');", setupConnection);
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "updated" },
			},
		};

		// Deliberately leave the connection closed: the bulk update must open it and close it again afterward.
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_autoopen",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		Assert.Equal(ConnectionState.Closed, connection.State);
		var result = await WriteToServerAsync(bulkUpdate, dataTable, isAsync);
		Assert.Equal(1, result.RowsUpdated);
		Assert.Equal(ConnectionState.Closed, connection.State);

		await connection.OpenAsync();
		using var selectCommand = new SingleStoreCommand("select value from bulk_update_autoopen where id = 1;", connection);
		Assert.Equal("updated", await selectCommand.ExecuteScalarAsync());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UpdatesFromDataRowSequence(bool isAsync)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_datarows;
create table bulk_update_datarows(id int primary key, value varchar(100));
insert into bulk_update_datarows values (1, 'old1'), (2, 'old2'), (3, 'old3');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "new1" },
				new object[] { 2, "new2" },
				new object[] { 3, "new3" },
			},
		};

		// Select a subset of rows so the source is a DataRow sequence rather than a DataTable.
		var dataRows = dataTable.Select("id <> 2");

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_datarows",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = isAsync ? await bulkUpdate.WriteToServerAsync(dataRows) : bulkUpdate.WriteToServer(dataRows);

		Assert.Equal(2, result.RowsStaged);
		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_datarows order by id;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal("new1", reader.GetString(0));
		Assert.True(await reader.ReadAsync());
		Assert.Equal("old2", reader.GetString(0)); // id = 2 was excluded from the source
		Assert.True(await reader.ReadAsync());
		Assert.Equal("new3", reader.GetString(0));
	}

	private static async ValueTask<SingleStoreBulkUpdateResult> WriteToServerAsync(SingleStoreBulkUpdate bulkUpdate, DataTable dataTable, bool isAsync) =>
		isAsync ? await bulkUpdate.WriteToServerAsync(dataTable) : bulkUpdate.WriteToServer(dataTable);

	internal static string GetLocalConnectionString(DatabaseFixture database)
	{
		var csb = new SingleStoreConnectionStringBuilder(database.Connection.ConnectionString)
		{
			AllowLoadLocalInfile = true,
		};
		return csb.ConnectionString;
	}
}
