using System.Data;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

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
    public SingleStoreBulkUpdate(SingleStoreConnection connection, SingleStoreTransaction transaction)
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

    // TODO: Add WriteToServer overloads in next steps
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
		var hasUpdateColumn = ColumnMappings.Any(m => !keyColumnsSet.Contains(m.DestinationColumn));

		if (!hasUpdateColumn)
			throw new InvalidOperationException("ColumnMappings must contain at least one non-key column to update.");
	}


    private readonly SingleStoreConnection m_connection;
    private readonly SingleStoreTransaction m_transaction;
    private readonly ILogger m_logger;
    private readonly List<SingleStoreError> m_warnings;
}
