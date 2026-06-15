using System.Data;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

// TODO: consider upsert support in a future version.

/// <summary>
/// Provides efficient bulk update operations for SingleStore databases.
/// </summary>
/// <remarks>
/// <para>
/// This class stages source rows into a temporary table using <see cref="SingleStoreBulkCopy"/>,
/// then updates matching rows in the destination table using a single <c>UPDATE ... JOIN</c> statement.
/// </para>
/// <para>This API is experimental and may change in the future.</para>
/// </remarks>
public sealed class SingleStoreBulkUpdate
{
	/// <summary>
	/// Initializes a <see cref="SingleStoreBulkUpdate"/> object with the specified connection, and optionally the active transaction.
	/// </summary>
	/// <param name="connection">The <see cref="SingleStoreConnection"/> to use.</param>
	/// <param name="transaction">(Optional) The <see cref="SingleStoreTransaction"/> to use.</param>
    public SingleStoreBulkUpdate(SingleStoreConnection connection, SingleStoreTransaction? transaction = null)
    {
        m_connection = connection ?? throw new ArgumentNullException(nameof(connection));
        m_transaction = transaction;
        m_logger = m_connection.LoggingConfiguration.BulkUpdateLogger;
        m_warnings = [];
        ColumnMappings = [];
        KeyColumns = [];
    }

    /// <summary>
    /// Gets or sets the name of the destination table.
    /// </summary>
    public string? DestinationTableName { get; set; }

    /// <summary>
    /// Gets the list of key columns used for the JOIN condition.
    /// These columns identify which rows to update.
    /// </summary>
    public List<string> KeyColumns { get; }

    /// <summary>
    /// Gets the collection of column mappings between source data and destination table.
    /// </summary>
    public List<SingleStoreBulkCopyColumnMapping> ColumnMappings { get; }

    /// <summary>
    /// Gets or sets the timeout in seconds for bulk operations.
    /// </summary>
    public int BulkCopyTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of rows to stage before firing the SingleStoreRowsStaged event.
    /// Only applies to the staging phase (LOAD DATA), not the UPDATE execution.
    /// Set to 0 to disable progress notifications.
    /// </summary>
    public int NotifyAfter { get; set; }

    /// <summary>
    /// Gets or sets whether to compute the RowsMatched count via a COUNT query.
    /// Default is true. Set to false to skip the COUNT query for better performance.
    /// When false, RowsMatched will be null in the result.
    /// </summary>
    public bool ComputeRowsMatched { get; set; } = true;

    /*/// <summary>
    /// This event is raised every time that the number of rows specified by the <see cref="NotifyAfter"/> property have been processed.
    /// </summary>
    /// <remarks>
    /// <para>Receipt of a RowsStaged event does not imply that any rows have been sent to the server or committed.</para>
    /// <para>The <see cref="SingleStoreRowsStagedEventArgs.Abort"/> property can be set to <c>true</c> by the event handler to abort the staging.</para>
    /// </remarks>
    public event SingleStoreRowsStagedEventHandler? SingleStoreRowsStaged;*/

    private void ValidateColumnMappings()
	{
		// Ensure the caller specified at least one key column.
		// Key columns define the JOIN condition between the destination table and the staging table.
		if (KeyColumns.Count == 0)
			throw new InvalidOperationException("KeyColumns must contain at least one column. KeyColumns are required in this version.");

		// Ensure the caller explicitly mapped the source data to destination columns.
		// Bulk update needs mappings to know which columns should be staged and which non-key columns should be updated.
		if (ColumnMappings.Count == 0)
			throw new InvalidOperationException("ColumnMappings cannot be empty. Add at least one column mapping.");

		// Validate destination column mappings.
		// Each mapping must have a destination column, destination columns must be unique,
		// and expression mappings are not supported by bulk update in this version.
		var seenColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var mapping in ColumnMappings)
		{
			// Ensure every mapping points to a real destination column name.
			if (string.IsNullOrWhiteSpace(mapping.DestinationColumn))
				throw new InvalidOperationException("ColumnMappings contains a mapping with a null or empty DestinationColumn.");

			// Ensure the same destination column isn't mapped more than once.
			// Duplicate mappings would make the staging table and UPDATE SET clause ambiguous.
			if (!seenColumns.Add(mapping.DestinationColumn))
				throw new InvalidOperationException($"ColumnMappings contains duplicate destination column '{mapping.DestinationColumn}'.");

			// Reject expression mappings for now.
			// Bulk update stages real destination columns into a temporary table, while expression mappings
			// may use user variables such as @tmp that are not real staging table columns.
			if (mapping.Expression is not null)
				throw new NotSupportedException("Expression column mappings are not supported by SingleStoreBulkUpdate in this version.");
		}

