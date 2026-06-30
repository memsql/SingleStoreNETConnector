using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SideBySide;

public class SchemaDetectorTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IsReferenceTableReturnsFalseForRegularTable(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_regular";
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY");

		var detector = new SchemaDetector(connection);
		var isReference = await detector.IsReferenceTableAsync(tableName, ioBehavior);

		Assert.False(isReference);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task IsReferenceTableReturnsTrueForReferenceTable(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_reference";
		await CreateReferenceTableAsync(connection, tableName, "id INT PRIMARY KEY");

		var detector = new SchemaDetector(connection);
		var isReference = await detector.IsReferenceTableAsync(tableName, ioBehavior);

		Assert.True(isReference);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetShardKeyColumnsReturnsColumnsInOrder(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_sharded";
		await CreateTableAsync(connection, tableName, "tenant_id INT, user_id INT, name VARCHAR(100), SHARD KEY (tenant_id, user_id)");

		var detector = new SchemaDetector(connection);
		var shardKeys = await detector.GetShardKeyColumnsAsync(tableName, ioBehavior);

		Assert.Equal(new[] { "tenant_id", "user_id" }, shardKeys);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetShardKeyColumnsReturnsEmptyWhenNoShardKey(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_no_shard";
		await CreateReferenceTableAsync(connection, tableName, "id INT PRIMARY KEY");

		var detector = new SchemaDetector(connection);
		var shardKeys = await detector.GetShardKeyColumnsAsync(tableName, ioBehavior);

		Assert.Empty(shardKeys);
	}

	[Fact]
	public async Task GetShardKeyColumnsReturnsSingleColumn()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_single_shard";
		await CreateTableAsync(connection, tableName, "id INT, name VARCHAR(100), SHARD KEY (id)");

		var detector = new SchemaDetector(connection);
		var shardKeys = await detector.GetShardKeyColumnsAsync(tableName, IOBehavior.Asynchronous);

		Assert.Equal(new[] { "id" }, shardKeys);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetTableSchemaReturnsSchema(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_schema";
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, name VARCHAR(100)");

		var detector = new SchemaDetector(connection);
		var schema = await detector.GetTableSchemaAsync(tableName, ioBehavior);

		Assert.NotNull(schema);
		Assert.Contains(schema.Rows.Cast<DataRow>(), row => row["ColumnName"].ToString() == "id");
		Assert.Contains(schema.Rows.Cast<DataRow>(), row => row["ColumnName"].ToString() == "name");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetColumnTypeDefinitionsReturnsAllColumns(bool isAsync)
	{
		var ioBehavior = ToIOBehavior(isAsync);

		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_simple";
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, name VARCHAR(100)");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, ioBehavior);

		Assert.Equal(2, definitions.Count);
		Assert.Contains("int", definitions["id"], StringComparison.OrdinalIgnoreCase);
		Assert.Contains("varchar(100)", definitions["name"], StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetColumnTypeDefinitionsIsCaseInsensitive()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_case";
		await CreateTableAsync(connection, tableName, "MyId INT PRIMARY KEY, MyValue VARCHAR(50)");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		// The map must be case-insensitive so callers can look up by the caller-supplied column name.
		Assert.True(definitions.ContainsKey("myid"));
		Assert.True(definitions.ContainsKey("MYVALUE"));
	}

	[Fact]
	public async Task GetColumnTypeDefinitionsPreservesLossyTypes()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_lossy";

		// These types are exactly the ones GetSchemaTable() reports inaccurately (VARBINARY -> BLOB,
		// BIT -> BIGINT, UNSIGNED dropped, DECIMAL precision/scale, ENUM members). The definition must
		// come verbatim from SHOW CREATE TABLE so the staging column matches the destination.
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, payload VARBINARY(16), flag BIT(1), amount DECIMAL(18,4), quantity INT UNSIGNED, status ENUM('active','inactive')");

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

	[Fact]
	public async Task GetColumnTypeDefinitionsPreservesCollation()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_collation";

		// Collation is not exposed by GetSchemaTable() but is part of the verbatim definition, and it determines
		// how the key-column equality in the UPDATE ... JOIN compares values, so it must be preserved.
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, name VARCHAR(100) COLLATE utf8_bin");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		Assert.Contains("varchar(100)", definitions["name"], StringComparison.OrdinalIgnoreCase);
		Assert.Contains("collate", definitions["name"], StringComparison.OrdinalIgnoreCase);
		Assert.Contains("utf8_bin", definitions["name"], StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetColumnTypeDefinitionsUnescapesBacktickInColumnName()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_backtick";

		// A backtick in a column name is doubled in SHOW CREATE TABLE output; the parser must unescape it back
		// to a single backtick so the dictionary key matches the real column name.
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, `odd``name` INT");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		Assert.True(definitions.ContainsKey("odd`name"));
		Assert.Contains("int", definitions["odd`name"], StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetColumnTypeDefinitionsExcludesColumnOptions()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_options";
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, note VARCHAR(50) NOT NULL DEFAULT 'none'");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		// Only the type portion is returned; column options must be stripped so the caller controls nullability.
		var noteDefinition = definitions["note"];

		Assert.Contains("varchar(50)", noteDefinition, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("NOT NULL", noteDefinition, StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("DEFAULT", noteDefinition, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task GetColumnTypeDefinitionsHandlesCommentWithSpecialCharacters()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_comment";

		// A column comment can contain the characters the parser keys on: top-level commas (which separate
		// columns), parentheses (which the type-args/paren tracking uses), and quotes/backticks. None of these
		// must confuse SplitTopLevel, the column-name parsing, or the type extraction. The comment is a column
		// option, so it must also be excluded from the returned type definition.
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, value VARCHAR(50) COMMENT 'a, b (c) ''d'' `e`', other INT");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		// All three columns must be discovered (the comment must not have swallowed "other" or split it early).
		Assert.Equal(3, definitions.Count);
		Assert.Contains("varchar(50)", definitions["value"], StringComparison.OrdinalIgnoreCase);
		Assert.DoesNotContain("comment", definitions["value"], StringComparison.OrdinalIgnoreCase);
		Assert.Contains("int", definitions["other"], StringComparison.OrdinalIgnoreCase);
	}

	[SkippableFact(ServerFeatures.Json)]
	public async Task GetColumnTypeDefinitionsHandlesJsonColumn()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_json";
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, data JSON");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		Assert.Contains("json", definitions["data"], StringComparison.OrdinalIgnoreCase);
	}

	[SkippableFact(ServerFeatures.ExtendedDataTypes)]
	public async Task GetColumnTypeDefinitionsHandlesVectorColumn()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var tableName = "schema_detector_coldefs_vector";

		// VECTOR has parenthesised arguments (dimension count, and possibly an element type) that must be kept as
		// part of the type token rather than split on or truncated.
		await CreateTableAsync(connection, tableName, "id INT PRIMARY KEY, embedding VECTOR(4)");

		var detector = new SchemaDetector(connection);
		var definitions = await detector.GetColumnTypeDefinitionsAsync(tableName, IOBehavior.Asynchronous);

		Assert.Contains("vector", definitions["embedding"], StringComparison.OrdinalIgnoreCase);
		Assert.Contains("4", definitions["embedding"], StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task IsReferenceTableThrowsForNonexistentTable()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);
		await connection.OpenAsync();

		var detector = new SchemaDetector(connection);

		await Assert.ThrowsAnyAsync<SingleStoreException>(
			async () => await detector.IsReferenceTableAsync("table_that_does_not_exist", IOBehavior.Asynchronous));
	}

	[Fact]
	public async Task GetTableSchemaThrowsWhenConnectionNotOpen()
	{
		await using var connection = new SingleStoreConnection(database.Connection.ConnectionString);

		// Deliberately do not open the connection: schema detection requires an open session.
		var detector = new SchemaDetector(connection);

		await Assert.ThrowsAsync<InvalidOperationException>(
			async () => await detector.GetTableSchemaAsync("any_table", IOBehavior.Asynchronous));
	}

	private static IOBehavior ToIOBehavior(bool isAsync) =>
		isAsync ? IOBehavior.Asynchronous : IOBehavior.Synchronous;

	private static async Task CreateTableAsync(SingleStoreConnection connection, string tableName, string columnDefinitions)
	{
		var quoted = IdentifierHelper.QuoteIdentifier(tableName);
		await using var cmd = new SingleStoreCommand($"DROP TABLE IF EXISTS {quoted}; CREATE TABLE {quoted} ({columnDefinitions});", connection);
		await cmd.ExecuteNonQueryAsync();
	}

	private static async Task CreateReferenceTableAsync(SingleStoreConnection connection, string tableName, string columnDefinitions)
	{
		var quoted = IdentifierHelper.QuoteIdentifier(tableName);
		await using var cmd = new SingleStoreCommand($"DROP TABLE IF EXISTS {quoted}; CREATE REFERENCE TABLE {quoted} ({columnDefinitions});", connection);
		await cmd.ExecuteNonQueryAsync();
	}
}
