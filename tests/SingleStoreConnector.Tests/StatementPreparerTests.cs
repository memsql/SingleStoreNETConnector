using System.Globalization;
using System.IO;
using System.Text;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Tests;

public class StatementPreparerTests
{
	[Theory]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2\nAND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2  -- mycomment\nAND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 -- mycomment\nAND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 -- mycomment\n  AND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 /* mycomment */\n  AND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 /* mycomment */ AND column2 = @param")]
	public void Bug429(string sql)
	{
		var parameters = new SingleStoreParameterCollection();
		parameters.AddWithValue("@param", 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@param", "123"), parsedSql);
	}

	[Theory]
	[InlineData("UPDATE table SET a=a-@b;")]
	[InlineData("UPDATE table SET a=a-/* subtract b */@b;")]
	[InlineData("UPDATE table SET a=a+@b;")]
	[InlineData("UPDATE table SET a=a/@b;")]
	[InlineData("UPDATE table SET a=a-- \n-@b;")]
	[InlineData("UPDATE table SET a = a-@b;")]
	[InlineData("UPDATE table SET a = a+@b;")]
	[InlineData("UPDATE table SET a = a - @b;")]
	[InlineData("UPDATE table SET a=@b-a;")]
	[InlineData("UPDATE table SET a=@b+a;")]
	[InlineData("UPDATE table SET a = @b-a;")]
	[InlineData("UPDATE table SET a = @b - a;")]
	public void Bug563(string sql)
	{
		var parameters = new SingleStoreParameterCollection();
		parameters.AddWithValue("@b", 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@b", "123"), parsedSql);
	}

	[Theory]
	[InlineData(@"SELECT /* * / @param */ 1;")]
	[InlineData("SELECT # @param \n1;")]
	[InlineData("SELECT -- @param \n1;")]
	public void ParametersIgnoredInComments(string sql)
	{
		Assert.Equal(sql, GetParsedSql(sql));
	}

	[Theory]
	[InlineData(SingleStoreDbType.String, DummyEnum.FirstValue, "'FirstValue'")]
	[InlineData(SingleStoreDbType.VarChar, DummyEnum.FirstValue, "'FirstValue'")]
	[InlineData(null, DummyEnum.FirstValue, "0")]
	public void EnumParametersAreParsedCorrectly(SingleStoreDbType? type, object value, string replacedValue)
	{
		const string sql = "SELECT @param;";
		var parameters = new SingleStoreParameterCollection();
		var parameter = new SingleStoreParameter("@param", value);

		if (type is not null)
		{
			parameter.SingleStoreDbType = type.Value;
		}

		parameters.Add(parameter);

		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
	}

	[Theory]
	[InlineData("SELECT '@param';")]
	[InlineData("SELECT \"@param\";")]
	[InlineData("SELECT `@param`;")]
	[InlineData("SELECT 'test\\'@param';")]
	[InlineData("SELECT \"test\\\"@param\";")]
	[InlineData("SELECT 'test''@param';")]
	[InlineData("SELECT \"test\"\"@param\";")]
	[InlineData("SELECT `test``@param`;")]
	public void ParametersIgnoredInStrings(string sql)
	{
		Assert.Equal(sql, GetParsedSql(sql));
	}

	[Theory]
	[InlineData("SELECT @var;", "var")]
	[InlineData("SELECT @var;", "@var")]
	[InlineData("SELECT @var;", "@`var`")]
	[InlineData("SELECT @var;", "@'var'")]
	[InlineData("SELECT @`var`;", "var")]
	[InlineData("SELECT @`var`;", "@var")]
	[InlineData("SELECT @`var`;", "@`var`")]
	[InlineData("SELECT @`v``ar`;", "v`ar")]
	[InlineData("SELECT @`v``ar`;", "@`v``ar`")]
	[InlineData("SELECT @'var';", "var")]
	[InlineData("SELECT @'var';", "@var")]
	[InlineData("SELECT @'var';", "@'var'")]
	[InlineData("SELECT @'v''ar';", "v'ar")]
	[InlineData("SELECT @'v''ar';", "@'v''ar'")]
	[InlineData("SELECT @\"var\";", "var")]
	[InlineData("SELECT @\"var\";", "@var")]
	[InlineData("SELECT @\"var\";", "@\"var\"")]
	[InlineData("SELECT @\"v\"\"ar\";", "v\"ar")]
	[InlineData("SELECT @\"v\"\"ar\";", "@\"v\"\"ar\"")]
	public void QuotedParameters(string sql, string parameterName)
	{
		var parameters = new SingleStoreParameterCollection();
		parameters.AddWithValue(parameterName, 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal("SELECT 123;", parsedSql);
	}

	[Theory]
	[InlineData(@"SET @'var':=1;
SELECT @foo+@'var' as R")]
	[InlineData(@"SET @'var':=1;
SELECT @foo+1 as R")]
	[InlineData(@"SET @'var':=@foo+1;
SELECT @'var' as R")]
	public void Bug589(string sql)
	{
		var parameters = new SingleStoreParameterCollection();
		parameters.AddWithValue("@foo", 22);
		var parsedSql = GetParsedSql(sql, parameters, StatementPreparerOptions.AllowUserVariables);
		Assert.Equal(sql.Replace("@foo", "22"), parsedSql);
	}

	[Theory]
	[MemberData(nameof(FormatParameterData))]
	public void FormatParameter(object parameterValue, string replacedValue, bool noBackslashEscapes = false)
	{
		var parameters = new SingleStoreParameterCollection { new("@param", parameterValue) };
		const string sql = "SELECT @param;";
		var options = noBackslashEscapes ? StatementPreparerOptions.NoBackslashEscapes : StatementPreparerOptions.None;
		var parsedSql = GetParsedSql(sql, parameters, options);
		Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
	}

	public static IEnumerable<object[]> FormatParameterData =>
		new[]
		{
			new object[] { (byte) 200, "200" },
			new object[] { (sbyte) -100, "-100" },
			new object[] { (short) -12345, "-12345" },
			new object[] { (ushort) 45678, "45678" },
			new object[] { -1_234_567_890, "-1234567890" },
			new object[] { 3_456_789_012u, "3456789012" },
			new object[] { -12_345_678_901L, "-12345678901" },
			new object[] { 12_345_678_901UL, "12345678901" },
#if NET481
			new object[] { 1.0123456f, "1.01234555" },
#else
			new object[] { 1.0123456f, "1.0123456" },
#endif
			new object[] { 1.0123456789012346, "1.0123456789012346" },
			new object[] { 123456789.123456789m, "123456789.123456789" },
			new object[] { "1234", "'1234'" },
			new object[] { "it's", "'it''s'" },
			new object[] { "it's", "'it''s'", true },
			new object[] { 'a', "'a'" },
			new object[] { '\'', "''''" },
			new object[] { '\'', "''''", true },
			new object[] { '\\', "'\\\\'" },
			new object[] { '\\', "'\\'", true },
			new object[] { "\\'", "'\\\\'''" },
			new object[] { "\\'", "'\\'''", true },
			new object[] { 'ﬃ', "'ﬃ'" },
			new object[] { new DateTime(1234, 12, 23, 12, 34, 56, 789), "timestamp('1234-12-23 12:34:56.789000')" },
			new object[] { new DateTimeOffset(1234, 12, 23, 12, 34, 56, 789, TimeSpan.FromHours(2)), "timestamp('1234-12-23 10:34:56.789000')" },
			new object[] { new TimeSpan(2, 3, 4, 5, 6), "'51:04:05.006000'" },
			new object[] { new TimeSpan(-2, -3, -4, -5, -6), "'-51:04:05.006000'" },
			new object[] { new Guid("00112233-4455-6677-8899-AABBCCDDEEFF"), "'00112233-4455-6677-8899-aabbccddeeff'" },
			new object[] { new byte[] { 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62 }, @"_binary'AB''a\\b'" },
			new object[] { new byte[] { 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62 }, @"_binary'AB''a\b'", true },
			new object[] { new MemoryStream(new byte[] { 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62 }), @"_binary'AB''a\\b'" },
			new object[] { new MemoryStream(new byte[] { 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62 }), @"_binary'AB''a\b'", true },
			new object[] { new MemoryStream(new byte[] { 0, 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62, 0x63 }, 1, 6, false, true), @"_binary'AB''a\\b'" },
			new object[] { new MemoryStream(new byte[] { 0, 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62, 0x63 }, 1, 6, false, true), @"_binary'AB''a\b'", true },
			new object[] { "\"AB\\ab'".AsMemory(), @"'""AB\\ab'''" },
			new object[] { new StringBuilder("\"AB\\ab'"), @"'""AB\\ab'''" },
		};

	[Theory]
	[InlineData(StatementPreparerOptions.GuidFormatChar36, "'61626364-6566-6768-696a-6b6c6d6e6f70'")]
	[InlineData(StatementPreparerOptions.GuidFormatChar32, "'6162636465666768696a6b6c6d6e6f70'")]
	[InlineData(StatementPreparerOptions.GuidFormatBinary16, "_binary'abcdefghijklmnop'")]
	[InlineData(StatementPreparerOptions.GuidFormatTimeSwapBinary16, "_binary'ghefabcdijklmnop'")]
	[InlineData(StatementPreparerOptions.GuidFormatLittleEndianBinary16, "_binary'dcbafehgijklmnop'")]
	public void GuidFormat(object options, string replacedValue)
	{
		var parameters = new SingleStoreParameterCollection { new("@param", new Guid("61626364-6566-6768-696a-6b6c6d6e6f70")) };
		const string sql = "SELECT @param;";
		var parsedSql = GetParsedSql(sql, parameters, (StatementPreparerOptions) options);
		Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
	}

	[Theory]
	[InlineData("SELECT 1;", "SELECT 1;", false, true)]
	[InlineData("SELECT 1;", "SELECT 1;", true, true)]
	[InlineData("SELECT 1", "SELECT 1", false, true)]
	[InlineData("SELECT 1", "SELECT 1;", true, true)]
	[InlineData("SELECT 1 -- comment", "SELECT 1 -- comment\n", false, true)]
	[InlineData("SELECT 1 -- comment", "SELECT 1 -- comment\n;", true, true)]
	[InlineData("SELECT 1 # comment", "SELECT 1 # comment\n", false, true)]
	[InlineData("SELECT 1 # comment", "SELECT 1 # comment\n;", true, true)]
	[InlineData("SELECT '1", "SELECT '1", false, false)]
	[InlineData("SELECT '1", "SELECT '1", true, false)]
	[InlineData("SELECT '1' /* test", "SELECT '1' /* test", false, false)]
	[InlineData("SELECT '1';", "SELECT '1';", false, true)]
	[InlineData("SELECT '1';", "SELECT '1';", true, true)]
	[InlineData("SELECT '1'", "SELECT '1'", false, true)]
	[InlineData("SELECT '1'", "SELECT '1';", true, true)]
	[InlineData("SELECT \"1\";", "SELECT \"1\";", false, true)]
	[InlineData("SELECT \"1\";", "SELECT \"1\";", true, true)]
	[InlineData("SELECT \"1\"", "SELECT \"1\"", false, true)]
	[InlineData("SELECT \"1\"", "SELECT \"1\";", true, true)]
	[InlineData("SELECT * FROM `SELECT`;", "SELECT * FROM `SELECT`;", false, true)]
	[InlineData("SELECT * FROM `SELECT`", "SELECT * FROM `SELECT`", false, true)]
	[InlineData("SELECT * FROM test WHERE id = ?;", "SELECT * FROM test WHERE id = 0;", false, true)]
	[InlineData("SELECT * FROM test WHERE id = ?", "SELECT * FROM test WHERE id = 0", false, true)]
	public void CompleteStatements(string sql, string expectedSql, bool appendSemicolon, bool expectedComplete)
	{
		var parameters = new SingleStoreParameterCollection { new() { Value = 0 } };
		var preparer = new StatementPreparer(sql, parameters, appendSemicolon ? StatementPreparerOptions.AppendSemicolon : StatementPreparerOptions.None);
		var writer = new ByteBufferWriter();
		var isComplete = preparer.ParseAndBindParameters(writer);
		Assert.Equal(expectedComplete, isComplete);
		string parsedSql;
		using (var payload = writer.ToPayloadData())
			parsedSql = Encoding.UTF8.GetString(payload.Span);
		Assert.Equal(expectedSql, parsedSql);
	}

	[Theory]
	[InlineData("SELECT 1", new[] { "SELECT 1" }, "")]
	[InlineData("SELECT 1;", new[] { "SELECT 1" }, "")]
	[InlineData("\r\n-- leading comment\r\nSELECT 1;\r\n\r\n-- trailing comment", new[] { "SELECT 1" }, "")]
	[InlineData("SELECT 1; SELECT 2;", new[] { "SELECT 1", "SELECT 2" }, ";")]
	[InlineData("SELECT ?;", new[] { "SELECT ?" }, "0")]
	[InlineData("SELECT ?, ?;", new[] { "SELECT ?, ?" }, "0,1")]
	[InlineData("SELECT ?, ?; SELECT ?, ?;", new[] { "SELECT ?, ?", "SELECT ?, ?" }, "0,1;2,3")]
	[InlineData("SELECT @one, @two;", new[] { "SELECT ?, ?" }, "@one,@two")]
	[InlineData("SELECT @one, @two; SELECT @zero, @three", new[] { "SELECT ?, ?", "SELECT ?, ?" }, "@one,@two;@zero,@three")]
	[InlineData("SELECT ?, ?; SELECT ?, ?", new[] { "SELECT ?, ?", "SELECT ?, ?" }, "0,1;2,3")]
	[InlineData("SELECT '@one' FROM `@three` WHERE `@zero` = @two;", new[] { "SELECT '@one' FROM `@three` WHERE `@zero` = ?" }, "@two")]
	public void SplitStatement(string sql, string[] expectedStatements, string expectedStatementParametersString)
	{
		// verify InlineData is in the expected format
		var expectedStatementParameters = expectedStatementParametersString.Split(';');
		Assert.Equal(expectedStatements.Length, expectedStatementParameters.Length);

		// make some dummy parameters available to the test input
		var parameters = new SingleStoreParameterCollection
		{
			new("@zero", 0),
			new("@one", 0),
			new("@two", 0),
			new("@three", 0),
		};

		var preparer = new StatementPreparer(sql, parameters, StatementPreparerOptions.None);
		using var parsedStatements = preparer.SplitStatements();
		var splitStatements = parsedStatements.Statements;
		Assert.Equal(expectedStatements.Length, splitStatements.Count);
		for (var i = 0; i < splitStatements.Count; i++)
		{
			var parsedSql = Encoding.UTF8.GetString(splitStatements[i].StatementBytes.Slice(1));
			Assert.Equal(expectedStatements[i], parsedSql);

			var expectedParameterNamesOrIndexes = expectedStatementParameters[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var expectedParameterIndexes = new int[expectedParameterNamesOrIndexes.Length];
			var expectedParameterNames = new string[expectedParameterNamesOrIndexes.Length];
			for (var j = 0; j < expectedParameterNamesOrIndexes.Length; j++)
			{
				if (expectedParameterNamesOrIndexes[j][0] == '@')
				{
					expectedParameterNames[j] = expectedParameterNamesOrIndexes[j];
					expectedParameterIndexes[j] = -1;
				}
				else
				{
					expectedParameterIndexes[j] = int.Parse(expectedParameterNamesOrIndexes[j], CultureInfo.InvariantCulture);
				}
			}

			Assert.Equal(expectedParameterIndexes, splitStatements[i].ParameterIndexes);
			Assert.Equal(expectedParameterNames, splitStatements[i].ParameterNames);
		}
	}

	private static string GetParsedSql(string input, SingleStoreParameterCollection parameters = null, StatementPreparerOptions options = StatementPreparerOptions.None)
	{
		var preparer = new StatementPreparer(input, parameters ?? new SingleStoreParameterCollection(), options);
		var writer = new ByteBufferWriter();
		preparer.ParseAndBindParameters(writer);
		using var payload = writer.ToPayloadData();
		return Encoding.UTF8.GetString(payload.Span);
	}
}
