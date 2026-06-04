namespace SingleStoreConnector;

public sealed class SingleStoreRowsStagedEventArgs : EventArgs
{
	/// <summary>
	/// Gets or sets a value indicating whether the bulk update operation should be aborted.
	/// </summary>
	public bool Abort { get; set; }

	/// <summary>
	/// Gets a value that returns the number of rows staged during the current bulk update operation.
	/// </summary>
	public int RowsStaged { get; internal set; }

	internal SingleStoreRowsStagedEventArgs()
	{
	}
}

/*/// <summary>
/// Represents the method that handles the <see cref="SingleStoreBulkUpdate.SingleStoreRowsStaged"/> event of a <see cref="SingleStoreBulkUpdate"/>.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public delegate void SingleStoreRowsStagedEventHandler(object sender, SingleStoreRowsStagedEventArgs e);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix*/
