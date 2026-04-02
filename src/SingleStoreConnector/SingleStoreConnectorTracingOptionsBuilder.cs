namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreConnectorTracingOptionsBuilder"/> provides an API for configuring OpenTelemetry tracing options.
/// </summary>
public sealed class SingleStoreConnectorTracingOptionsBuilder
{
	/// <summary>
	/// Gets or sets a value indicating whether to enable the "time-to-first-read" event.
	/// Default is true to preserve existing behavior.
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
