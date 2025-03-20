using System.Text;

namespace SingleStoreConnector.Tests;

public class ServerVersionTests
{
	[Fact]
	public void Empty()
	{
		var empty = ServerVersion.Empty;
		Assert.Equal("", empty.OriginalString);
		Assert.Equal(new Version(0, 0), empty.Version);
	}

	[Theory]
	[InlineData("5.7.21-log", "5.7.21")]
	[InlineData("8.0.13", "8.0.13")]
	[InlineData("5.7.25-28", "5.7.25")]
	[InlineData("5.7.25-", "5.7.25")]
	[InlineData("5.7.25-10.2.3", "5.7.25")]
	[InlineData("a.b.c", "0.0.0")]
	[InlineData("1", "1.0.0")]
	[InlineData("1.", "1.0.0")]
	[InlineData("1.2", "1.2.0")]
	[InlineData("1.2.", "1.2.0")]
	[InlineData("1.2.3", "1.2.3")]
	[InlineData("1.2.3.", "1.2.3")]
	[InlineData("1.2.3-", "1.2.3")]
	public void ParseServerVersion(string input, string expectedString)
	{
		var serverVersion = new ServerVersion(Encoding.UTF8.GetBytes(input));
		var expected = Version.Parse(expectedString);
		Assert.Equal(expected, serverVersion.Version);
	}
}
