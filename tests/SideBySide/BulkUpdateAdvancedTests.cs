namespace SideBySide;

public class BulkUpdateAdvancedTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
	[Fact]
	public async Task FiresProgressNotifications()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand($@"drop table if exists bulk_update_progress;
create table bulk_update_progress(id int primary key, value int);
insert into bulk_update_progress values {SequentialRows(100)};", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(int)),
			},
		};
		for (var i = 1; i <= 100; i++)
			dataTable.Rows.Add(i, i * 2);

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_progress",
			NotifyAfter = 25,
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var eventCount = 0;
		bulkUpdate.SingleStoreRowsStaged += (sender, e) =>
		{
			eventCount++;
			Assert.True(e.RowsStaged > 0);
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(100, result.RowsStaged);
		Assert.True(eventCount > 0, "expected at least one progress event for NotifyAfter=25 over 100 rows");
	}

	[Fact]
	public async Task AbortStopsStaging()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand($@"drop table if exists bulk_update_abort;
create table bulk_update_abort(id int primary key, value int);
insert into bulk_update_abort values {SequentialRows(100)};", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(int)),
			},
		};
		for (var i = 1; i <= 100; i++)
			dataTable.Rows.Add(i, i * 2);

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_abort",
			NotifyAfter = 25,
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		// Abort on the first progress notification; staging must stop before all rows are sent.
		bulkUpdate.SingleStoreRowsStaged += (sender, e) => e.Abort = true;

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.True(result.RowsStaged < 100, $"expected staging to stop early after abort, but staged {result.RowsStaged} rows");
	}

	[Fact]
	public async Task DoesNotFireProgressNotificationsWhenNotifyAfterIsZero()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand($@"drop table if exists bulk_update_no_notify;
create table bulk_update_no_notify(id int primary key, value int);
insert into bulk_update_no_notify values {SequentialRows(100)};", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(int)),
			},
		};
		for (var i = 1; i <= 100; i++)
			dataTable.Rows.Add(i, i * 2);

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_no_notify",
			NotifyAfter = 0, // notifications disabled
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var eventCount = 0;
		bulkUpdate.SingleStoreRowsStaged += (sender, e) => eventCount++;

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(100, result.RowsStaged);
		Assert.Equal(0, eventCount);
	}

	[Fact]
	public async Task SkipsMatchCountWhenComputeRowsMatchedIsFalse()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_nocount;
