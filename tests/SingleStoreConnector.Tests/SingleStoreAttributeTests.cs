#if BASELINE
using MySql.Data.MySqlClient;
#endif
using System;
using Xunit;

namespace SingleStoreConnector.Tests;

public class SingleStoreAttributeTests
{
	[Fact]
	public void Construct()
	{
		var attribute = new SingleStoreAttribute();
#if BASELINE
		Assert.Null(attribute.AttributeName);
#else
		Assert.Equal("", attribute.AttributeName);
#endif
		Assert.Null(attribute.Value);
	}

	[Fact]
	public void ConstructWithArguments()
	{
		var attribute = new SingleStoreAttribute("name", "value");
		Assert.Equal("name", attribute.AttributeName);
		Assert.Equal("value", attribute.Value);
	}

	[Fact]
	public void Clone()
	{
		var attribute = new SingleStoreAttribute("name", "value");
		var clone = attribute.Clone();
		Assert.NotSame(attribute, clone);
		Assert.Equal(attribute.AttributeName, clone.AttributeName);
		Assert.Equal(attribute.Value, clone.Value);
	}
}