		// Validate key column names.
		// Key columns must be non-empty and unique because they are used to build the JOIN condition.
		var keyColumnsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var keyColumn in KeyColumns)
		{
			// Ensure the key column has a valid name.
			if (string.IsNullOrWhiteSpace(keyColumn))
				throw new InvalidOperationException("KeyColumns cannot contain null or empty column names.");

			// Ensure the same key column isn't specified more than once.
			if (!keyColumnsSet.Add(keyColumn))
				throw new InvalidOperationException($"KeyColumns contains duplicate column '{keyColumn}'.");
		}

		// Ensure every key column is included in ColumnMappings.
		// The staging table must contain the key columns so the UPDATE JOIN can match rows.
		foreach (var keyColumn in keyColumnsSet)
		{
			if (!seenColumns.Contains(keyColumn))
			{
				throw new InvalidOperationException(
					$"Key column '{keyColumn}' not found in ColumnMappings. All key columns must be mapped.");
			}
		}

		// Ensure there is at least one non-key column to update.
		// If all mapped columns are key columns, the UPDATE statement would have an empty SET clause.
		if (GetUpdateColumns().Count == 0)
			throw new InvalidOperationException("ColumnMappings must contain at least one non-key column to update.");
	}

    private async ValueTask ValidateSchemaAsync(string tableName, CancellationToken cancellationToken)
	{
		var schemaDetector = new SchemaDetector(m_connection);

		// TODO: make changes to support the solutions described here -- https://docs.singlestore.com/cloud/reference/troubleshooting-reference/query-errors/error-1706-hy-000-feature-multi-table-update-delete-with-a-reference-table-as-target-table-is-not-supported-by-memsql/
		if (await schemaDetector.IsReferenceTableAsync(tableName, cancellationToken).ConfigureAwait(false))
			throw new NotSupportedException($"Target table '{tableName}' is a reference table. Bulk updates on reference tables are not supported in this version.");

		var shardKeyColumns = await schemaDetector.GetShardKeyColumnsAsync(tableName, cancellationToken).ConfigureAwait(false);
		var updateColumns = GetUpdateColumns();

		foreach (var updateColumn in updateColumns)
		{
			if (shardKeyColumns.Contains(updateColumn, StringComparer.OrdinalIgnoreCase))
				throw new InvalidOperationException($"Column '{updateColumn}' is a shard key. SingleStore does not support updating shard key columns.");
		}

		var schema = await schemaDetector.GetTableSchemaAsync(tableName, cancellationToken).ConfigureAwait(false);

		var tableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (DataRow row in schema.Rows)
		{
			var columnName = row["ColumnName"]?.ToString();
			if (columnName is not null)
				tableColumns.Add(columnName);
		}

		foreach (var columnMapping in ColumnMappings)
		{
			if (!tableColumns.Contains(columnMapping.DestinationColumn))
				throw new InvalidOperationException($"Column '{columnMapping.DestinationColumn}' does not exist in target table '{tableName}'.");
		}
	}

    private List<string> GetUpdateColumns() =>
		ColumnMappings
			.Select(x => x.DestinationColumn)
			.Where(x => !KeyColumns.Contains(x, StringComparer.OrdinalIgnoreCase))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

	/// <summary>
	/// Creates a session-scoped temporary staging table containing only the mapped (key + update) columns
	/// of the destination table, ready to receive the source data via <see cref="SingleStoreBulkCopy"/>.
	/// </summary>
	/// <param name="destinationTableName">The destination table whose column types are mirrored.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>The name of the created temporary table.</returns>
	/// <remarks>
	/// <para>
	/// Column type definitions are copied verbatim from <c>SHOW CREATE TABLE</c> rather than reconstructed from
	/// <c>GetSchemaTable()</c>. The schema table is lossy for several SingleStore types (for example
	/// <c>VARBINARY</c> is reported as <c>BLOB</c> and <c>BIT(1)</c> as <c>BIGINT</c>, and <c>UNSIGNED</c>,
	/// character set, collation and <c>ENUM</c>/<c>SET</c> member lists are not exposed), so copying the exact
	/// definition is the only way to guarantee the staging column matches the destination column. Matching the
	/// collation in particular keeps the key-column equality used by the <c>UPDATE ... JOIN</c> well defined.
	/// </para>
	/// <para>
	/// The key columns form the staging table's <c>PRIMARY KEY</c>, so they are always declared <c>NOT NULL</c>
	/// even when the destination column is nullable (a nullable primary key column is not allowed, and SQL
	/// equality on <c>NULL</c> would not match rows in the join anyway). When the destination table's shard key
	/// is a subset of the key columns, the staging table is sharded the same way so the join can run locally;
	/// otherwise it falls back to the primary key distribution and logs a shard-key mismatch warning.
	/// </para>
	/// <para>
	/// This must run on the same open connection (and transaction) used for staging, counting and updating,
	/// because the temporary table is session-scoped.
	/// </para>
	/// </remarks>
	private async Task<string> CreateStagingTableAsync(string destinationTableName, CancellationToken cancellationToken)
	{
		// Generate a unique temporary table name. The "g" suffix on the GUID guarantees the identifier
		// starts with a letter regardless of the GUID's first hex digit.
		var tempTableName = $"_bulk_update_staging_g{Guid.NewGuid():N}";

		var schemaDetector = new SchemaDetector(m_connection);

		// Pull the exact, server-rendered type definition for every column so the staging columns are
		// byte-for-byte type compatible with the destination (see remarks).
		var columnTypeDefinitions = await schemaDetector.GetColumnTypeDefinitionsAsync(destinationTableName, cancellationToken).ConfigureAwait(false);
		var shardKeyColumns = await schemaDetector.GetShardKeyColumnsAsync(destinationTableName, cancellationToken).ConfigureAwait(false);

		// Emit a column definition for each mapped column, preserving the order in which the columns appear
		// in the destination table is unnecessary here: the staging table only needs the columns to exist by
		// name. SingleStoreBulkCopy maps source ordinals to these destination column names when staging.
		var keyColumnSet = new HashSet<string>(KeyColumns, StringComparer.OrdinalIgnoreCase);
		var seenColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var columnDefinitions = new List<string>(ColumnMappings.Count);

		foreach (var mapping in ColumnMappings)
		{
			var columnName = mapping.DestinationColumn;

			// ColumnMappings is validated for duplicates earlier, but guard anyway so a duplicate can never
			// produce an invalid CREATE TABLE with two columns of the same name.
			if (!seenColumns.Add(columnName))
				continue;

			if (!columnTypeDefinitions.TryGetValue(columnName, out var typeDefinition))
				throw new InvalidOperationException($"Column '{columnName}' not found in destination table '{destinationTableName}'.");

			// Key columns become the staging primary key, so they must be NOT NULL even when nullable in the
			// destination. Update columns are left nullable so a NULL source value stages successfully; any
			// real NOT NULL violation then surfaces against the destination during the UPDATE.
			var nullability = keyColumnSet.Contains(columnName) ? "NOT NULL" : "NULL";

			columnDefinitions.Add($"{IdentifierHelper.QuoteIdentifier(columnName)} {typeDefinition} {nullability}");
		}

		// The key columns identify the rows to update, so they are the natural primary key of the staging
		// table. This also rejects duplicate keys in the source data with a clear primary-key violation.
		var primaryKey = $"PRIMARY KEY ({string.Join(", ", KeyColumns.Select(IdentifierHelper.QuoteIdentifier))})";

		var stagingShardKey = ComputeStagingShardKey(shardKeyColumns, keyColumnSet);

		var createTableSql = new StringBuilder();
		createTableSql.Append("CREATE TEMPORARY TABLE ");
		createTableSql.Append(IdentifierHelper.QuoteIdentifier(tempTableName));
		createTableSql.Append(" (");
		createTableSql.Append(string.Join(", ", columnDefinitions));
		createTableSql.Append(", ");
		createTableSql.Append(primaryKey);

		if (stagingShardKey.Count != 0)
		{
			createTableSql.Append(", SHARD KEY (");
			createTableSql.Append(string.Join(", ", stagingShardKey.Select(IdentifierHelper.QuoteIdentifier)));
			createTableSql.Append(')');
		}

		createTableSql.Append(')');

		using (var cmd = m_connection.CreateCommand())
		{
			cmd.CommandText = createTableSql.ToString();
			cmd.Transaction = m_transaction;
			cmd.CommandTimeout = BulkCopyTimeout;
			await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
		}

		Log.CreatedStagingTableForBulkUpdate(m_logger, tempTableName, columnDefinitions.Count);

		return tempTableName;
	}

	/// <summary>
	/// Determines the shard key to declare on the staging table so that the <c>UPDATE ... JOIN</c> can run as a
	/// local (non-reshuffled) join whenever possible.
	/// </summary>
	/// <remarks>
	/// A shard key must be a subset of the primary key, which for the staging table is exactly the key columns.
	/// When the destination's shard key is contained in the key columns we reuse it verbatim (preserving its
	/// column order) so both tables hash to the same partitions. When it is not — for example the destination is
	/// sharded on a column that is not a join key — the staging table cannot be aligned, so we fall back to the
	/// primary-key distribution (by returning an empty list, which omits an explicit shard key) and warn.
	/// </remarks>
	private List<string> ComputeStagingShardKey(List<string> destinationShardKeyColumns, HashSet<string> keyColumnSet)
	{
		if (destinationShardKeyColumns.Count == 0)
			return [];

		if (destinationShardKeyColumns.All(keyColumnSet.Contains))
			return destinationShardKeyColumns;

		Log.ShardKeyMismatchForBulkUpdate(
			m_logger,
			string.Join(", ", KeyColumns),
			string.Join(", ", destinationShardKeyColumns));

		return [];
	}

    private readonly SingleStoreConnection m_connection;
    private readonly SingleStoreTransaction? m_transaction;
    private readonly ILogger m_logger;
    private readonly List<SingleStoreError> m_warnings;
}
