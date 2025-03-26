using System.Runtime.Serialization;

namespace SingleStoreConnector;

/// <summary>
/// <see cref="SingleStoreProtocolException"/> is thrown when there is an internal protocol error communicating with SingleStore Server.
/// </summary>
[Serializable]
public sealed class SingleStoreProtocolException : InvalidOperationException
{
	/// <summary>
	/// Creates a new <see cref="SingleStoreProtocolException"/> for an out-of-order packet.
	/// </summary>
	/// <param name="expectedSequenceNumber">The expected packet sequence number.</param>
	/// <param name="packetSequenceNumber">The actual packet sequence number.</param>
	/// <returns>A new <see cref="SingleStoreProtocolException"/>.</returns>
	internal static SingleStoreProtocolException CreateForPacketOutOfOrder(int expectedSequenceNumber, int packetSequenceNumber) =>
		new SingleStoreProtocolException($"Packet received out-of-order. Expected {expectedSequenceNumber:d}; got {packetSequenceNumber:d}.");

#if NET8_0_OR_GREATER
	[Obsolete(DiagnosticId = "SYSLIB0051")]
#endif
	private SingleStoreProtocolException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	private SingleStoreProtocolException(string message)
		: base(message)
	{
	}
}