create table bulk_update_nocount(id int primary key, value varchar(100));
insert into bulk_update_nocount values (1, 'original');", connection))
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
			Rows = { new object[] { 1, "updated" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_nocount",
			ComputeRowsMatched = false,
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsStaged);
		Assert.Equal(-1, result.RowsMatched); // -1 signals the COUNT was intentionally skipped
		Assert.Equal(1, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_nocount where id = 1;", connection);
		Assert.Equal("updated", await selectCommand.ExecuteScalarAsync());
	}

	[Fact]
	public async Task HandlesSpecialCharactersInIdentifiers()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists `my-special-table`;
create table `my-special-table`(`user-id` int primary key, `user name` varchar(100), `select` varchar(50));
insert into `my-special-table` values (1, 'Alice', 'value1');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("user-id", typeof(int)),
				new DataColumn("user name", typeof(string)),
				new DataColumn("select", typeof(string)),
			},
			Rows = { new object[] { 1, "Alice Updated", "value2" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "my-special-table",
			KeyColumns = { "user-id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "user-id"),
				new SingleStoreBulkCopyColumnMapping(1, "user name"),
				new SingleStoreBulkCopyColumnMapping(2, "select"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsStaged);
		Assert.Equal(1, result.RowsMatched);
		Assert.Equal(1, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select `user name`, `select` from `my-special-table` where `user-id` = 1;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal("Alice Updated", reader.GetString(0));
		Assert.Equal("value2", reader.GetString(1));
	}

	[Fact]
	public async Task RoundTripsLossyColumnTypes()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// These destination types are exactly the ones GetSchemaTable() reports inaccurately. The staging table
		// must mirror them verbatim from SHOW CREATE TABLE, so the values round-trip without conversion errors.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_lossy;
create table bulk_update_lossy(id int primary key, amount decimal(18,4), quantity int unsigned, status enum('active','inactive'));
insert into bulk_update_lossy values (1, 0.0000, 0, 'active');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("amount", typeof(decimal)),
				new DataColumn("quantity", typeof(long)),
				new DataColumn("status", typeof(string)),
			},
			Rows = { new object[] { 1, 1234.5678m, 4000000000L, "inactive" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_lossy",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "amount"),
				new SingleStoreBulkCopyColumnMapping(2, "quantity"),
				new SingleStoreBulkCopyColumnMapping(3, "status"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsUpdated);
		Assert.Empty(result.Warnings);

		using var selectCommand = new SingleStoreCommand("select amount, quantity, status from bulk_update_lossy where id = 1;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal(1234.5678m, reader.GetDecimal(0));
		Assert.Equal(4000000000L, Convert.ToInt64(reader.GetValue(1)));
		Assert.Equal("inactive", reader.GetString(2));
	}

	[Fact]
	public async Task UpdatesRowsFromDataReader()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_reader_dest;
drop table if exists bulk_update_reader_src;
create table bulk_update_reader_dest(id int primary key, value varchar(100));
insert into bulk_update_reader_dest values (1, 'old1'), (2, 'old2');
create table bulk_update_reader_src(id int primary key, value varchar(100));
insert into bulk_update_reader_src values (1, 'new1'), (2, 'new2');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		// Read source rows on a second connection so the reader is independent of the update connection.
		using var readerConnection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await readerConnection.OpenAsync();
		using var selectCommand = new SingleStoreCommand("select id, value from bulk_update_reader_src order by id;", readerConnection);
		using var reader = await selectCommand.ExecuteReaderAsync();

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_reader_dest",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(reader);

		Assert.Equal(2, result.RowsStaged);
		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var verifyCommand = new SingleStoreCommand("select value from bulk_update_reader_dest where id = 1;", connection);
		Assert.Equal("new1", await verifyCommand.ExecuteScalarAsync());
	}

	[Fact]
	public async Task UpdatesWhenShardKeyAlignsWithKeyColumns()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// The destination shard key (id) is a subset of the key columns, so the staging table is sharded the same
		// way and the join can run locally. The update must succeed.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_shard_aligned;
create table bulk_update_shard_aligned(id int, value varchar(100), primary key (id), shard key (id));
insert into bulk_update_shard_aligned values (1, 'old1'), (2, 'old2');", connection))
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
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_shard_aligned",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_shard_aligned where id = 1;", connection);
		Assert.Equal("new1", await selectCommand.ExecuteScalarAsync());
	}

	[Fact]
	public async Task UpdatesWhenCompositeShardKeyOrderDiffersFromKeyColumns()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// The destination shard key is (tenant_id, user_id) but the key columns are declared in the opposite order
		// (user_id, then tenant_id). ComputeStagingShardKey returns the destination's shard-key columns verbatim, so
		// the staging SHARD KEY keeps the destination order (tenant_id, user_id) while the staging PRIMARY KEY uses
		// KeyColumns order (user_id, tenant_id). SingleStore only requires the primary key to *contain* every shard
		// key column (a set rule, not an ordering/prefix rule), so CREATE TEMPORARY TABLE is valid and the update
		// succeeds. Preserving the destination shard-key order also keeps the join co-located.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_shard_composite;
create table bulk_update_shard_composite(tenant_id int, user_id int, value varchar(100), primary key (tenant_id, user_id), shard key (tenant_id, user_id));
insert into bulk_update_shard_composite values (1, 100, 'old1'), (2, 200, 'old2');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("user_id", typeof(int)),
				new DataColumn("tenant_id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 100, 1, "new1" },
				new object[] { 200, 2, "new2" },
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_shard_composite",
			KeyColumns = { "user_id", "tenant_id" }, // deliberately the opposite order from the destination shard key
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "user_id"),
				new SingleStoreBulkCopyColumnMapping(1, "tenant_id"),
				new SingleStoreBulkCopyColumnMapping(2, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_shard_composite where tenant_id = 1 and user_id = 100;", connection);
		Assert.Equal("new1", await selectCommand.ExecuteScalarAsync());
	}

	[Fact]
	public async Task UpdatesWhenShardKeyDoesNotAlignWithKeyColumns()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// The destination shard key (region) is not among the key columns, so the staging table cannot be aligned
		// and falls back to primary-key distribution (logging a mismatch warning). region is left unmapped so it is
		// not treated as a shard-key update. The update must still succeed.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_shard_mismatch;
create table bulk_update_shard_mismatch(id int, region int, value varchar(100), primary key (id, region), shard key (region));
insert into bulk_update_shard_mismatch values (1, 10, 'old1'), (2, 20, 'old2');", connection))
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
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_shard_mismatch",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(2, result.RowsUpdated);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_shard_mismatch where id = 1;", connection);
		Assert.Equal("new1", await selectCommand.ExecuteScalarAsync());
	}

	[Fact]
	public async Task UpdatesLargeDataset()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand($@"drop table if exists bulk_update_large;
create table bulk_update_large(id int primary key, value int);
insert into bulk_update_large values {SequentialRows(5000)};", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(int)),
			},
		};
		for (var i = 1; i <= 5000; i++)
			dataTable.Rows.Add(i, i * 2);

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_large",
			BulkCopyTimeout = 60,
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(5000, result.RowsStaged);
		Assert.Equal(5000, result.RowsMatched);

		using var selectCommand = new SingleStoreCommand("select value from bulk_update_large where id = 2500;", connection);
		Assert.Equal(5000, Convert.ToInt32(await selectCommand.ExecuteScalarAsync()));
	}

	// Builds a "(1,0),(2,0),...,(count,0)" VALUES list to seed a table with sequential ids.
	private static string SequentialRows(int count)
	{
		var builder = new StringBuilder();
		for (var i = 1; i <= count; i++)
		{
			if (builder.Length != 0)
				builder.Append(',');
			builder.Append('(').Append(i).Append(",0)");
		}

		return builder.ToString();
	}
}
