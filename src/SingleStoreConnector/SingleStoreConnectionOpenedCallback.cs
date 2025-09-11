namespace SingleStoreConnector;

/// <summary>
/// A callback that is invoked when a new <see cref="SingleStoreConnection"/> is opened.
/// </summary>
/// <param name="context">A <see cref="SingleStoreConnectionOpenedContext"/> giving information about the connection being opened.</param>
/// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.</param>
/// <returns>A <see cref="ValueTask"/> representing the result of the possibly-asynchronous operation.</returns>
public delegate ValueTask SingleStoreConnectionOpenedCallback(SingleStoreConnectionOpenedContext context, CancellationToken cancellationToken);
