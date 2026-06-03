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
        m_logger = m_connection.LoggingConfiguration.BulkCopyLogger;
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

    // TODO: Add WriteToServer overloads in next steps
    private readonly SingleStoreConnection m_connection;
    private readonly SingleStoreTransaction m_transaction;
    private readonly ILogger m_logger;
    private readonly List<SingleStoreError> m_warnings;
}
