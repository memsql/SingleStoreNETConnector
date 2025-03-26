using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Protocol.Serialization;

internal sealed class StandardPayloadHandler : IPayloadHandler
{
	public StandardPayloadHandler(IByteHandler byteHandler)
	{
		ByteHandler = byteHandler;
		m_getNextSequenceNumber = () => m_sequenceNumber++;
	}

	public void Dispose()
	{
		m_bufferedByteReader = null;
		Utility.Dispose(ref m_byteHandler);
	}

	public void StartNewConversation() =>
		m_sequenceNumber = 0;

	public void SetNextSequenceNumber(int sequenceNumber) =>
		m_sequenceNumber = (byte) sequenceNumber;

	public IByteHandler ByteHandler
	{
		get
		{
#if NET7_0_OR_GREATER
			ObjectDisposedException.ThrowIf(m_byteHandler is null, this);
#else
			if (m_byteHandler is null)
				throw new ObjectDisposedException(nameof(StandardPayloadHandler));
#endif
			return m_byteHandler;
		}
		set
		{
			var oldByteHandler = m_byteHandler;
#if NET6_0_OR_GREATER
			ArgumentNullException.ThrowIfNull(value);
#else
			if (value is null)
				throw new ArgumentNullException(nameof(value));
#endif
			m_byteHandler = value;
			oldByteHandler?.Dispose();
			m_bufferedByteReader = new();
		}
	}

	public ValueTask<ArraySegment<byte>> ReadPayloadAsync(ArraySegmentHolder<byte> cache, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior) =>
		ProtocolUtility.ReadPayloadAsync(m_bufferedByteReader!, m_byteHandler!, m_getNextSequenceNumber, cache, protocolErrorBehavior, ioBehavior);

	public ValueTask WritePayloadAsync(ReadOnlyMemory<byte> payload, IOBehavior ioBehavior) =>
		ProtocolUtility.WritePayloadAsync(m_byteHandler!, m_getNextSequenceNumber, payload, ioBehavior);

	private readonly Func<int> m_getNextSequenceNumber;
	private IByteHandler? m_byteHandler;
	private BufferedByteReader? m_bufferedByteReader;
	private byte m_sequenceNumber;
}
