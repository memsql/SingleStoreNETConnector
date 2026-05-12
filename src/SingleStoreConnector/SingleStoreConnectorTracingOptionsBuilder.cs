namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreConnectorTracingOptionsBuilder"/> provides an API for configuring OpenTelemetry tracing options.
/// </summary>
public sealed class SingleStoreConnectorTracingOptionsBuilder
{
	/// <summary>
	/// Gets or sets a value indicating whether to enable the "read-result-set-header" event.
	/// Default is false; set to true to opt in to this event.
	/// </summary>
	public SingleStoreConnectorTracingOptionsBuilder EnableResultSetHeaderEvent(bool enable = true)
	{
		m_enableResultSetHeaderEvent = enable;
		return this;
	}

	internal SingleStoreConnectorTracingOptions Build() =>
		new()
		{
			EnableResultSetHeaderEvent = m_enableResultSetHeaderEvent,
		};

	private bool m_enableResultSetHeaderEvent = SingleStoreConnectorTracingOptions.Default.EnableResultSetHeaderEvent;
}
