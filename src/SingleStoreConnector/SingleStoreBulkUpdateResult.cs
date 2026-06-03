namespace SingleStoreConnector;

/// <summary>
/// Represents the result of a <see cref="SingleStoreBulkUpdate"/> operation.
/// </summary>
public sealed class SingleStoreBulkUpdateResult
{
	/// <summary>
	/// The warnings, if any. Users of <see cref="SingleStoreBulkUpdate"/> should check that this collection is empty to avoid
	/// potential data loss from failed data type conversions.
	/// </summary>
	public IReadOnlyList<SingleStoreError> Warnings { get; }

	/// <summary>
	/// The number of rows that were loaded into the staging table during the bulk update operation.
	/// </summary>
	public int RowsStaged { get; }

	/// <summary>
	/// The number of staged rows that matched rows in the destination table.
	/// </summary>
	public int RowsMatched { get; }

	/// <summary>
	/// The number of rows that were updated during the bulk update operation.
	/// </summary>
	public int RowsUpdated { get; }

	internal SingleStoreBulkUpdateResult(
		IReadOnlyList<SingleStoreError> warnings,
		int rowsStaged,
		int rowsMatched,
		int rowsUpdated)
	{
		Warnings = warnings;
		RowsStaged = rowsStaged;
		RowsMatched = rowsMatched;
		RowsUpdated = rowsUpdated;
	}
}
