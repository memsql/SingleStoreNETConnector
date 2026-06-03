using SingleStoreConnector.Utilities;

namespace SideBySide;

public class SchemaDetectorTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
	[Fact]
	public async Task IsReferenceTable_RegularTable_ReturnsFalse()
	{
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
			var isReference = await detector.IsReferenceTableAsync(tableName);

			Assert.False(isReference);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task IsReferenceTable_ReferenceTable_ReturnsTrue()
	{
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
			var isReference = await detector.IsReferenceTableAsync(tableName);

			Assert.True(isReference);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetShardKeyColumns_TableWithShardKey_ReturnsColumnsInOrder()
	{
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
			var shardKeys = await detector.GetShardKeyColumnsAsync(tableName);

			Assert.Equal(new[] { "tenant_id", "user_id" }, shardKeys);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetShardKeyColumns_TableWithoutShardKey_ReturnsEmpty()
	{
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
			var shardKeys = await detector.GetShardKeyColumnsAsync(tableName);

			Assert.Empty(shardKeys);
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	[Fact]
	public async Task GetTableSchema_ExistingTable_ReturnsSchema()
	{
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
			var schema = await detector.GetTableSchemaAsync(tableName);

			Assert.NotNull(schema);
			Assert.Contains(schema.Rows.Cast<DataRow>(), row => row["ColumnName"].ToString() == "id");
			Assert.Contains(schema.Rows.Cast<DataRow>(), row => row["ColumnName"].ToString() == "name");
		}
		finally
		{
			await DropTableIfExistsAsync(connection, tableName);
		}
	}

	private static async Task DropTableIfExistsAsync(SingleStoreConnection connection, string tableName)
	{
		var quotedTableName = IdentifierHelper.QuoteIdentifier(tableName);

		await using var cmd = new SingleStoreCommand($"DROP TABLE IF EXISTS {quotedTableName}", connection);
		await cmd.ExecuteNonQueryAsync();
	}
}
