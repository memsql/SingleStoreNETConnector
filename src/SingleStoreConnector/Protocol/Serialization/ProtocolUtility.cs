using System.Buffers;
using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.Protocol.Serialization;

internal static class ProtocolUtility
{
	public static int GetBytesPerCharacter(CharacterSet characterSet)
	{
		switch (characterSet)
		{
			case CharacterSet.Binary:
				return 1;

			case CharacterSet.Utf8GeneralCaseInsensitive:
			case CharacterSet.Utf8Binary:
			case CharacterSet.Utf8UnicodeCaseInsensitive:
			case CharacterSet.Utf8IcelandicCaseInsensitive:
			case CharacterSet.Utf8LatvianCaseInsensitive:
			case CharacterSet.Utf8RomanianCaseInsensitive:
			case CharacterSet.Utf8SlovenianCaseInsensitive:
			case CharacterSet.Utf8PolishCaseInsensitive:
			case CharacterSet.Utf8EstonianCaseInsensitive:
			case CharacterSet.Utf8SpanishCaseInsensitive:
			case CharacterSet.Utf8SwedishCaseInsensitive:
			case CharacterSet.Utf8TurkishCaseInsensitive:
			case CharacterSet.Utf8CzechCaseInsensitive:
			case CharacterSet.Utf8DanishCaseInsensitive:
			case CharacterSet.Utf8LithuanianCaseInsensitive:
			case CharacterSet.Utf8SlovakCaseInsensitive:
			case CharacterSet.Utf8Spanish2CaseInsensitive:
			case CharacterSet.Utf8RomanCaseInsensitive:
			case CharacterSet.Utf8PersianCaseInsensitive:
			case CharacterSet.Utf8EsperantoCaseInsensitive:
			case CharacterSet.Utf8HungarianCaseInsensitive:
			case CharacterSet.Utf8SinhalaCaseInsensitive:
				return 3;

			case CharacterSet.Utf8Mb4GeneralCaseInsensitive:
			case CharacterSet.Utf8Mb4Binary:
			case CharacterSet.Utf8Mb4UnicodeCaseInsensitive:
			case CharacterSet.Utf8Mb4IcelandicCaseInsensitive:
			case CharacterSet.Utf8Mb4LatvianCaseInsensitive:
			case CharacterSet.Utf8Mb4RomanianCaseInsensitive:
			case CharacterSet.Utf8Mb4SlovenianCaseInsensitive:
			case CharacterSet.Utf8Mb4PolishCaseInsensitive:
			case CharacterSet.Utf8Mb4EstonianCaseInsensitive:
			case CharacterSet.Utf8Mb4SpanishCaseInsensitive:
			case CharacterSet.Utf8Mb4SwedishCaseInsensitive:
			case CharacterSet.Utf8Mb4TurkishCaseInsensitive:
			case CharacterSet.Utf8Mb4CzechCaseInsensitive:
			case CharacterSet.Utf8Mb4DanishCaseInsensitive:
			case CharacterSet.Utf8Mb4LithuanianCaseInsensitive:
			case CharacterSet.Utf8Mb4SlovakCaseInsensitive:
			case CharacterSet.Utf8Mb4Spanish2CaseInsensitive:
			case CharacterSet.Utf8Mb4RomanCaseInsensitive:
			case CharacterSet.Utf8Mb4PersianCaseInsensitive:
			case CharacterSet.Utf8Mb4EsperantoCaseInsensitive:
			case CharacterSet.Utf8Mb4HungarianCaseInsensitive:
			case CharacterSet.Utf8Mb4SinhalaCaseInsensitive:
				return 4;

			default:
				throw new NotSupportedException($"Maximum byte length of character set {characterSet} is unknown.");
		}
	}

	public static async ValueTask<ArraySegment<byte>> ReadPayloadAsync(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegmentHolder<byte> previousPayloads, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
	{
		previousPayloads.Clear();
		while (true)
		{
			// read the packet header
			var headerBytes = await bufferedByteReader.ReadBytesAsync(byteHandler, 4, ioBehavior).ConfigureAwait(false);
			if (headerBytes.Count < 4)
			{
				return protocolErrorBehavior == ProtocolErrorBehavior.Ignore ? default :
					throw new SingleStoreEndOfStreamException(4, headerBytes.Count);
			}

			// read values from the header before the memory is potentially overwritten by ReadBytesAsync
			var payloadLength = (int) SerializationUtility.ReadUInt32(headerBytes.AsSpan()[..3]);
			int packetSequenceNumber = headerBytes.AsSpan()[3];
			var expectedSequenceNumber = getNextSequenceNumber() % 256;

			// read the packet payload
			var payloadBytes = await bufferedByteReader.ReadBytesAsync(byteHandler, payloadLength, ioBehavior).ConfigureAwait(false);

			Packet packet;
			if (expectedSequenceNumber != -1 && packetSequenceNumber != expectedSequenceNumber)
			{
				if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
					packet = default;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
				else if (payloadBytes is [ErrorPayload.Signature, ..])
#else
				else if (payloadBytes.Count > 0 && payloadBytes.AsSpan()[0] == ErrorPayload.Signature)
#endif
					packet = new(payloadBytes);
				else
					throw SingleStoreProtocolException.CreateForPacketOutOfOrder(expectedSequenceNumber, packetSequenceNumber);
			}
			else
			{
				packet = payloadBytes.Count >= payloadLength ? new(payloadBytes) :
					protocolErrorBehavior == ProtocolErrorBehavior.Throw ? throw new SingleStoreEndOfStreamException(payloadLength, payloadBytes.Count) :
					default;
			}

			// if this is a complete packet, return it
			if (previousPayloads.Count == 0 && packet.Contents.Count < MaxPacketSize)
				return packet.Contents;

			// resize the buffer of previous payloads if necessary, then append this payload to it
			var previousPayloadsArray = previousPayloads.Array;
			if (previousPayloadsArray is null)
				previousPayloadsArray = new byte[ProtocolUtility.MaxPacketSize + 1];
			else if (previousPayloads.Offset + previousPayloads.Count + packet.Contents.Count > previousPayloadsArray.Length)
				Array.Resize(ref previousPayloadsArray, previousPayloadsArray.Length * 2);

			packet.Contents.AsSpan().CopyTo(previousPayloadsArray.AsSpan(previousPayloads.Offset + previousPayloads.Count));
			previousPayloads.ArraySegment = new(previousPayloadsArray, previousPayloads.Offset, previousPayloads.Count + packet.Contents.Count);

			if (packet.Contents.Count < ProtocolUtility.MaxPacketSize)
				return previousPayloads.ArraySegment;
		}
	}

	public static async ValueTask WritePayloadAsync(IByteHandler byteHandler, Func<int> getNextSequenceNumber, ReadOnlyMemory<byte> payload, IOBehavior ioBehavior)
	{
		var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(MaxPacketSize, payload.Length) + 4);
		try
		{
			var bytesSent = 0;
			do
			{
				var contents = payload.Slice(bytesSent, Math.Min(MaxPacketSize, payload.Length - bytesSent));
				var bufferLength = contents.Length + 4;

				SerializationUtility.WriteUInt32((uint) contents.Length, buffer, 0, 3);
				buffer[3] = (byte) getNextSequenceNumber();
				contents.CopyTo(buffer.AsMemory(4));

				await byteHandler.WriteBytesAsync(new ArraySegment<byte>(buffer, 0, bufferLength), ioBehavior).ConfigureAwait(false);
				bytesSent += contents.Length;
			}
			while (bytesSent < payload.Length);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	public const int MaxPacketSize = 16777215;
}
