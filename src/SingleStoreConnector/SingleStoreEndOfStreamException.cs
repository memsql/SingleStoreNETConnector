namespace SingleStoreConnector;

[Serializable]
public sealed class SingleStoreEndOfStreamException : EndOfStreamException
{
	internal SingleStoreEndOfStreamException(int expectedByteCount, int readByteCount)
		: base("An incomplete response was received from the server")
	{
		ExpectedByteCount = expectedByteCount;
		ReadByteCount = readByteCount;
	}

	public int ExpectedByteCount { get; }
	public int ReadByteCount { get; }
}
