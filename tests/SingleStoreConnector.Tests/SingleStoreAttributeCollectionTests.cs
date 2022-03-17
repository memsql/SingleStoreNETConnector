#if BASELINE
using MySql.Data.MySqlClient;
#endif
using System;
using Xunit;

namespace SingleStoreConnector.Tests;

public class SingleStoreAttributeCollectionTests
{
	public SingleStoreAttributeCollectionTests()
	{
		m_collection = new SingleStoreCommand().Attributes;
		m_attribute = new SingleStoreAttribute("name", "value");
	}

	[Fact]
	public void EmptyCount()
	{
		AssertEmpty();
	}

	[Fact]
	public void CountAfterAdd()
	{
		AddAttribute();
		AssertSingle();
	}

	[Fact]
	public void ClearAfterAdd()
	{
		AddAttribute();
		m_collection.Clear();
		AssertEmpty();
	}

	[Fact]
	public void GetAtIndex()
	{
		AddAttribute();
		Assert.Same(m_attribute, m_collection[0]);
	}

	[Fact]
	public void SetAttribute()
	{
		m_collection.SetAttribute("name2", "value2");
		var attribute = AssertSingle();
		Assert.Equal("name2", attribute.AttributeName);
		Assert.Equal("value2", attribute.Value);
	}

#if !BASELINE
	[Fact]
	public void SetAttributeTwice()
	{
		m_collection.SetAttribute("name2", "value2");
		m_collection.SetAttribute("name2", "value3");
		var attribute = AssertSingle();
		Assert.Equal("name2", attribute.AttributeName);
		Assert.Equal("value3", attribute.Value);
	}
#endif

	private void AddAttribute()
	{
#if BASELINE
		m_collection.SetAttribute(m_attribute);
#else
		m_collection.Add(m_attribute);
#endif
	}

	private void AssertEmpty()
	{
#if BASELINE
		Assert.Equal(0, m_collection.Count);
#else
		Assert.Empty(m_collection);
#endif
	}

	private SingleStoreAttribute AssertSingle()
	{
#if BASELINE
		Assert.Equal(1, m_collection.Count);
		return m_collection[0];
#else
		return (SingleStoreAttribute) Assert.Single(m_collection);
#endif
	}

	private readonly SingleStoreAttributeCollection m_collection;
	private readonly SingleStoreAttribute m_attribute;
}
