#if BASELINE
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace SingleStoreConnector.Tests;

public class SingleStoreExceptionTests
{
	[Fact]
	public void Data()
	{
		var exception = new SingleStoreException(SingleStoreErrorCode.No, "two", "three");
		Assert.Equal(1002, exception.Data["Server Error Code"]);
		Assert.Equal("two", exception.Data["SqlState"]);
	}
}
