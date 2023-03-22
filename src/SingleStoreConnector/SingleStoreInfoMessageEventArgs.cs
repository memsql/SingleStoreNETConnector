namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreInfoMessageEventArgs"/> contains the data supplied to the <see cref="SingleStoreInfoMessageEventHandler"/> event handler.
/// </summary>
public sealed class SingleStoreInfoMessageEventArgs : EventArgs
{
	internal SingleStoreInfoMessageEventArgs(IReadOnlyList<SingleStoreError> errors) => Errors = errors;

	/// <summary>
	/// The list of errors being reported.
	/// </summary>
	public IReadOnlyList<SingleStoreError> Errors { get; }
}

/// <summary>
/// Defines the event handler for <see cref="SingleStoreConnection.InfoMessage"/>.
/// </summary>
/// <param name="sender">The sender. This is the associated <see cref="SingleStoreConnection"/>.</param>
/// <param name="args">The <see cref="SingleStoreInfoMessageEventArgs"/> containing the errors.</param>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public delegate void SingleStoreInfoMessageEventHandler(object sender, SingleStoreInfoMessageEventArgs args);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
