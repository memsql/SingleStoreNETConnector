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
	/// The number of staged rows that matched at least one row in the destination table on the key columns, or
	/// <c>null</c> when <see cref="SingleStoreBulkUpdate.ComputeRowsMatched"/> was set to <c>false</c> and the count
	/// was not computed.
	/// </summary>
	/// <remarks>This never exceeds <see cref="RowsStaged"/>; <c>RowsStaged - RowsMatched</c> is the number of staged
	/// rows that matched no destination row. Note that <see cref="RowsAffected"/> can still exceed this when the
	/// destination key columns are not unique, because a single staged row can update several destination rows.</remarks>
	public int? RowsMatched { get; }

	/// <summary>
	/// The number of rows affected by the <c>UPDATE</c>, as reported by the server.
	/// </summary>
	/// <remarks>The exact meaning depends on the connection's <see cref="SingleStoreConnectionStringBuilder.UseAffectedRows"/>
	/// setting. With the default (<c>UseAffectedRows=false</c>) this is the number of rows <em>matched</em> by the update —
	/// including rows that already held the new values — and therefore typically equals <see cref="RowsMatched"/>. With
	/// <c>UseAffectedRows=true</c> it is the number of rows whose values actually <em>changed</em>.</remarks>
	public int RowsAffected { get; }

	internal SingleStoreBulkUpdateResult(
		IReadOnlyList<SingleStoreError> warnings,
		int rowsStaged,
		int? rowsMatched,
		int rowsAffected)
	{
		Warnings = warnings;
		RowsStaged = rowsStaged;
		RowsMatched = rowsMatched;
		RowsAffected = rowsAffected;
	}
}
