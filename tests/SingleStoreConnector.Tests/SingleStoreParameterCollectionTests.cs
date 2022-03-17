#if BASELINE
using MySql.Data.MySqlClient;
#endif
using System;
using Xunit;

namespace SingleStoreConnector.Tests;

public class SingleStoreParameterCollectionTests
{
	public SingleStoreParameterCollectionTests()
	{
		m_collection = new SingleStoreCommand().Parameters;
	}

	[Fact]
	public void InsertAtNegative() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.Insert(-1, new SingleStoreParameter()));

	[Fact]
	public void InsertPastEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.Insert(1, new SingleStoreParameter()));

	[Fact]
	public void RemoveAtNegative() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.RemoveAt(-1));

	[Fact]
	public void RemoveAtEnd() => Assert.Throws<ArgumentOutOfRangeException>(() => m_collection.RemoveAt(0));

	SingleStoreParameterCollection m_collection;
}
