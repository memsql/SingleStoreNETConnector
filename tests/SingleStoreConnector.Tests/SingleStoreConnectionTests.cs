using System.Text;

namespace SingleStoreConnector.Tests;

public class SingleStoreConnectionTests
{
	[Theory]
	[InlineData(IsolationLevel.ReadCommitted, null, false, "\x38\0\0\0\x03set session transaction isolation level read committed;\x13\0\0\0\x03start transaction;")]
	[InlineData(IsolationLevel.ReadCommitted, null, true, "\x3A\0\0\0\x03\0\x01set session transaction isolation level read committed;\x15\0\0\0\x03\0\x01start transaction;")]
	[InlineData(IsolationLevel.ReadCommitted, false, false, "\x38\0\0\0\x03set session transaction isolation level read committed;\x1E\0\0\0\x03start transaction read write;")]
	[InlineData(IsolationLevel.ReadCommitted, false, true, "\x3A\0\0\0\x03\0\x01set session transaction isolation level read committed;\x20\0\0\0\x03\0\x01start transaction read write;")]
	[InlineData(IsolationLevel.ReadCommitted, true, false, "\x38\0\0\0\x03set session transaction isolation level read committed;\x1D\0\0\0\x03start transaction read only;")]
	[InlineData(IsolationLevel.ReadCommitted, true, true, "\x3A\0\0\0\x03\0\x01set session transaction isolation level read committed;\x1F\0\0\0\x03\0\x01start transaction read only;")]
	public void GetStartTransactionPayload(IsolationLevel isolationLevel, bool? isReadOnly, bool supportsQueryAttributes, string expected)
	{
		var payload = SingleStoreConnection.GetStartTransactionPayload(isolationLevel, isReadOnly, supportsQueryAttributes);
		Assert.Equal(expected, Encoding.ASCII.GetString(payload.Span.ToArray()));
	}
}
