#if BASELINE
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace SingleStoreConnector.Tests;

public class SingleStoreHelperTests
{
	[Theory]
	[InlineData("", "")]
	[InlineData("test", "test")]
	[InlineData("\"", "\\\"")]
	[InlineData(@"'", @"\'")]
	[InlineData(@"\", @"\\")]
	[InlineData(@"''", @"\'\'")]
	[InlineData(@"'begin", @"\'begin")]
	[InlineData(@"end'", @"end\'")]
	[InlineData(@"mid'dle", @"mid\'dle")]
	[InlineData(@"doub''led", @"doub\'\'led")]
	[InlineData(@"'a'b'", @"\'a\'b\'")]
	public void EscapeString(string input, string expected)
	{
		var actual = SingleStoreHelper.EscapeString(input);
		Assert.Equal(expected, actual);
		if (expected == input)
			Assert.Same(input, actual);
	}
}
