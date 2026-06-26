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
	/// The number of staged rows that matched rows in the destination table, or <c>-1</c> when
	/// <see cref="SingleStoreBulkUpdate.ComputeRowsMatched"/> was set to <c>false</c> and the count was not computed.
	/// </summary>
	public int RowsMatched { get; }

	/// <summary>
	/// The number of rows affected by the <c>UPDATE</c>, as reported by the server.
	/// </summary>
	/// <remarks>The exact meaning depends on the connection's <see cref="SingleStoreConnectionStringBuilder.UseAffectedRows"/>
	/// setting. With the default (<c>UseAffectedRows=false</c>) this is the number of rows <em>matched</em> by the update —
	/// including rows that already held the new values — and therefore typically equals <see cref="RowsMatched"/>. With
	/// <c>UseAffectedRows=true</c> it is the number of rows whose values actually <em>changed</em>.</remarks>
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
