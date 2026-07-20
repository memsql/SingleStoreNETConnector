namespace SideBySide;

public class BulkUpdateValidationTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
	[Fact]
	public async Task ThrowsWhenDestinationTableNameNotSet()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value")));
		Assert.Contains("DestinationTableName", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenKeyColumnsEmpty()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			ColumnMappings = { new SingleStoreBulkCopyColumnMapping(0, "id") },
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id")));
		Assert.Contains("KeyColumns", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenColumnMappingsEmpty()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id" },
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id")));
		Assert.Contains("ColumnMappings", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenKeyColumnNotMapped()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id", "tenant_id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value")));
		Assert.Contains("tenant_id", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenNoUpdateColumns()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id" },
			ColumnMappings = { new SingleStoreBulkCopyColumnMapping(0, "id") },
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id")));
		Assert.Contains("non-key column", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ThrowsWhenDuplicateKeyColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id", "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value")));
		Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("id", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenKeyColumnNameIsEmpty()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "  " },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value")));
		Assert.Contains("KeyColumns", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenDestinationColumnIsEmpty()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, ""),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value")));
		Assert.Contains("DestinationColumn", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenMappedColumnDoesNotExistInTargetTable()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_missing_column;
create table bulk_update_missing_column(id int primary key, value varchar(100));", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("missing", typeof(string)),
			},
			Rows = { new object[] { 1, "test" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_missing_column",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "missing"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(dataTable));
		Assert.Contains("missing", exception.Message);
		Assert.Contains("does not exist", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ThrowsOnExpressionMapping()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value", "UNHEX(@value)"),
			},
		};

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value")));
		Assert.Contains("Expression", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ThrowsOnDuplicateDestinationColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
				new SingleStoreBulkCopyColumnMapping(2, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(NewTable("id", "value", "value2")));
		Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ThrowsForReferenceTable()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_reference;
create reference table bulk_update_reference(id int primary key, value varchar(100));", connection))
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
			Rows = { new object[] { 1, "test" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_reference",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await bulkUpdate.WriteToServerAsync(dataTable));
		Assert.Contains("reference table", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task ThrowsWhenUpdatingShardKeyColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// Shard on tenant_id, then attempt to update tenant_id (a shard key) as a non-key column. SingleStore does
		// not allow updating shard key columns, so this must be rejected. tenant_id is part of the primary key
		// because SingleStore requires the primary key to contain all shard key columns.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_shardkey;
create table bulk_update_shardkey(id int, tenant_id int, value varchar(100), primary key (id, tenant_id), shard key (tenant_id));", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("tenant_id", typeof(int)),
			},
			Rows = { new object[] { 1, 2 } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_shardkey",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "tenant_id"),
			},
		};

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await bulkUpdate.WriteToServerAsync(dataTable));
		Assert.Contains("shard key", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("tenant_id", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenUpdatingGeneratedColumn()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// total is a generated (computed) column; its value is derived from an expression and cannot be updated.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_generated;
create table bulk_update_generated(id int primary key, price int, total as (price * 2) persisted int);", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("total", typeof(int)),
			},
			Rows = { new object[] { 1, 100 } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_generated",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "total"),
			},
		};

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await bulkUpdate.WriteToServerAsync(dataTable));
		Assert.Contains("generated", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("total", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenGeneratedColumnUsedAsKey()
	{
		using var connection = new SingleStoreConnection(BulkUpdateTests.GetLocalConnectionString(database));
		await connection.OpenAsync();

		// gen_key is a generated column used as a key column. It is still rejected: a generated column has no plain
		// column type that the staging table can reproduce, so it cannot be staged even as a key.
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_update_generated_key;
create table bulk_update_generated_key(id int, gen_key as (id * 10) persisted int, value varchar(100), primary key (id, gen_key));", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("gen_key", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows = { new object[] { 10, "updated" } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "bulk_update_generated_key",
			KeyColumns = { "gen_key" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "gen_key"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await bulkUpdate.WriteToServerAsync(dataTable));
		Assert.Contains("generated", exception.Message, StringComparison.OrdinalIgnoreCase);
		Assert.Contains("gen_key", exception.Message);
	}

	[Fact]
	public async Task ThrowsWhenAllowLoadLocalInfileNotSet()
	{
		// Deliberately build a connection string with AllowLoadLocalInfile=false; staging relies on it, so the
		// operation must fail early with a clear message before opening the connection or running any command.
		var csb = new SingleStoreConnectionStringBuilder(database.Connection.ConnectionString)
		{
			AllowLoadLocalInfile = false,
		};
		using var connection = new SingleStoreConnection(csb.ConnectionString);

		var dataTable = new DataTable
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(int)),
			},
			Rows = { { 1, 10 } },
		};

		var bulkUpdate = new SingleStoreBulkUpdate(connection)
		{
			DestinationTableName = "any_table",
			KeyColumns = { "id" },
			ColumnMappings =
			{
				new SingleStoreBulkCopyColumnMapping(0, "id"),
				new SingleStoreBulkCopyColumnMapping(1, "value"),
			},
		};

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await bulkUpdate.WriteToServerAsync(dataTable));
		Assert.Contains("AllowLoadLocalInfile", exception.Message);

		// The connection was never opened: the failure happened before any I/O.
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	private static DataTable NewTable(params string[] columnNames)
	{
		var dataTable = new DataTable();
		foreach (var columnName in columnNames)
			dataTable.Columns.Add(columnName, typeof(string));
		return dataTable;
	}
}
