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
	public async Task AbortLeavesDestinationUnchanged()
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

		// Abort on the first progress notification. Aborting must cancel the whole operation, not perform a
		// partial update: no rows are updated and the destination table is left unchanged.
		bulkUpdate.SingleStoreRowsStaged += (sender, e) => e.Abort = true;

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(0, result.RowsAffected);

		// Every row must still hold its seeded value of 0 (the update would have set value = id * 2).
		using var selectCommand = new SingleStoreCommand("select count(*) from bulk_update_abort where value <> 0;", connection);
		Assert.Equal(0L, Convert.ToInt64(await selectCommand.ExecuteScalarAsync()));
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

	[Theory]
	[InlineData(false, 2)] // CLIENT_FOUND_ROWS: RowsAffected counts matched rows, including the unchanged one
	[InlineData(true, 1)] // RowsAffected counts only the row whose value actually changed
	public async Task RowsAffectedDependsOnUseAffectedRows(bool useAffectedRows, int expectedRowsAffected)
	{
		var csb = new SingleStoreConnectionStringBuilder(database.Connection.ConnectionString)
		{
			AllowLoadLocalInfile = true,
			UseAffectedRows = useAffectedRows,
		};

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_affected;
create table bulk_update_affected(id int primary key, value varchar(100));
insert into bulk_update_affected values (1, 'unchanged'), (2, 'before');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		// Row 1 is updated to the value it already holds (no change); row 2 changes. Both match the join, so the
		// difference between the two settings is whether RowsAffected counts the unchanged matched row.
		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "unchanged" },
				new object[] { 2, "after" },
			},
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_affected",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		// RowsMatched counts the matched source rows (both) regardless of the connection setting.
		Assert.Equal(2, result.RowsMatched);
		Assert.Equal(expectedRowsAffected, result.RowsAffected);
	}

	[Fact]
	public async Task NonUniqueDestinationKeyMatchesMultipleRows()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// The destination key column (grp) is not unique: one staged row matches two destination rows. RowsMatched
		// counts the single matched staged row (never exceeding RowsStaged), while RowsAffected reflects the two
		// destination rows the UPDATE touched.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_nonunique;
