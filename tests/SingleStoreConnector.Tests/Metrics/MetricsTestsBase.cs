#nullable enable

using System.Diagnostics.Metrics;

namespace SingleStoreConnector.Tests.Metrics;

public abstract class MetricsTestsBase : IDisposable
{
	public MetricsTestsBase()
	{
		m_measurements = new();
		m_timeMeasurements = new();

		Server = new FakeSingleStoreServer();
		Server.Start();

		m_meterListener = new MeterListener
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (instrument.Meter.Name == "SingleStoreConnector")
					listener.EnableMeasurementEvents(instrument);
			}
		};
		m_meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
		m_meterListener.SetMeasurementEventCallback<float>(OnMeasurementRecorded);
		m_meterListener.Start();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			m_meterListener.Dispose();
			Server.Stop();
		}
	}

	protected string? PoolName { get; set; }
	protected FakeSingleStoreServer Server { get; }

	protected SingleStoreConnectionStringBuilder CreateConnectionStringBuilder() =>
		new SingleStoreConnectionStringBuilder
		{
			Server = "localhost",
			Port = (uint) Server.Port,
			UserID = "test",
			Password = "test",
		};

	protected void AssertMeasurement(string name, int expected)
	{
		// clear cached measurements from observable counters
		lock (m_measurements)
		{
			m_measurements.Remove("db.client.connections.idle.max");
			m_measurements.Remove("db.client.connections.idle.min");
			m_measurements.Remove("db.client.connections.max");
		}
		m_meterListener.RecordObservableInstruments();
		lock (m_measurements)
			Assert.Equal(expected, m_measurements.GetValueOrDefault(name));
	}

	protected List<float> GetAndClearMeasurements(string name)
	{
		if (!m_timeMeasurements.TryGetValue(name, out var list))
			list = new List<float>();
		m_timeMeasurements[name] = new List<float>();
		return list;
	}

#if NO_METRICS_TESTS
	protected const string MetricsSkip = "Metrics tests are skipped";
#else
	protected const string MetricsSkip = null;
#endif

	private void OnMeasurementRecorded(Instrument instrument, int measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
	{
		var (poolName, stateTag) = GetTags(tags);
		if (poolName != PoolName)
			return;

		lock (m_measurements)
		{
			m_measurements[instrument.Name] = m_measurements.GetValueOrDefault(instrument.Name) + measurement;
			if (stateTag.Length != 0)
				m_measurements[$"{instrument.Name}|{stateTag}"] = m_measurements.GetValueOrDefault($"{instrument.Name}|{stateTag}") + measurement;
		}
	}

	private void OnMeasurementRecorded(Instrument instrument, float measurement, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
	{
		var (poolName, stateTag) = GetTags(tags);
		if (poolName != PoolName)
			return;

		lock (m_timeMeasurements)
		{
			if (!m_timeMeasurements.TryGetValue(instrument.Name, out var list))
				list = m_timeMeasurements[instrument.Name] = new List<float>();
			list.Add(measurement);
		}
	}

	private (string PoolName, string State) GetTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
	{
		var poolName = "";
		var state = "";
		foreach (var tag in tags)
		{
			if (tag.Key == "pool.name" && tag.Value is string s1)
				poolName = s1;
			else if (tag.Key == "state" && tag.Value is string s2)
				state = s2;
		}
		return (poolName, state);
	}


	private readonly Dictionary<string, int> m_measurements;
	private readonly Dictionary<string, List<float>> m_timeMeasurements;
	private readonly MeterListener m_meterListener;
}
