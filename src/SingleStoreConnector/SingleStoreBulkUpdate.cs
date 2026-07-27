using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

// TODO: PLAT-8044 (consider upsert support in a future version)

/// <summary>
/// <para><see cref="SingleStoreBulkUpdate"/> lets you efficiently update many existing rows in a SingleStore table
/// from an in-memory source. It complements <see cref="SingleStoreBulkCopy"/>: where bulk copy <em>inserts</em> rows,
/// bulk update <em>modifies</em> rows that already exist, matching them on the columns in <see cref="KeyColumns"/>.</para>
/// <para>The source rows are first staged into a temporary table using <see cref="SingleStoreBulkCopy"/>, then a single
/// <c>UPDATE ... JOIN</c> copies the non-key column values into the matching rows of the destination table.</para>
/// <para>Because staging uses <see cref="SingleStoreBulkCopy"/>, which loads data via <c>LOAD DATA LOCAL INFILE</c>,
/// the connection string <em>must</em> have <c>AllowLoadLocalInfile=true</c> in order to use this class.</para>
/// <para>Example code:</para>
/// <code>
/// // open a connection that is allowed to load local data
/// await using var connection = new SingleStoreConnection("...;AllowLoadLocalInfile=True");
/// await connection.OpenAsync();
///
/// // the source data; the column ordinals are referenced by the column mappings below
/// var dataTable = new DataTable
/// {
///     Columns = { new DataColumn("id", typeof(int)), new DataColumn("status", typeof(string)) },
///     Rows = { { 1, "active" }, { 2, "disabled" } },
/// };
///
/// // update the "status" column of the rows whose "id" matches
/// var bulkUpdate = new SingleStoreBulkUpdate(connection)
/// {
///     DestinationTableName = "users",
///     KeyColumns = { "id" },
///     ColumnMappings =
///     {
///         new SingleStoreBulkCopyColumnMapping(0, "id"),     // source column 0 -&gt; key column "id"
///         new SingleStoreBulkCopyColumnMapping(1, "status"), // source column 1 -&gt; updated column "status"
///     },
/// };
/// var result = await bulkUpdate.WriteToServerAsync(dataTable);
///
/// // check for problems
/// if (result.Warnings.Count != 0) { /* handle potential data loss warnings */ }
/// </code>
/// </summary>
/// <remarks>
/// <para>The following restrictions apply, and <c>WriteToServer</c> throws if they are not met: <see cref="KeyColumns"/>
/// is required and every key column must be mapped; at least one non-key column must be mapped; the source must not
/// contain duplicate key values; shard key columns cannot be updated; generated (computed) columns cannot be mapped;
/// reference tables are not supported; and expression column mappings are not supported.</para>
/// <para>An instance of this class is not thread-safe; do not share an instance across concurrent operations.</para>
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
	/// The name of the table whose rows are updated.
	/// </summary>
	/// <remarks>This name needs to be quoted if it contains special characters.</remarks>
	public string? DestinationTableName { get; set; }

	/// <summary>
	/// The columns that identify which rows to update. They form the <c>JOIN</c> condition between the destination
	/// table and the staging table, so every key column must also appear in <see cref="ColumnMappings"/>.
	/// </summary>
	public List<string> KeyColumns { get; }

	/// <summary>
	/// A collection of <see cref="SingleStoreBulkCopyColumnMapping"/> objects that map source column ordinals onto
	/// destination column names. Every key column and at least one non-key (updated) column must be mapped.
	/// </summary>
	public List<SingleStoreBulkCopyColumnMapping> ColumnMappings { get; }

	/// <summary>
	/// The number of seconds for each phase of the operation to complete before it times out, or <c>0</c> for no
	/// timeout (the default). A single bulk update can spend a long time staging, counting, or updating, so a
	/// finite timeout should be chosen deliberately.
	/// </summary>
	public int BulkUpdateTimeout { get; set; }

	/// <summary>
	/// If non-zero, this specifies the number of rows to be staged before raising the <see cref="SingleStoreRowsStaged"/>
	/// event. This applies only to the staging phase, not to the <c>UPDATE</c> execution.
	/// </summary>
	public int NotifyAfter { get; set; }

	/// <summary>
	/// Whether to compute <see cref="SingleStoreBulkUpdateResult.RowsMatched"/> via a <c>COUNT</c> query (default <c>true</c>).
	/// Set this to <c>false</c> to skip that query for better performance, in which case
	/// <see cref="SingleStoreBulkUpdateResult.RowsMatched"/> is <c>null</c>.
	/// </summary>
	public bool ComputeRowsMatched { get; set; } = true;

	/// <summary>
	/// This event is raised every time that the number of rows specified by the <see cref="NotifyAfter"/> property have been processed.
	/// </summary>
	/// <remarks>
	/// <para>Receipt of a RowsStaged event does not imply that any rows have been sent to the server or committed.</para>
	/// <para>The <see cref="SingleStoreRowsStagedEventArgs.Abort"/> property can be set to <c>true</c> by the event handler
	/// to cancel the operation. Aborting stops staging and skips the <c>UPDATE</c>, so no rows in the destination table
	/// are modified.</para>
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

		// Staging loads the source rows with LOAD DATA LOCAL INFILE (via SingleStoreBulkCopy), which requires
		// AllowLoadLocalInfile=true. Check it up front (reading the connection string works whether or not the
		// connection is open) so a misconfigured connection fails with a clear message before any command runs.
		if (!new SingleStoreConnectionStringBuilder(m_connection.ConnectionString).AllowLoadLocalInfile)
			throw new NotSupportedException("SingleStoreBulkUpdate requires AllowLoadLocalInfile=true in the connection string, because it stages data using LOAD DATA LOCAL INFILE.");

		// Snapshot all operation inputs now so that a SingleStoreRowsStaged event handler (which fires between
		// staging and the UPDATE) cannot change the destination table, key columns or mappings mid-operation and
		// produce a staging table and UPDATE built from different configurations.
		var plan = CreatePlan();

		// Reset any warnings from a previous call so the result only reflects this operation.
		m_warnings.Clear();

		// Materialize a lazy DataRow sequence once, so its row count is known (which makes empty input return a
		// consistent zero-count result regardless of source type) and so SingleStoreBulkCopy can enumerate it
		// without re-running the original (possibly single-use) source.
		if (source is IEnumerable<DataRow> dataRows && source is not ICollection<DataRow> && source is not IReadOnlyCollection<DataRow>)
			source = dataRows.ToList();

		// Short-circuit input whose row count is known to be zero: there is nothing to stage or update, so avoid
		// opening the connection and creating a staging table. (An IDataReader's count is unknown, so it still
		// flows through and stages zero rows naturally.)
		if (GetRowCount(source) == 0)
			return CreateResult(rowsStaged: 0, rowsMatched: ComputeRowsMatched ? 0 : null, rowsAffected: 0);

		var stopwatch = Stopwatch.StartNew();

		// All phases must run on one open session because the staging table is a session-scoped temporary table.
		// Open the connection if the caller left it closed, and close it again only if we were the ones to open it.
		var closeConnection = false;
		if (m_connection.State != ConnectionState.Open)
		{
			await m_connection.OpenAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			closeConnection = true;
		}

		// A single SchemaDetector per operation so the destination's SHOW CREATE TABLE is fetched once and reused
		// across reference-table, shard-key, generated-column and column-type inspection.
		var schemaDetector = new SchemaDetector(m_connection, m_transaction, BulkUpdateTimeout);

		string? tempTableName = null;
		try
		{
			// Reject reference tables and shard-key/generated updates, and confirm every mapped column exists.
			await ValidateSchemaAsync(schemaDetector, plan, ioBehavior, cancellationToken).ConfigureAwait(false);

			Log.StartingBulkUpdate(m_logger, plan.DestinationTableName, string.Join(", ", plan.KeyColumns), string.Join(", ", plan.UpdateColumns), GetRowCount(source));

			// Phase 1: create the staging table mirroring the destination column types.
			tempTableName = await CreateStagingTableAsync(schemaDetector, plan, ioBehavior, cancellationToken).ConfigureAwait(false);

			// Phase 2: stage the source rows into the temporary table via SingleStoreBulkCopy.
			var (rowsStaged, aborted) = await StageDataAsync(plan, tempTableName, source, ioBehavior, cancellationToken).ConfigureAwait(false);

			// If the caller aborted staging via the SingleStoreRowsStaged event, abort the whole operation: do not
			// run the UPDATE, so no rows are modified. Only the staging table (dropped below) was touched.
			if (aborted)
			{
				stopwatch.Stop();
				Log.CompletedBulkUpdate(m_logger, rowsStaged, -1, 0, stopwatch.ElapsedMilliseconds);
				return CreateResult(rowsStaged, rowsMatched: null, rowsAffected: 0);
			}

			// Phase 3 (optional): count how many staged rows match a destination row.
			var rowsMatched = await ComputeMatchedRowsAsync(plan, tempTableName, ioBehavior, cancellationToken).ConfigureAwait(false);
			if (rowsMatched is { } matched && rowsStaged > matched)
				Log.LargeUnmatchedCountForBulkUpdate(m_logger, rowsStaged, matched, rowsStaged - matched);

			// Phase 4: run the UPDATE ... JOIN that copies the non-key values into the matching rows.
			var rowsAffected = await ExecuteUpdateAsync(plan, tempTableName, ioBehavior, cancellationToken).ConfigureAwait(false);

			stopwatch.Stop();
			Log.CompletedBulkUpdate(m_logger, rowsStaged, rowsMatched ?? -1, rowsAffected, stopwatch.ElapsedMilliseconds);

			// RowsMatched is null when ComputeRowsMatched was false (the count was intentionally skipped).
			return CreateResult(rowsStaged, rowsMatched, rowsAffected);
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
	/// Captures an immutable snapshot of the operation inputs (destination table, key columns, column mappings and
	/// the derived update columns) so the rest of the operation is unaffected by later mutations of the public
	/// properties.
	/// </summary>
	private BulkUpdatePlan CreatePlan()
	{
		var destinationTableName = DestinationTableName ??
			throw new InvalidOperationException("DestinationTableName must be set before calling WriteToServer.");

		return new BulkUpdatePlan(
			destinationTableName,
			[.. KeyColumns],
			[.. ColumnMappings],
			GetUpdateColumns());
	}

	/// <summary>
	/// An immutable snapshot of the inputs for a single bulk update operation.
	/// </summary>
	private sealed class BulkUpdatePlan(
		string destinationTableName,
		IReadOnlyList<string> keyColumns,
		IReadOnlyList<SingleStoreBulkCopyColumnMapping> columnMappings,
		IReadOnlyList<string> updateColumns)
	{
		public string DestinationTableName { get; } = destinationTableName;
		public IReadOnlyList<string> KeyColumns { get; } = keyColumns;
		public IReadOnlyList<SingleStoreBulkCopyColumnMapping> ColumnMappings { get; } = columnMappings;
		public IReadOnlyList<string> UpdateColumns { get; } = updateColumns;
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

	/// <summary>
	/// Builds the operation result, snapshotting the warnings collected so far into a new list so that a result
	/// returned from one call is not mutated when the same <see cref="SingleStoreBulkUpdate"/> instance is reused.
	/// </summary>
	private SingleStoreBulkUpdateResult CreateResult(int rowsStaged, int? rowsMatched, int rowsAffected) =>
		new(new List<SingleStoreError>(m_warnings), rowsStaged, rowsMatched, rowsAffected);

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

	private static async ValueTask ValidateSchemaAsync(SchemaDetector schemaDetector, BulkUpdatePlan plan, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var tableName = plan.DestinationTableName;

		// TODO: PLAT-8044 (make changes to support the solutions described here -- https://docs.singlestore.com/cloud/reference/troubleshooting-reference/query-errors/error-1706-hy-000-feature-multi-table-update-delete-with-a-reference-table-as-target-table-is-not-supported-by-memsql/)
		if (await schemaDetector.IsReferenceTableAsync(tableName, ioBehavior, cancellationToken).ConfigureAwait(false))
			throw new NotSupportedException($"Target table '{tableName}' is a reference table. Bulk updates on reference tables are not supported in this version.");

		var shardKeyColumns = await schemaDetector.GetShardKeyColumnsAsync(tableName, ioBehavior, cancellationToken).ConfigureAwait(false);

		foreach (var updateColumn in plan.UpdateColumns)
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

		foreach (var columnMapping in plan.ColumnMappings)
		{
			if (!tableColumns.Contains(columnMapping.DestinationColumn))
				throw new InvalidOperationException($"Column '{columnMapping.DestinationColumn}' does not exist in target table '{tableName}'.");
		}

		// Reject mapped generated (computed) columns with a clear error. An update column cannot be assigned (its
		// value is derived from an expression), and a key column cannot be staged either: the staging table mirrors
		// the destination column's type, but a generated column's definition (AS (expr) [PERSISTED] type) has no
		// plain, reproducible column type to copy. Without this check the operation would fail later with a
		// confusing server error.
		var generatedColumns = await schemaDetector.GetGeneratedColumnsAsync(tableName, ioBehavior, cancellationToken).ConfigureAwait(false);
		if (generatedColumns.Count != 0)
		{
			foreach (var columnMapping in plan.ColumnMappings)
			{
				if (generatedColumns.Contains(columnMapping.DestinationColumn))
					throw new NotSupportedException($"Column '{columnMapping.DestinationColumn}' is a generated (computed) column, which is not supported by SingleStoreBulkUpdate.");
			}
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
	/// <param name="schemaDetector">The schema detector used to read the destination table's definition.</param>
	/// <param name="plan">The snapshot of the operation inputs.</param>
	/// <param name="ioBehavior">Whether to perform database I/O synchronously or asynchronously.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>The name of the created temporary table.</returns>
	/// <remarks>
	/// <para>
	/// Column type definitions are copied verbatim from <c>SHOW CREATE TABLE</c> rather than reconstructed from
	/// <c>GetSchemaTable()</c>. The schema table is lossy for several SingleStore types (for example
	/// <c>VARBINARY</c> is reported as <c>BLOB</c> and <c>BIT(1)</c> as <c>BIGINT</c>, and <c>UNSIGNED</c>,
	/// character set, collation and <c>ENUM</c>/<c>SET</c> member lists are not exposed), so copying the exact
	/// definition is the only way to guarantee the staging column matches the destination column. Matching the
	/// collation in particular keeps the key-column equality used by the <c>UPDATE ... JOIN</c> well-defined.
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
	private async Task<string> CreateStagingTableAsync(SchemaDetector schemaDetector, BulkUpdatePlan plan, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// Generate a unique temporary table name.
		var tempTableName = $"_bulk_update_staging_{Guid.NewGuid():N}";

		var destinationTableName = plan.DestinationTableName;

		// Pull the exact, server-rendered type definition for every column so the staging columns are
		// byte-for-byte type compatible with the destination (see remarks).
		var columnTypeDefinitions = await schemaDetector.GetColumnTypeDefinitionsAsync(destinationTableName, ioBehavior, cancellationToken).ConfigureAwait(false);
		var shardKeyColumns = await schemaDetector.GetShardKeyColumnsAsync(destinationTableName, ioBehavior, cancellationToken).ConfigureAwait(false);

		// Emit a column definition for each mapped column, preserving the order in which the columns appear
		// in the destination table is unnecessary here: the staging table only needs the columns to exist by
		// name. SingleStoreBulkCopy maps source ordinals to these destination column names when staging.
		var keyColumnSet = new HashSet<string>(plan.KeyColumns, StringComparer.OrdinalIgnoreCase);
		var seenColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var columnDefinitions = new List<string>(plan.ColumnMappings.Count);

		foreach (var mapping in plan.ColumnMappings)
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
		var primaryKey = $"PRIMARY KEY ({string.Join(", ", plan.KeyColumns.Select(IdentifierHelper.QuoteIdentifier))})";

		var stagingShardKey = ComputeStagingShardKey(plan, shardKeyColumns, keyColumnSet);

		// Create the staging table as ROWSTORE explicitly: it is small, primary-key indexed and short-lived
		var createTableSql = new StringBuilder();
		createTableSql.Append("CREATE ROWSTORE TEMPORARY TABLE ");
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
			cmd.CommandTimeout = BulkUpdateTimeout;
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
	private List<string> ComputeStagingShardKey(BulkUpdatePlan plan, List<string> destinationShardKeyColumns, HashSet<string> keyColumnSet)
	{
		if (destinationShardKeyColumns.Count == 0)
			return [];

		if (destinationShardKeyColumns.All(keyColumnSet.Contains))
			return destinationShardKeyColumns;

		Log.ShardKeyMismatchForBulkUpdate(
			m_logger,
			string.Join(", ", plan.KeyColumns),
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
	private async Task<(int RowsStaged, bool Aborted)> StageDataAsync(BulkUpdatePlan plan, string tempTableName, object source, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var bulkCopy = new SingleStoreBulkCopy(m_connection, m_transaction)
		{
			DestinationTableName = tempTableName,
			BulkCopyTimeout = BulkUpdateTimeout,
			NotifyAfter = NotifyAfter,
		};

		// Forward our column mappings unchanged: source ordinal -> staging column name.
		foreach (var mapping in plan.ColumnMappings)
			bulkCopy.ColumnMappings.Add(mapping);

		// Re-raise SingleStoreBulkCopy's progress event as a bulk-update staging event, and propagate the
		// caller's request to abort. Only subscribe when progress notifications are actually requested.
		var aborted = false;
		void OnRowsCopied(object sender, SingleStoreRowsCopiedEventArgs e)
		{
			var args = new SingleStoreRowsStagedEventArgs { RowsStaged = e.RowsCopied };
			SingleStoreRowsStaged?.Invoke(this, args);
			if (args.Abort)
			{
				aborted = true;
				e.Abort = true;
			}
		}

		var notifyProgress = NotifyAfter > 0 && SingleStoreRowsStaged is not null;
		if (notifyProgress)
			bulkCopy.SingleStoreRowsCopied += OnRowsCopied;

		try
		{
			var result = await StageWithBulkCopyAsync(bulkCopy, source, ioBehavior, cancellationToken).ConfigureAwait(false);

			m_warnings.AddRange(result.Warnings);

			Log.StagedDataForBulkUpdate(m_logger, result.RowsInserted, result.Warnings.Count);

			return (result.RowsInserted, aborted);
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
	/// For a <see cref="DataRow"/> sequence, <see cref="SingleStoreBulkCopy"/> needs the column count up front
	/// (taken from the owning <see cref="DataTable"/> of the first row); the caller has already materialized any
	/// lazy sequence and short-circuited empty input, so the sequence is a non-empty collection here.
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
	/// Counts how many staged rows match at least one row in the destination table, joined on the key columns.
	/// </summary>
	/// <param name="tempTableName">The session-scoped staging table populated by <see cref="StageDataAsync"/>.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>
	/// The number of staged rows that match at least one destination row, or <see langword="null"/> when
	/// <see cref="ComputeRowsMatched"/> is <see langword="false"/> (the count was intentionally skipped).
	/// </returns>
	/// <remarks>
	/// <para>
	/// This counts staged rows (via <c>WHERE EXISTS</c>) rather than joined pairs, so the result never exceeds the
	/// number of staged rows even when the destination key columns are not unique and a single staged row matches
	/// several destination rows. It lets the caller distinguish staged rows that matched a destination row from
	/// staged rows that matched nothing (<c>RowsStaged - RowsMatched</c>). Callers that do not need this distinction
	/// can set <see cref="ComputeRowsMatched"/> to <see langword="false"/> to skip the query.
	/// </para>
	/// <para>
	/// The match uses the key columns, which were created in the staging table with the destination's exact type and
	/// collation (see <see cref="CreateStagingTableAsync"/>), so this count is consistent with the rows the UPDATE
	/// will match. It must run on the same open connection/transaction as the rest of the operation because the
	/// staging table is session-scoped.
	/// </para>
	/// </remarks>
	private async Task<int?> ComputeMatchedRowsAsync(BulkUpdatePlan plan, string tempTableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!ComputeRowsMatched)
			return null;

		// Count staged rows that have at least one matching destination row. Counting from the staging side with
		// EXISTS (rather than COUNT(*) over an INNER JOIN) keeps the result <= RowsStaged when the destination key
		// columns are not unique, so RowsStaged - RowsMatched is a correct count of unmatched staged rows.
		var countSql =
			$"SELECT COUNT(*) FROM {IdentifierHelper.QuoteIdentifier(tempTableName)} AS s " +
			$"WHERE EXISTS (SELECT 1 FROM {IdentifierHelper.QuoteQualifiedIdentifier(plan.DestinationTableName)} AS t WHERE {BuildKeyJoinCondition(plan)})";

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = countSql;
		cmd.Transaction = m_transaction;
		cmd.CommandTimeout = BulkUpdateTimeout;

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
	private async Task<int> ExecuteUpdateAsync(BulkUpdatePlan plan, string tempTableName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// Assign each non-key mapped column from the staging row: t.`c1` = s.`c1`, t.`c2` = s.`c2` ...
		var setClause = string.Join(
			", ",
			plan.UpdateColumns.Select(c => $"t.{IdentifierHelper.QuoteIdentifier(c)} = s.{IdentifierHelper.QuoteIdentifier(c)}"));

		var updateSql =
			$"UPDATE {IdentifierHelper.QuoteQualifiedIdentifier(plan.DestinationTableName)} AS t " +
			$"INNER JOIN {IdentifierHelper.QuoteIdentifier(tempTableName)} AS s ON {BuildKeyJoinCondition(plan)} " +
			$"SET {setClause}";

		using var cmd = m_connection.CreateCommand();
		cmd.CommandText = updateSql;
		cmd.Transaction = m_transaction;
		cmd.CommandTimeout = BulkUpdateTimeout;

		// Collect any warnings raised during the UPDATE. Errors are already IReadOnlyList<SingleStoreError>.
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
			cmd.CommandTimeout = BulkUpdateTimeout;
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
	private static string BuildKeyJoinCondition(BulkUpdatePlan plan) =>
		string.Join(
			" AND ",
			plan.KeyColumns.Select(k => $"t.{IdentifierHelper.QuoteIdentifier(k)} = s.{IdentifierHelper.QuoteIdentifier(k)}"));

	private readonly SingleStoreConnection m_connection;
	private readonly SingleStoreTransaction? m_transaction;
	private readonly ILogger m_logger;
	private readonly List<SingleStoreError> m_warnings;
}
