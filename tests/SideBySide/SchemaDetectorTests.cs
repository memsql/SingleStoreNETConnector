using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SideBySide;

public class SchemaDetectorTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IsReferenceTable_RegularTable_ReturnsFalse(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_regular";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var isReference = await detector.IsReferenceTableAsync(tableName, ioBehavior);

			Assert.False(isReference);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IsReferenceTable_ReferenceTable_ReturnsTrue(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_reference";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE REFERENCE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var isReference = await detector.IsReferenceTableAsync(tableName, ioBehavior);

			Assert.True(isReference);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetShardKeyColumns_TableWithShardKey_ReturnsColumnsInOrder(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_sharded";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					tenant_id INT,
					user_id INT,
					name VARCHAR(100),
					SHARD KEY (tenant_id, user_id)
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var shardKeys = await detector.GetShardKeyColumnsAsync(tableName, ioBehavior);

			Assert.Equal(new[] { "tenant_id", "user_id" }, shardKeys);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetShardKeyColumns_TableWithoutShardKey_ReturnsEmpty(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_no_shard";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE REFERENCE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var shardKeys = await detector.GetShardKeyColumnsAsync(tableName, ioBehavior);

			Assert.Empty(shardKeys);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetShardKeyColumns_SingleColumnShardKey_ReturnsColumn()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_single_shard";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					id INT,
					name VARCHAR(100),
					SHARD KEY (id)
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var shardKeys = await detector.GetShardKeyColumnsAsync(tableName, IOBehavior.Asynchronous);

			Assert.Equal(new[] { "id" }, shardKeys);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetTableSchema_ExistingTable_ReturnsSchema(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_schema";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY,
					name VARCHAR(100)
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var schema = await detector.GetTableSchemaAsync(tableName, ioBehavior);

			Assert.NotNull(schema);
			Assert.Contains(schema.Rows.Cast<DataRow>(), row => row["ColumnName"].ToString() == "id");
			Assert.Contains(schema.Rows.Cast<DataRow>(), row => row["ColumnName"].ToString() == "name");
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetColumnTypeDefinitions_SimpleColumns_ReturnsTypesForAllColumns(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_coldefs_simple";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY,
					name VARCHAR(100)
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, ioBehavior);

			Assert.Equal(2, definitions.Count);
			Assert.Contains("int", definitions["id"], StringComparison.OrdinalIgnoreCase);
			Assert.Contains("varchar(100)", definitions["name"], StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetColumnTypeDefinitions_IsCaseInsensitiveOnColumnName()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_coldefs_case";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					MyId INT PRIMARY KEY,
					MyValue VARCHAR(50)
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

			// The map must be case-insensitive so callers can look up by the caller-supplied column name.
			Assert.True(definitions.ContainsKey("myid"));
			Assert.True(definitions.ContainsKey("MYVALUE"));
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetColumnTypeDefinitions_LossyTypes_PreservesExactServerDefinition()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_coldefs_lossy";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			// These types are exactly the ones GetSchemaTable() reports inaccurately (VARBINARY -> BLOB,
			// BIT -> BIGINT, UNSIGNED dropped, DECIMAL precision/scale, ENUM members). The definition must
			// come verbatim from SHOW CREATE TABLE so the staging column matches the destination.
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY,
					payload VARBINARY(16),
					flag BIT(1),
					amount DECIMAL(18,4),
					quantity INT UNSIGNED,
					status ENUM('active','inactive')
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

			Assert.Contains("varbinary(16)", definitions["payload"], StringComparison.OrdinalIgnoreCase);
			Assert.Contains("bit(1)", definitions["flag"], StringComparison.OrdinalIgnoreCase);
			Assert.Contains("decimal(18,4)", definitions["amount"], StringComparison.OrdinalIgnoreCase);
			Assert.Contains("unsigned", definitions["quantity"], StringComparison.OrdinalIgnoreCase);

			// The ENUM member list (including the comma between members) must survive top-level comma splitting.
			Assert.Contains("'active'", definitions["status"], StringComparison.OrdinalIgnoreCase);
			Assert.Contains("'inactive'", definitions["status"], StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetColumnTypeDefinitions_ExcludesColumnOptions()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "test_coldefs_options";
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		try
		{
			await using (var cmd = new SingleStoreCommand($@"
				DROP TABLE IF EXISTS {quotedTableName};
				CREATE TABLE {quotedTableName}
				(
					id INT PRIMARY KEY,
					note VARCHAR(50) NOT NULL DEFAULT 'none'
				);", connection))
			{
				await cmd.ExecuteNonQueryAsync();
			}

			var detector = new SchemaDetector(connection);
			var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

			// Only the type portion is returned; column options must be stripped so the caller controls nullability.
			var noteDefinition = definitions["note"];

			Assert.Contains("varchar(50)", noteDefinition, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("NOT NULL", noteDefinition, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("DEFAULT", noteDefinition, StringComparison.OrdinalIgnoreCase);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task IsReferenceTable_NonexistentTable_Throws()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var detector = new SchemaDetector(connection);

		await Assert.ThrowsAnyAsync<SingleStoreException>(
			async () => await detector.IsReferenceTableAsync("table_that_does_not_exist", IOBehavior.Asynchronous));
	}

	[Fact]
	public async Task GetTableSchema_ConnectionNotOpen_Throws()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);

		// Deliberately do not open the connection: schema detection requires an open session.
		var detector = new SchemaDetector(connection);

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () => await detector.GetTableSchemaAsync("any_table", IOBehavior.Asynchronous));
	}

	private static IOBehavior ToIOBehavior(bool isAsync) =>
		isAsync ? IOBehavior.Asynchronous : IOBehavior.Synchronous;

	private static async Task DropTableIfExistsAsync(SingleStoreConnection connection, string tableName)
	{
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		await using var cmd = new SingleStoreCommand($"DROP TABLE IF EXISTS {quotedTableName}", connection);
		await cmd.ExecuteNonQueryAsync();
	}
}
