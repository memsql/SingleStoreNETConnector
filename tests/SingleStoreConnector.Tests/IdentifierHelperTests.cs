using System;
using SingleStoreConnector.Utilities;
using Xunit;

namespace SingleStoreConnector.Tests
{
	public class IdentifierHelperTests
	{
		[Theory]
		[InlineData("users", "`users`")]
		[InlineData("user_id", "`user_id`")]
		[InlineData("select", "`select`")] // Reserved word
		[InlineData("order", "`order`")] // Reserved word
		[InlineData("my table", "`my table`")] // Space
		[InlineData("my-table", "`my-table`")] // Hyphen
		[InlineData("my`table", "`my``table`")] // Backtick inside
		[InlineData("用户表", "`用户表`")] // Unicode
		[InlineData("таблиця", "`таблиця`")] // Unicode
		public void QuoteIdentifier_ValidInput_ReturnsQuoted(string input, string expected)
		{
			var result = IdentifierHelper.QuoteIdentifier(input);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void QuoteIdentifier_NullOrEmpty_Throws(string input)
		{
			Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteIdentifier(input));
		}

		[Fact]
		public void QuoteIdentifier_NullChar_Throws()
		{
			Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteIdentifier("table\0name"));
		}

		[Theory]
		[InlineData("db.users", "`db`.`users`")]
		[InlineData("my db.my table", "`my db`.`my table`")]
		public void QuoteQualifiedIdentifier_ValidInput_ReturnsQuoted(string input, string expected)
		{
			var result = IdentifierHelper.QuoteQualifiedIdentifier(input);
			Assert.Equal(expected, result);
		}
	}
}
