using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Tests;

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
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteIdentifier(input));

	[Fact]
	public void QuoteIdentifier_NullChar_Throws()
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteIdentifier("table\0name"));

	[Theory]
	[InlineData("db.users", "`db`.`users`")]
	[InlineData("my db.my table", "`my db`.`my table`")]
	[InlineData("select.order", "`select`.`order`")] // Reserved words
	[InlineData("db.my-table", "`db`.`my-table`")] // Hyphen
	[InlineData("db.my table", "`db`.`my table`")] // Space
	[InlineData("db.用户表", "`db`.`用户表`")] // Unicode
	[InlineData("db.таблиця", "`db`.`таблиця`")] // Unicode
	public void QuoteQualifiedIdentifier_ValidInput_ReturnsQuoted(string input, string expected)
	{
		var result = IdentifierHelper.QuoteQualifiedIdentifier(input);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("`db`.`users`", "`db`.`users`")]
	[InlineData("`my db`.`my table`", "`my db`.`my table`")]
	[InlineData("`db.with.dot`.`users`", "`db.with.dot`.`users`")]
	[InlineData("`db`.`table.with.dot`", "`db`.`table.with.dot`")]
	[InlineData("`db.with.dot`.`table.with.dot`", "`db.with.dot`.`table.with.dot`")]
	[InlineData("db.`table.with.dot`", "`db`.`table.with.dot`")]
	[InlineData("`db.with.dot`.users", "`db.with.dot`.`users`")]
	public void QuoteQualifiedIdentifier_AlreadyQuotedInput_ReturnsQuoted(string input, string expected)
	{
		var result = IdentifierHelper.QuoteQualifiedIdentifier(input);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData("db.my`table", "`db`.`my``table`")]
	[InlineData("my`db.my`table", "`my``db`.`my``table`")]
	[InlineData("`my``db`.`my``table`", "`my``db`.`my``table`")]
	public void QuoteQualifiedIdentifier_BackticksInsideIdentifiers_ReturnsQuoted(string input, string expected)
	{
		var result = IdentifierHelper.QuoteQualifiedIdentifier(input);

		Assert.Equal(expected, result);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void QuoteQualifiedIdentifier_NullOrEmpty_Throws(string input)
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteQualifiedIdentifier(input));

	[Fact]
	public void QuoteQualifiedIdentifier_NullChar_Throws()
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteQualifiedIdentifier("db.table\0name"));

	[Theory]
	[InlineData(".users")]
	[InlineData("db.")]
	[InlineData("db..users")]
	[InlineData("db. .users")]
	public void QuoteQualifiedIdentifier_EmptyPart_Throws(string input)
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteQualifiedIdentifier(input));

	[Theory]
	[InlineData("`db.users")]
	[InlineData("db.`users")]
	[InlineData("`db`.`users")]
	public void QuoteQualifiedIdentifier_UnterminatedQuotedIdentifier_Throws(string input)
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteQualifiedIdentifier(input));

	[Theory]
	[InlineData("`db`extra.users")]
	[InlineData("db.`users`extra")]
	public void QuoteQualifiedIdentifier_UnexpectedCharactersAfterQuotedIdentifier_Throws(string input)
		=> Assert.Throws<ArgumentException>(() => IdentifierHelper.QuoteQualifiedIdentifier(input));
}
