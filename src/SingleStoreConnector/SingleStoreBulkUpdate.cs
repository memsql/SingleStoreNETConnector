using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Protocol.Serialization;
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

    /// <summary>
    /// This event is raised every time that the number of rows specified by the <see cref="NotifyAfter"/> property have been processed.
    /// </summary>
    /// <remarks>
    /// <para>Receipt of a RowsStaged event does not imply that any rows have been sent to the server or committed.</para>
    /// <para>The <see cref="SingleStoreRowsStagedEventArgs.Abort"/> property can be set to <c>true</c> by the event handler to abort the staging.</para>
    /// </remarks>
    public event SingleStoreRowsStagedEventHandler? SingleStoreRowsStaged;

	/// <summary>
	/// Updates rows in the destination table using the data in the supplied <see cref="DataTable"/>.
	/// </summary>
	/// <param name="dataTable">The <see cref="DataTable"/> containing the key and update column values.</param>
	/// <returns>A <see cref="SingleStoreBulkUpdateResult"/> describing the result of the operation.</returns>
    public SingleStoreBulkUpdateResult WriteToServer(DataTable dataTable)
	{
		ArgumentNullException.ThrowIfNull(dataTable);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, dataTable, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

	/// <summary>
	/// Asynchronously updates rows in the destination table using the data in the supplied <see cref="DataTable"/>.
	/// </summary>
	/// <param name="dataTable">The <see cref="DataTable"/> containing the key and update column values.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="SingleStoreBulkUpdateResult"/> describing the result of the operation.</returns>
    public async ValueTask<SingleStoreBulkUpdateResult> WriteToServerAsync(DataTable dataTable, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dataTable);
		return await WriteToServerAsync(IOBehavior.Asynchronous, dataTable, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Updates rows in the destination table using the data in the supplied sequence of <see cref="DataRow"/> objects.
	/// </summary>
	/// <param name="dataRows">The collection of <see cref="DataRow"/> objects containing the key and update column values.</param>
	/// <returns>A <see cref="SingleStoreBulkUpdateResult"/> describing the result of the operation.</returns>
    public SingleStoreBulkUpdateResult WriteToServer(IEnumerable<DataRow> dataRows)
	{
		ArgumentNullException.ThrowIfNull(dataRows);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, dataRows, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

	/// <summary>
	/// Asynchronously updates rows in the destination table using the data in the supplied sequence of <see cref="DataRow"/> objects.
	/// </summary>
	/// <param name="dataRows">The collection of <see cref="DataRow"/> objects containing the key and update column values.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="SingleStoreBulkUpdateResult"/> describing the result of the operation.</returns>
    public async ValueTask<SingleStoreBulkUpdateResult> WriteToServerAsync(IEnumerable<DataRow> dataRows, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dataRows);
		return await WriteToServerAsync(IOBehavior.Asynchronous, dataRows, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Updates rows in the destination table using the data read from the supplied <see cref="IDataReader"/>.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to read the key and update column values from.</param>
	/// <returns>A <see cref="SingleStoreBulkUpdateResult"/> describing the result of the operation.</returns>
    public SingleStoreBulkUpdateResult WriteToServer(IDataReader dataReader)
	{
		ArgumentNullException.ThrowIfNull(dataReader);
#pragma warning disable CA2012 // Safe because method completes synchronously
		return WriteToServerAsync(IOBehavior.Synchronous, dataReader, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
	}

	/// <summary>
	/// Asynchronously updates rows in the destination table using the data read from the supplied <see cref="IDataReader"/>.
	/// </summary>
	/// <param name="dataReader">The <see cref="IDataReader"/> to read the key and update column values from.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="SingleStoreBulkUpdateResult"/> describing the result of the operation.</returns>
    public async ValueTask<SingleStoreBulkUpdateResult> WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(dataReader);
		return await WriteToServerAsync(IOBehavior.Asynchronous, dataReader, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// The single implementation behind every <c>WriteToServer</c>/<c>WriteToServerAsync</c> overload.
	/// </summary>
	/// <param name="ioBehavior">
	/// Whether to perform database I/O synchronously or asynchronously. The synchronous public overloads pass
	/// <see cref="IOBehavior.Synchronous"/>, which causes every inner database call to complete inline so the
	/// returned task is already finished — making the <c>GetAwaiter().GetResult()</c> in those overloads safe
	/// (no blocking wait on outstanding async work). This mirrors <see cref="SingleStoreBulkCopy"/>.
	/// </param>
	/// <param name="source">The source data: a <see cref="DataTable"/>, a sequence of <see cref="DataRow"/>, or an <see cref="IDataReader"/>.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    private async ValueTask<SingleStoreBulkUpdateResult> WriteToServerAsync(IOBehavior ioBehavior, object source, CancellationToken cancellationToken)
	{
		// Validate configuration before touching the connection so misconfiguration fails fast and cheaply.
		ValidateColumnMappings();

		var destinationTableName = DestinationTableName ??
			throw new InvalidOperationException("DestinationTableName must be set before calling WriteToServer.");

		// Reset any warnings from a previous call so the result only reflects this operation.
		m_warnings.Clear();

		// Short-circuit input whose row count is known to be zero: there is nothing to stage or update, so avoid
		// opening the connection and creating a staging table. (An IDataReader's count is unknown, so it still
		// flows through and stages zero rows naturally.)
		if (GetRowCount(source) == 0)
			return new SingleStoreBulkUpdateResult(m_warnings.AsReadOnly(), rowsStaged: 0, rowsMatched: ComputeRowsMatched ? 0 : -1, rowsUpdated: 0);

		var stopwatch = Stopwatch.StartNew();

		// All phases must run on one open session because the staging table is a session-scoped temporary table.
		// Open the connection if the caller left it closed, and close it again only if we were the ones to open it.
		var closeConnection = false;
		if (m_connection.State != ConnectionState.Open)
		{
			await m_connection.OpenAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			closeConnection = true;
		}

		string? tempTableName = null;
		try
		{
			// Reject reference tables and shard-key updates, and confirm every mapped column exists.
			await ValidateSchemaAsync(destinationTableName, ioBehavior, cancellationToken).ConfigureAwait(false);

			var updateColumns = GetUpdateColumns();
			Log.StartingBulkUpdate(m_logger, destinationTableName, string.Join(", ", KeyColumns), string.Join(", ", updateColumns), GetRowCount(source));

			// Phase 1: create the staging table mirroring the destination column types.
			tempTableName = await CreateStagingTableAsync(destinationTableName, ioBehavior, cancellationToken).ConfigureAwait(false);

			// Phase 2: stage the source rows into the temporary table via SingleStoreBulkCopy.
			var rowsStaged = await StageDataAsync(tempTableName, source, ioBehavior, cancellationToken).ConfigureAwait(false);

			// Phase 3 (optional): count how many staged rows match a destination row.
			var rowsMatched = await ComputeMatchedRowsAsync(tempTableName, ioBehavior, cancellationToken).ConfigureAwait(false);
			if (rowsMatched is { } matched && rowsStaged > matched)
				Log.LargeUnmatchedCountForBulkUpdate(m_logger, rowsStaged, matched, rowsStaged - matched);

			// Phase 4: run the UPDATE ... JOIN that copies the non-key values into the matching rows.
			var rowsUpdated = await ExecuteUpdateAsync(tempTableName, ioBehavior, cancellationToken).ConfigureAwait(false);

			stopwatch.Stop();
			Log.CompletedBulkUpdate(m_logger, rowsStaged, rowsMatched ?? -1, rowsUpdated, stopwatch.ElapsedMilliseconds);

			// RowsMatched is reported as -1 when ComputeRowsMatched was false (the count was intentionally skipped).
			return new SingleStoreBulkUpdateResult(m_warnings.AsReadOnly(), rowsStaged, rowsMatched ?? -1, rowsUpdated);
		}
		finally
		{
			// Drop the staging table before closing a connection we opened (a closed connection's session, and
			// therefore the temporary table, is already gone).
			await DropStagingTableAsync(tempTableName, ioBehavior).ConfigureAwait(false);

			if (closeConnection)
				m_connection.Close();
		}
	}

	/// <summary>
	/// Returns the number of rows in the source for logging, or <c>-1</c> when the count is not known in advance
	/// (for example an <see cref="IDataReader"/>, which is consumed as it is staged).
	/// </summary>
    private static int GetRowCount(object source) =>
		source switch
		{
			DataTable dataTable => dataTable.Rows.Count,
			ICollection<DataRow> dataRows => dataRows.Count,
			IReadOnlyCollection<DataRow> dataRows => dataRows.Count,
			_ => -1,
		};

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

    private async ValueTask ValidateSchemaAsync(string tableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var schemaDetector = new SchemaDetector(m_connection, m_transaction);

		// TODO: make changes to support the solutions described here -- https://docs.singlestore.com/cloud/reference/troubleshooting-reference/query-errors/error-1706-hy-000-feature-multi-table-update-delete-with-a-reference-table-as-target-table-is-not-supported-by-memsql/
		if (await schemaDetector.IsReferenceTableAsync(tableName, ioBehavior, cancellationToken).ConfigureAwait(false))
			throw new NotSupportedException($"Target table '{tableName}' is a reference table. Bulk updates on reference tables are not supported in this version.");

		var shardKeyColumns = await schemaDetector.GetShardKeyColumnsAsync(tableName, ioBehavior, cancellationToken).ConfigureAwait(false);
		var updateColumns = GetUpdateColumns();

		foreach (var updateColumn in updateColumns)
		{
			if (shardKeyColumns.Contains(updateColumn, StringComparer.OrdinalIgnoreCase))
				throw new InvalidOperationException($"Column '{updateColumn}' is a shard key. SingleStore does not support updating shard key columns.");
		}

		var schema = await schemaDetector.GetTableSchemaAsync(tableName, ioBehavior, cancellationToken).ConfigureAwait(false);

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
    private async Task<string> CreateStagingTableAsync(string destinationTableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// Generate a unique temporary table name. The "g" suffix on the GUID guarantees the identifier
		// starts with a letter regardless of the GUID's first hex digit.
		var tempTableName = $"_bulk_update_staging_g{Guid.NewGuid():N}";

		var schemaDetector = new SchemaDetector(m_connection, m_transaction);

		// Pull the exact, server-rendered type definition for every column so the staging columns are
		// byte-for-byte type compatible with the destination (see remarks).
		var columnTypeDefinitions = await schemaDetector.GetColumnTypeDefinitionsAsync(destinationTableName, ioBehavior, cancellationToken).ConfigureAwait(false);
		var shardKeyColumns = await schemaDetector.GetShardKeyColumnsAsync(destinationTableName, ioBehavior, cancellationToken).ConfigureAwait(false);

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
			await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
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

	/// <summary>
	/// Stages the source rows into the temporary table created by <see cref="CreateStagingTableAsync"/>,
	/// using <see cref="SingleStoreBulkCopy"/> (which loads the data via <c>LOAD DATA LOCAL INFILE</c>).
	/// </summary>
	/// <param name="tempTableName">The session-scoped temporary staging table to load into.</param>
	/// <param name="source">The source data: a <see cref="DataTable"/>, a sequence of <see cref="DataRow"/>, or an <see cref="IDataReader"/>.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>The number of rows staged into the temporary table.</returns>
	/// <remarks>
	/// <para>
	/// This must run on the same open connection (and transaction) as <see cref="CreateStagingTableAsync"/>,
	/// because the temporary table is session-scoped. The caller is responsible for opening the connection
	/// and creating the staging table before calling this method.
	/// </para>
	/// <para>
	/// The bulk-update column mappings are forwarded verbatim to <see cref="SingleStoreBulkCopy"/>. Each
	/// mapping's <see cref="SingleStoreBulkCopyColumnMapping.SourceOrdinal"/> selects a column from the source
	/// data, and its <see cref="SingleStoreBulkCopyColumnMapping.DestinationColumn"/> names a column in the
	/// staging table (which contains exactly the mapped columns by name). This keeps the source-ordinal /
	/// destination-name relationship identical between staging and the later <c>UPDATE ... JOIN</c>.
	/// </para>
	/// </remarks>
    private async Task<int> StageDataAsync(string tempTableName, object source, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var bulkCopy = new SingleStoreBulkCopy(m_connection, m_transaction)
		{
			DestinationTableName = tempTableName,
			BulkCopyTimeout = BulkCopyTimeout,
			NotifyAfter = NotifyAfter,
		};

		// Forward our column mappings unchanged: source ordinal -> staging column name.
		foreach (var mapping in ColumnMappings)
			bulkCopy.ColumnMappings.Add(mapping);

		// Re-raise SingleStoreBulkCopy's progress event as a bulk-update staging event, and propagate the
		// caller's request to abort. Only subscribe when progress notifications are actually requested.
		void OnRowsCopied(object sender, SingleStoreRowsCopiedEventArgs e)
		{
			var args = new SingleStoreRowsStagedEventArgs { RowsStaged = e.RowsCopied };
			SingleStoreRowsStaged?.Invoke(this, args);
			if (args.Abort)
				e.Abort = true;
		}

		var notifyProgress = NotifyAfter > 0 && SingleStoreRowsStaged is not null;
		if (notifyProgress)
			bulkCopy.SingleStoreRowsCopied += OnRowsCopied;

		try
		{
			var result = await StageWithBulkCopyAsync(bulkCopy, source, ioBehavior, cancellationToken).ConfigureAwait(false);

			m_warnings.AddRange(result.Warnings);

			Log.StagedDataForBulkUpdate(m_logger, result.RowsInserted, result.Warnings.Count);

			return result.RowsInserted;
		}
		finally
		{
			if (notifyProgress)
				bulkCopy.SingleStoreRowsCopied -= OnRowsCopied;
		}
	}

	/// <summary>
	/// Dispatches the source data to the appropriate <see cref="SingleStoreBulkCopy"/> overload, selecting the
	/// synchronous or asynchronous method according to <paramref name="ioBehavior"/>.
	/// </summary>
	/// <remarks>
	/// <see cref="SingleStoreBulkCopy"/> exposes separate synchronous (<c>WriteToServer</c>) and asynchronous
	/// (<c>WriteToServerAsync</c>) methods rather than an <see cref="IOBehavior"/> overload, so the behavior is
	/// selected here. Calling the synchronous methods on the synchronous path keeps the whole operation inline,
	/// preserving the no-sync-over-async guarantee that lets the public synchronous overloads block safely.
	/// For a <see cref="DataRow"/> sequence, <see cref="SingleStoreBulkCopy"/> needs the column count before it
	/// enumerates the rows (taken from the owning <see cref="DataTable"/> of the first row); the sequence is
	/// materialized first so a lazy source is not consumed by the peek and then re-enumerated (empty) by the bulk
	/// copy. Empty input is short-circuited before staging, so the sequence is expected to be non-empty here; it
	/// is still guarded so an unexpected empty sequence fails clearly rather than dereferencing a missing row.
	/// </remarks>
    private static ValueTask<SingleStoreBulkCopyResult> StageWithBulkCopyAsync(SingleStoreBulkCopy bulkCopy, object source, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		switch (source)
		{
		case DataTable dataTable:
			return ioBehavior == IOBehavior.Synchronous
				? new ValueTask<SingleStoreBulkCopyResult>(bulkCopy.WriteToServer(dataTable))
				: bulkCopy.WriteToServerAsync(dataTable, cancellationToken);

		case IEnumerable<DataRow> dataRows:
			var rows = dataRows as IReadOnlyList<DataRow> ?? dataRows.ToList();
			if (rows.Count == 0)
				throw new ArgumentException("Cannot stage an empty sequence of rows.", nameof(source));
			var columnCount = rows[0].Table.Columns.Count;
			return ioBehavior == IOBehavior.Synchronous
				? new ValueTask<SingleStoreBulkCopyResult>(bulkCopy.WriteToServer(rows, columnCount))
				: bulkCopy.WriteToServerAsync(rows, columnCount, cancellationToken);

		case IDataReader dataReader:
			return ioBehavior == IOBehavior.Synchronous
				? new ValueTask<SingleStoreBulkCopyResult>(bulkCopy.WriteToServer(dataReader))
				: bulkCopy.WriteToServerAsync(dataReader, cancellationToken);

		default:
			throw new ArgumentException($"Unsupported source type '{source.GetType()}'.", nameof(source));
		}
	}

	/// <summary>
	/// Counts how many staged rows match a row in the destination table, joined on the key columns.
	/// </summary>
	/// <param name="tempTableName">The session-scoped staging table populated by <see cref="StageDataAsync"/>.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>
	/// The number of staged rows that match a destination row, or <see langword="null"/> when
	/// <see cref="ComputeRowsMatched"/> is <see langword="false"/> (the count was intentionally skipped).
	/// </returns>
	/// <remarks>
	/// <para>
	/// This runs an extra <c>SELECT COUNT(*)</c> over the same <c>INNER JOIN</c> that the subsequent
	/// <c>UPDATE ... JOIN</c> uses, letting the caller distinguish staged rows that updated a destination row
	/// from staged rows that matched nothing. Callers that do not need this distinction can set
	/// <see cref="ComputeRowsMatched"/> to <see langword="false"/> to skip the query.
	/// </para>
	/// <para>
	/// The join uses the key columns, which were created in the staging table with the destination's exact
	/// type and collation (see <see cref="CreateStagingTableAsync"/>), so this count is consistent with the
	/// rows the UPDATE will match. It must run on the same open connection/transaction as the rest of the
	/// operation because the staging table is session-scoped.
	/// </para>
	/// </remarks>
    private async Task<int?> ComputeMatchedRowsAsync(string tempTableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!ComputeRowsMatched)
			return null;

		var countSql =
			$"SELECT COUNT(*) FROM {IdentifierHelper.QuoteQualifiedIdentifier(DestinationTableName!)} AS t " +
			$"INNER JOIN {IdentifierHelper.QuoteIdentifier(tempTableName)} AS s ON {BuildKeyJoinCondition()}";

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = countSql;
		cmd.Transaction = m_transaction;
		cmd.CommandTimeout = BulkCopyTimeout;

		var scalar = await cmd.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

		// COUNT(*) comes back as a long; convert rather than cast so the boxed type is handled correctly.
		var rowsMatched = scalar is null or DBNull ? 0 : Convert.ToInt32(scalar, CultureInfo.InvariantCulture);

		Log.QueriedMatchCountForBulkUpdate(m_logger, rowsMatched);

		return rowsMatched;
	}

	/// <summary>
	/// Executes the <c>UPDATE ... JOIN</c> that copies the non-key column values from the staging table into
	/// the matching rows of the destination table.
	/// </summary>
	/// <param name="tempTableName">The session-scoped staging table populated by <see cref="StageDataAsync"/>.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>The number of rows affected by the update, as reported by the server.</returns>
	/// <remarks>
	/// <para>
	/// Rows are matched on the key columns (the same join used by <see cref="ComputeMatchedRowsAsync"/>) and the
	/// non-key mapped columns are assigned from the staging row. The statement runs on the same open
	/// connection/transaction as the rest of the operation because the staging table is session-scoped.
	/// </para>
	/// <para>
	/// The returned count reflects the server's affected-row semantics, which depend on the connection's
	/// <see cref="SingleStoreConnectionStringBuilder.UseAffectedRows"/> setting. With the default
	/// (<c>UseAffectedRows=false</c>, i.e. <c>CLIENT_FOUND_ROWS</c>), the count is the number of rows
	/// <em>matched</em> by the join — including rows that already held the target values — so it typically
	/// equals <see cref="ComputeMatchedRowsAsync"/>'s result. With <c>UseAffectedRows=true</c>, it is the number
	/// of rows whose values actually changed.
	/// </para>
	/// <para>
	/// Warnings raised while executing the statement (for example truncation or conversion warnings) are
	/// collected via the connection's <see cref="SingleStoreConnection.InfoMessage"/> event and surfaced on the
	/// operation result.
	/// </para>
	/// </remarks>
    private async Task<int> ExecuteUpdateAsync(string tempTableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// Assign each non-key mapped column from the staging row: t.`c1` = s.`c1`, t.`c2` = s.`c2` ...
		var setClause = string.Join(
			", ",
			GetUpdateColumns().Select(c => $"t.{IdentifierHelper.QuoteIdentifier(c)} = s.{IdentifierHelper.QuoteIdentifier(c)}"));

		var updateSql =
			$"UPDATE {IdentifierHelper.QuoteQualifiedIdentifier(DestinationTableName!)} AS t " +
			$"INNER JOIN {IdentifierHelper.QuoteIdentifier(tempTableName)} AS s ON {BuildKeyJoinCondition()} " +
			$"SET {setClause}";

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = updateSql;
		cmd.Transaction = m_transaction;
		cmd.CommandTimeout = BulkCopyTimeout;

		// Collect any warnings raised during the UPDATE. Errors is already IReadOnlyList<SingleStoreError>.
		void OnInfoMessage(object sender, SingleStoreInfoMessageEventArgs args) => m_warnings.AddRange(args.Errors);

		m_connection.InfoMessage += OnInfoMessage;
		try
		{
			var rowsUpdated = await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			Log.ExecutedBulkUpdate(m_logger, rowsUpdated);

			return rowsUpdated;
		}
		finally
		{
			m_connection.InfoMessage -= OnInfoMessage;
		}
	}

	/// <summary>
	/// Drops the temporary staging table created by <see cref="CreateStagingTableAsync"/> on a best-effort basis.
	/// </summary>
	/// <param name="tempTableName">The name of the staging table, or <see langword="null"/> if none was created.</param>
	/// <remarks>
	/// <para>
	/// The staging table is session-scoped, so it is discarded automatically when the session ends (for example
	/// when a connection opened by the bulk update is closed, or when a pooled connection is reset). This explicit
	/// drop matters mainly when the caller supplied an already-open connection that they continue to use, freeing
	/// the temporary table promptly rather than leaving it until the session is reset.
	/// </para>
	/// <para>
	/// Cleanup never throws: a failed drop is logged and swallowed so it cannot mask the outcome (or the original
	/// exception) of the bulk update. The drop is skipped when the connection is no longer open, because in that
	/// case the session — and therefore the temporary table — is already gone. <see cref="CancellationToken.None"/>
	/// is used deliberately so cleanup still runs after a cancelled or timed-out operation.
	/// </para>
	/// </remarks>
    private async Task DropStagingTableAsync(string? tempTableName, IOBehavior ioBehavior)
	{
		if (string.IsNullOrEmpty(tempTableName))
			return;

		// A session-scoped temporary table cannot outlive its session, so there is nothing to drop (and no usable
		// connection to issue the command on) once the connection is no longer open.
		if (m_connection.State != ConnectionState.Open)
			return;

		try
		{
			using var cmd = m_connection.CreateCommand();
			cmd.CommandText = $"DROP TEMPORARY TABLE IF EXISTS {IdentifierHelper.QuoteIdentifier(tempTableName!)}";
			cmd.Transaction = m_transaction;
			cmd.CommandTimeout = BulkCopyTimeout;
			await cmd.ExecuteNonQueryAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			// A failed cleanup is non-fatal: the temporary table will be discarded when the session ends.
			Log.FailedToDropStagingTableForBulkUpdate(m_logger, ex, tempTableName!, ex.Message);
		}
	}

	/// <summary>
	/// Builds the key-column equi-join predicate shared by the match-count query and the update, joining the
	/// destination table (alias <c>t</c>) to the staging table (alias <c>s</c>) on every key column.
	/// </summary>
    private string BuildKeyJoinCondition() =>
		string.Join(
			" AND ",
			KeyColumns.Select(k => $"t.{IdentifierHelper.QuoteIdentifier(k)} = s.{IdentifierHelper.QuoteIdentifier(k)}"));

    private readonly SingleStoreConnection m_connection;
    private readonly SingleStoreTransaction? m_transaction;
    private readonly ILogger m_logger;
    private readonly List<SingleStoreError> m_warnings;
}
