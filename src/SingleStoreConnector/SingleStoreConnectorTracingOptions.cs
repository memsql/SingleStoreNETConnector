namespace SingleStoreConnector;

internal sealed class SingleStoreConnectorTracingOptions
{
	public bool EnableResultSetHeaderEvent { get; set; }

	public static SingleStoreConnectorTracingOptions Default { get; } = new();
}
