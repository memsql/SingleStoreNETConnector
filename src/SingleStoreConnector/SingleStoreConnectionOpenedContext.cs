namespace SingleStoreConnector;

/// <summary>
/// Contains information passed to <see cref="SingleStoreConnectionOpenedCallback"/> when a new <see cref="SingleStoreConnection"/> is opened.
/// </summary>
public sealed class SingleStoreConnectionOpenedContext
{
	/// <summary>
	/// The <see cref="SingleStoreConnection"/> that was opened.
	/// </summary>
	public SingleStoreConnection Connection { get; }

	/// <summary>
	/// Bitflags giving the conditions under which a connection was opened.
	/// </summary>
	public SingleStoreConnectionOpenedConditions Conditions { get; }

	internal SingleStoreConnectionOpenedContext(SingleStoreConnection connection, SingleStoreConnectionOpenedConditions conditions)
	{
		Connection = connection;
		Conditions = conditions;
	}
}
