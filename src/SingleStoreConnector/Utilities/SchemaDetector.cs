using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SingleStoreConnector.Utilities;

internal sealed class SchemaDetector(SingleStoreConnection connection)
{
	private static readonly Regex referenceTableRegex =
		new(@"CREATE\s+(?:(?:ROWSTORE|COLUMNSTORE)\s+)?REFERENCE\s+TABLE",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex shardKeyRegex =
		new(@"SHARD\s+KEY(?:\s+(?:`(?:``|[^`])*`|[^\s(]+))?\s*\((?<columns>[^)]*)\)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private readonly SingleStoreConnection m_connection =
		connection ?? throw new ArgumentNullException(nameof(connection));

	/// <summary>
	/// Detects if the specified table is a reference table.
	/// </summary>
	public async Task<bool> IsReferenceTableAsync(string tableName, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		var createTableSql = await GetCreateTableStatementAsync(tableName, cancellationToken)
			.ConfigureAwait(false);

		return referenceTableRegex.IsMatch(createTableSql);
	}

	/// <summary>
	/// Gets the shard key columns for the specified table.
	/// </summary>
	/// <returns>List of shard key column names, or empty list if no shard key.</returns>
	public async Task<List<string>> GetShardKeyColumnsAsync(string tableName, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		var shardKeys = new List<(int Sequence, string ColumnName)>();

		// Method 1: Check SHOW INDEXES for __SHARDKEY.
		// This is preferred because Seq_in_index preserves composite shard key column order.
		using (var cmd = m_connection.CreateCommand())
		{
			cmd.CommandText = $"SHOW INDEXES FROM {IdentifierHelper.QuoteQualifiedIdentifier(tableName)}";

			await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
				.ConfigureAwait(false);

			var keyNameOrdinal = reader.GetOrdinal("Key_name");
			var columnNameOrdinal = reader.GetOrdinal("Column_name");
			var sequenceOrdinal = reader.GetOrdinal("Seq_in_index");

			while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				if (reader.IsDBNull(keyNameOrdinal))
					continue;

				var keyName = reader.GetString(keyNameOrdinal);
				if (!string.Equals(keyName, "__SHARDKEY", StringComparison.Ordinal))
					continue;

				if (reader.IsDBNull(columnNameOrdinal))
					continue;

				var sequence = reader.IsDBNull(sequenceOrdinal)
					? shardKeys.Count + 1
					: Convert.ToInt32(reader.GetValue(sequenceOrdinal), CultureInfo.InvariantCulture);

				var columnName = reader.GetString(columnNameOrdinal);
				shardKeys.Add((sequence, columnName));
			}
		}

		if (shardKeys.Count != 0)
		{
			return shardKeys
				.OrderBy(x => x.Sequence)
				.Select(x => x.ColumnName)
				.ToList();
		}

		// Method 2: Parse SHOW CREATE TABLE for SHARD KEY.
		// This is a fallback for cases where SHOW INDEXES doesn't expose __SHARDKEY.
		var createTableSql = await GetCreateTableStatementAsync(tableName, cancellationToken)
			.ConfigureAwait(false);

		var match = shardKeyRegex.Match(createTableSql);
		if (!match.Success)
			return new List<string>();

		var shardKeyList = match.Groups["columns"].Value;
		if (string.IsNullOrWhiteSpace(shardKeyList))
			return new List<string>();

		return ParseIdentifierList(shardKeyList);
	}

	/// <summary>
	/// Gets the CREATE TABLE statement for the specified table.
	/// </summary>
	private async Task<string> GetCreateTableStatementAsync(string tableName, CancellationToken cancellationToken)
	{
		EnsureConnectionIsOpen();

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = $"SHOW CREATE TABLE {IdentifierHelper.QuoteQualifiedIdentifier(tableName)}";

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken)
			.ConfigureAwait(false);

		if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
		{
			// SHOW CREATE TABLE returns the CREATE TABLE statement in the second column.
			return reader.GetString(1);
		}

		throw new InvalidOperationException($"Unable to retrieve CREATE TABLE statement for {tableName}.");
	}

	/// <summary>
	/// Gets column metadata for the specified table.
	/// </summary>
	public async Task<DataTable> GetTableSchemaAsync(string tableName, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = $"SELECT * FROM {IdentifierHelper.QuoteQualifiedIdentifier(tableName)} LIMIT 0";

		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly, cancellationToken)
			.ConfigureAwait(false);

		return reader.GetSchemaTable()
			?? throw new InvalidOperationException($"Unable to retrieve schema for {tableName}.");
	}

	private void EnsureConnectionIsOpen()
	{
		if (m_connection.State != ConnectionState.Open)
			throw new InvalidOperationException("Connection must be open before detecting schema.");
	}

	private static List<string> ParseIdentifierList(string identifierList)
	{
		var identifiers = new List<string>();
		var current = new StringBuilder();
		var inBackticks = false;

		for (var i = 0; i < identifierList.Length; i++)
		{
			var ch = identifierList[i];

			if (ch == '`')
			{
				if (inBackticks && i + 1 < identifierList.Length && identifierList[i + 1] == '`')
				{
					current.Append('`');
					i++;
				}
				else
				{
					inBackticks = !inBackticks;
				}
			}
			else if (ch == ',' && !inBackticks)
			{
				AddIdentifier();
			}
			else
			{
				current.Append(ch);
			}
		}

		if (inBackticks)
			throw new InvalidOperationException("Invalid shard key definition: unterminated quoted identifier.");

		AddIdentifier();
		return identifiers;

		void AddIdentifier()
		{
			var identifier = current.ToString().Trim();
			if (identifier.Length != 0)
				identifiers.Add(identifier);

			current.Clear();
		}
	}
}