create rowstore table bulk_update_nonunique(id int primary key, grp int, value varchar(100));
insert into bulk_update_nonunique values (1, 100, 'old'), (2, 100, 'old'), (3, 200, 'old');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("grp", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows = { new object[] { 100, "updated" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_nonunique",
			KeyColumns = { "grp" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "grp"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsStaged);
		Assert.Equal(1, result.RowsMatched); // the single staged row matched (not the 2 destination rows it joined)
		Assert.Equal(2, result.RowsAffected); // both grp=100 destination rows were updated

		using var selectCommand = new SingleStoreCommand("select count(*) from bulk_update_nonunique where grp = 100 and value = 'updated';", connection);
		Assert.Equal(2L, Convert.ToInt64(await selectCommand.ExecuteScalarAsync()));
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
		Assert.Null(result.RowsMatched); // null signals the COUNT was intentionally skipped
		Assert.Equal(1, result.RowsAffected);

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
		Assert.Equal(1, result.RowsAffected);

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

		Assert.Equal(1, result.RowsAffected);
		Assert.Empty(result.Warnings);

		using var selectCommand = new SingleStoreCommand("select amount, quantity, status from bulk_update_lossy where id = 1;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal(1234.5678m, reader.GetDecimal(0));
		Assert.Equal(4000000000L, Convert.ToInt64(reader.GetValue(1)));
		Assert.Equal("inactive", reader.GetString(2));
	}

	[Fact]
	public async Task RoundTripsBinaryColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// VARBINARY is staged directly (not via an UNHEX expression mapping the caller provides); SingleStoreBulkCopy
		// applies the hex conversion itself based on the staging column's type, so the bytes must round-trip exactly.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_binary;
create table bulk_update_binary(id int primary key, payload varbinary(16));
insert into bulk_update_binary values (1, NULL);", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var payload = new byte[] { 0x00, 0x01, 0xFE, 0xFF, 0x10, 0x20 };
		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("payload", typeof(byte[])),
			},
			Rows = { new object[] { 1, payload } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_binary",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "payload"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsAffected);
		Assert.Empty(result.Warnings);

		using var selectCommand = new SingleStoreCommand("select payload from bulk_update_binary where id = 1;", connection);
		Assert.Equal(payload, (byte[]) (await selectCommand.ExecuteScalarAsync())!);
	}

	[Fact]
	public async Task RoundTripsBitColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// BIT is staged directly; SingleStoreBulkCopy converts the staged value with CAST(... AS UNSIGNED) based on
		// the staging column's type, so the bit value must round-trip.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_bit;
create table bulk_update_bit(id int primary key, flags bit(8));
insert into bulk_update_bit values (1, b'00000000');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("flags", typeof(ulong)),
			},
			Rows = { new object[] { 1, 0b1010_0101UL } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_bit",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "flags"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsAffected);
		Assert.Empty(result.Warnings);

		using var selectCommand = new SingleStoreCommand("select flags from bulk_update_bit where id = 1;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal(0b1010_0101UL, reader.GetUInt64(0));
	}

	[SkippableFact(ServerFeatures.ExtendedDataTypes)]
	public async Task RoundTripsVectorColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// VECTOR carries both a dimension count and an element type. The staging column mirrors VECTOR(3, F32)
		// verbatim, and SingleStoreBulkCopy reconstructs the value with UNHEX(...):>VECTOR(3, F32) from that
		// staging column's metadata, so the vector must round-trip exactly.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_vector;
create table bulk_update_vector(id int primary key, embedding vector(3, F32));
insert into bulk_update_vector values (1, '[0,0,0]');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var embedding = new[] { 1.5f, -2.5f, 3.25f };
		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("embedding", typeof(float[])),
			},
			Rows = { new object[] { 1, embedding } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_vector",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "embedding"),
			},
		};

		var result = await bulkUpdate.WriteToServerAsync(dataTable);

		Assert.Equal(1, result.RowsAffected);
		Assert.Empty(result.Warnings);

		using var selectCommand = new SingleStoreCommand("select embedding from bulk_update_vector where id = 1;", connection);
		using var reader = await selectCommand.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal(embedding, reader.GetFieldValue<ReadOnlyMemory<float>>(0).ToArray());
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

		// Read source rows on a second connection: an IDataReader source must not be open on the bulk update's own
		// connection, which needs to run schema queries, create the staging table, and load data.
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
		Assert.Equal(2, result.RowsAffected);

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
		Assert.Equal(2, result.RowsAffected);

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
		Assert.Equal(2, result.RowsAffected);

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
		Assert.Equal(2, result.RowsAffected);

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
			BulkUpdateTimeout = 60,
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

	[Fact]
	public async Task CancellationRunsCleanupAndLeavesDestinationUnchanged()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand($@"drop table if exists bulk_update_cancel;
create table bulk_update_cancel(id int primary key, value int);
insert into bulk_update_cancel values {SequentialRows(100)};", connection))
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

		using var cts = new CancellationTokenSource();

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_cancel",
			NotifyAfter = 10,
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		// Cancel the token from the progress handler, i.e. after the staging table has been created and data is
		// being staged. The cancellation is then observed before the UPDATE runs, so this exercises the real
		// cleanup path (the staging table gets dropped) rather than cancelling before anything was created.
		bulkUpdate.SingleStoreRowsStaged += (_, _) => cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await bulkUpdate.WriteToServerAsync(dataTable, cts.Token));

		// Cleanup ran: the connection the caller supplied is still open and usable, and no staging table leaked —
		// a fresh bulk update on the same connection succeeds. (If the temporary table had not been dropped, or the
		// connection had been left in a bad state, this second operation would fail.)
		Assert.Equal(ConnectionState.Open, connection.State);

		// No partial update happened: the destination still holds its original values (value = 0 for every row).
		using (var verifyCommand = new SingleStoreCommand("select count(*) from bulk_update_cancel where value <> 0;", connection))
			Assert.Equal(0L, Convert.ToInt64(await verifyCommand.ExecuteScalarAsync()));

		var secondUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_cancel",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};
		var result = await secondUpdate.WriteToServerAsync(dataTable);
		Assert.Equal(100, result.RowsStaged);
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
