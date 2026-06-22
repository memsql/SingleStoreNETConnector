using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SingleStoreConnector.Protocol.Serialization;

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
	public async Task<bool> IsReferenceTableAsync(string tableName, IOBehavior ioBehavior, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		var createTableSql = await GetCreateTableStatementAsync(tableName, ioBehavior, cancellationToken)
			.ConfigureAwait(false);

		return referenceTableRegex.IsMatch(createTableSql);
	}

	/// <summary>
	/// Gets the shard key columns for the specified table.
	/// </summary>
	/// <returns>List of shard key column names, or empty list if no shard key.</returns>
	public async Task<List<string>> GetShardKeyColumnsAsync(string tableName, IOBehavior ioBehavior, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		var shardKeys = new List<(int Sequence, string ColumnName)>();

		// Method 1: Check SHOW INDEXES for __SHARDKEY.
		// This is preferred because Seq_in_index preserves composite shard key column order.
		using (var cmd = m_connection.CreateCommand())
		{
			cmd.CommandText = $"SHOW INDEXES FROM {IdentifierHelper.QuoteQualifiedIdentifier(tableName)}";

			await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken)
				.ConfigureAwait(false);

			var keyNameOrdinal = reader.GetOrdinal("Key_name");
			var columnNameOrdinal = reader.GetOrdinal("Column_name");
			var sequenceOrdinal = reader.GetOrdinal("Seq_in_index");

			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
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
		var createTableSql = await GetCreateTableStatementAsync(tableName, ioBehavior, cancellationToken)
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
	private async Task<string> GetCreateTableStatementAsync(string tableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		EnsureConnectionIsOpen();

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = $"SHOW CREATE TABLE {IdentifierHelper.QuoteQualifiedIdentifier(tableName)}";

		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken)
			.ConfigureAwait(false);

		if (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
		{
			// SHOW CREATE TABLE returns the CREATE TABLE statement in the second column.
			return reader.GetString(1);
		}

		throw new InvalidOperationException($"Unable to retrieve CREATE TABLE statement for {tableName}.");
	}

	/// <summary>
	/// Gets the exact type definition (including length/precision, character set and collation) for each
	/// column of the specified table, as it appears in <c>SHOW CREATE TABLE</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The returned type definitions are taken verbatim from the server rather than reconstructed from
	/// <c>GetSchemaTable()</c>. Copying the definition verbatim guarantees the staging column has the
	/// exact same type as the destination column, which keeps key-column equality (including collation)
	/// well-defined in the <c>UPDATE ... JOIN</c>.
	/// </para>
	/// <para>
	/// Only the type portion is returned (data type, any parenthesised arguments, <c>UNSIGNED</c>/<c>ZEROFILL</c>,
	/// <c>CHARACTER SET</c> and <c>COLLATE</c>). Column options that are inappropriate for a staging table —
	/// <c>NOT NULL</c>/<c>NULL</c>, <c>DEFAULT</c>, <c>AUTO_INCREMENT</c>, generated-column expressions and
	/// <c>COMMENT</c> — are intentionally excluded so the caller can decide nullability itself.
	/// </para>
	/// </remarks>
	/// <returns>A case-insensitive map of column name to its type definition.</returns>
	public async Task<Dictionary<string, string>> GetColumnTypeDefinitionsAsync(string tableName, IOBehavior ioBehavior, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		var createTableSql = await GetCreateTableStatementAsync(tableName, ioBehavior, cancellationToken)
			.ConfigureAwait(false);

		var definitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		var body = ExtractTableBody(createTableSql, tableName);
		foreach (var item in SplitTopLevel(body))
		{
			var trimmed = item.TrimStart();

			// In SHOW CREATE TABLE output every column definition begins with a backtick-quoted column name,
			// while table-level constraints (PRIMARY KEY, SHARD KEY, KEY, UNIQUE KEY, CONSTRAINT, ...) begin
			// with an unquoted keyword. Skip anything that is not a column definition.
			if (trimmed.Length == 0 || trimmed[0] != '`')
				continue;

			var (columnName, rest) = ParseQuotedNameAndRest(trimmed);
			var typeDefinition = ExtractTypeDefinition(rest);
			if (typeDefinition.Length != 0)
				definitions[columnName] = typeDefinition;
		}

		return definitions;
	}

	/// <summary>
	/// Gets column metadata for the specified table.
	/// </summary>
	public async Task<DataTable> GetTableSchemaAsync(string tableName, IOBehavior ioBehavior, CancellationToken cancellationToken = default)
	{
		EnsureConnectionIsOpen();

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = $"SELECT * FROM {IdentifierHelper.QuoteQualifiedIdentifier(tableName)} LIMIT 0";

		await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly, ioBehavior, cancellationToken)
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

	/// <summary>
	/// Returns the contents of the column-list parentheses in a <c>CREATE TABLE</c> statement
	/// (everything between the first top-level <c>(</c> and its matching <c>)</c>).
	/// </summary>
	private static string ExtractTableBody(string createTableSql, string tableName)
	{
		var start = -1;
		var depth = 0;

		for (var i = 0; i < createTableSql.Length; i++)
		{
			var ch = createTableSql[i];

			if (ch == '`' || ch == '\'' || ch == '"')
			{
				i = SkipQuoted(createTableSql, i, ch);
				continue;
			}

			if (ch == '(')
			{
				if (depth == 0)
					start = i + 1;
				depth++;
			}
			else if (ch == ')')
			{
				depth--;
				if (depth == 0)
					return createTableSql.Substring(start, i - start);
			}
		}

		throw new InvalidOperationException($"Unable to parse CREATE TABLE statement for {tableName}.");
	}

	/// <summary>
	/// Splits a column/constraint list on commas that are at the top level
	/// (not inside parentheses, backticks or quotes).
	/// </summary>
	private static List<string> SplitTopLevel(string body)
	{
		var items = new List<string>();
		var depth = 0;
		var start = 0;

		for (var i = 0; i < body.Length; i++)
		{
			var ch = body[i];

			if (ch == '`' || ch == '\'' || ch == '"')
			{
				i = SkipQuoted(body, i, ch);
				continue;
			}

			if (ch == '(')
			{
				depth++;
			}
			else if (ch == ')')
			{
				depth--;
			}
			else if (ch == ',' && depth == 0)
			{
				items.Add(body.Substring(start, i - start));
				start = i + 1;
			}
		}

		items.Add(body.Substring(start));
		return items;
	}

	/// <summary>
	/// Parses a leading backtick-quoted identifier from a column definition and returns the unquoted
	/// identifier together with the remainder of the definition (the part after the column name).
	/// </summary>
	private static (string Name, string Remainder) ParseQuotedNameAndRest(string columnDefinition)
	{
		// columnDefinition is known to start with a backtick.
		var name = new StringBuilder();

		for (var i = 1; i < columnDefinition.Length; i++)
		{
			var ch = columnDefinition[i];

			if (ch == '`')
			{
				if (i + 1 < columnDefinition.Length && columnDefinition[i + 1] == '`')
				{
					name.Append('`');
					i++;
				}
				else
				{
					return (name.ToString(), columnDefinition.Substring(i + 1));
				}
			}
			else
			{
				name.Append(ch);
			}
		}

		throw new InvalidOperationException("Invalid column definition: unterminated quoted column name.");
	}

	/// <summary>
	/// Extracts only the type portion of a column definition (the part after the column name), keeping the
	/// data type, any parenthesised arguments, <c>UNSIGNED</c>/<c>ZEROFILL</c> and <c>CHARACTER SET</c>/<c>COLLATE</c>
	/// clauses, and dropping column options such as <c>NOT NULL</c>, <c>DEFAULT</c>, <c>AUTO_INCREMENT</c> and <c>COMMENT</c>.
	/// </summary>
	private static string ExtractTypeDefinition(string rest)
	{
		var tokens = TokenizeTopLevel(rest);
		if (tokens.Count == 0)
			return string.Empty;

		// The first token is always the data type, including any parenthesised arguments such as
		// "varchar(255)", "decimal(18,4)", "bit(1)", "enum('a','b')" or "vector(4, F32)".
		var kept = new List<string> { tokens[0] };

		for (var i = 1; i < tokens.Count;)
		{
			var keyword = tokens[i].ToUpperInvariant();

			if (keyword is "UNSIGNED" or "SIGNED" or "ZEROFILL")
			{
				kept.Add(tokens[i]);
				i++;
			}
			else if (keyword == "CHARACTER" && i + 2 < tokens.Count && string.Equals(tokens[i + 1], "SET", StringComparison.OrdinalIgnoreCase))
			{
				kept.Add(tokens[i]);
				kept.Add(tokens[i + 1]);
				kept.Add(tokens[i + 2]);
				i += 3;
			}
			else if (keyword == "CHARSET" && i + 1 < tokens.Count)
			{
				kept.Add(tokens[i]);
				kept.Add(tokens[i + 1]);
				i += 2;
			}
			else if (keyword == "COLLATE" && i + 1 < tokens.Count)
			{
				kept.Add(tokens[i]);
				kept.Add(tokens[i + 1]);
				i += 2;
			}
			else
			{
				// Anything else (NOT, NULL, DEFAULT, AUTO_INCREMENT, GENERATED, AS, COMMENT, ...) is a
				// column option rather than part of the type, so the type definition ends here.
				break;
			}
		}

		return string.Join(" ", kept);
	}

	/// <summary>
	/// Splits text into whitespace-delimited tokens, treating a parenthesised group as part of the token it
	/// is attached to and never splitting inside parentheses, backticks or quotes.
	/// </summary>
	private static List<string> TokenizeTopLevel(string text)
	{
		var tokens = new List<string>();
		var current = new StringBuilder();
		var depth = 0;

		for (var i = 0; i < text.Length; i++)
		{
			var ch = text[i];

			if (ch == '`' || ch == '\'' || ch == '"')
			{
				var end = SkipQuoted(text, i, ch);
				current.Append(text, i, end - i + 1);
				i = end;
			}
			else if (ch == '(')
			{
				depth++;
				current.Append(ch);
			}
			else if (ch == ')')
			{
				depth--;
				current.Append(ch);
			}
			else if (char.IsWhiteSpace(ch) && depth == 0)
			{
				if (current.Length != 0)
				{
					tokens.Add(current.ToString());
					current.Clear();
				}
			}
			else
			{
				current.Append(ch);
			}
		}

		if (current.Length != 0)
			tokens.Add(current.ToString());

		return tokens;
	}

	/// <summary>
	/// Given the index of an opening quote character (backtick, single quote or double quote), returns the
	/// index of the matching closing quote, accounting for doubled-quote escaping and backslash escaping.
	/// </summary>
	private static int SkipQuoted(string text, int openIndex, char quote)
	{
		for (var i = openIndex + 1; i < text.Length; i++)
		{
			var ch = text[i];

			// Backslash escaping applies inside string literals but not inside backtick identifiers.
			if (ch == '\\' && quote != '`' && i + 1 < text.Length)
			{
				i++;
				continue;
			}

			if (ch == quote)
			{
				// A doubled quote is an escaped quote, not a terminator.
				if (i + 1 < text.Length && text[i + 1] == quote)
				{
					i++;
					continue;
				}

				return i;
			}
		}

		// Unterminated quote: treat the rest of the string as quoted.
		return text.Length - 1;
	}
}
