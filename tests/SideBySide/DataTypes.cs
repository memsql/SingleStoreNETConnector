using System.Globalization;
using SingleStoreConnector.Core;
#if BASELINE
using MySql.Data.Types;
#endif

// mysql-connector-net will throw SqlNullValueException, which is an exception type related to SQL Server:
// "The exception that is thrown when the Value property of a System.Data.SqlTypes structure is set to null."
// However, DbDataReader.GetString etc. are documented as throwing InvalidCastException: https://msdn.microsoft.com/en-us/library/system.data.common.dbdatareader.getstring.aspx
// Additionally, that is what DbDataReader.GetFieldValue<T> throws. For consistency, we prefer InvalidCastException.
#if BASELINE
using GetValueWhenNullException = System.Data.SqlTypes.SqlNullValueException;
using GetGuidWhenNullException = MySql.Data.MySqlClient.SingleStoreException;
using GetBytesWhenNullException = System.NullReferenceException;
using GetGeographyWhenNullException = System.Exception;
#else
using GetValueWhenNullException = System.InvalidCastException;
using GetGuidWhenNullException = System.InvalidCastException;
using GetBytesWhenNullException = System.InvalidCastException;
using GetGeographyWhenNullException = System.InvalidCastException;
#endif

namespace SideBySide;

public sealed class DataTypes : IClassFixture<DataTypesFixture>, IDisposable
{
	public DataTypes(DataTypesFixture database)
	{
		Connection = new SingleStoreConnection(CreateConnectionStringBuilder().ConnectionString);
		Connection.Open();
	}

	public void Dispose() => Connection.Dispose();

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
	[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
	[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
	[InlineData("Int24", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
	[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
	[InlineData("Int32", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
	[InlineData("UInt32", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
	[InlineData("Int64", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
	[InlineData("UInt64", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
	public async Task GetBoolean(string column, int[] flags, bool[] values)
	{
		await DoGetValue(column, (r, n) => r.GetBoolean(n), (r, s) => r.GetBoolean(s), flags, values).ConfigureAwait(false);
	}

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new short[] { 0, 0, -128, 127, 123 })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new short[] { 0, 0, 0, 255, 123 })]
	[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new short[] { 0, 0, -32768, 32767, 12345 })]
	[InlineData("UInt16", new[] { 1, 0, 0, 2, 0 }, new short[] { 0, 0, 0, 0, 12345 })]
	[InlineData("Int24", new[] { 1, 0, 2, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt24", new[] { 1, 0, 0, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
	[InlineData("Int32", new[] { 1, 0, 2, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt32", new[] { 1, 0, 0, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
	[InlineData("Int64", new[] { 1, 0, 2, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt64", new[] { 1, 0, 0, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
	public async Task GetInt16(string column, int[] flags, short[] values)
	{
		await DoGetValue(column, (r, n) => r.GetInt16(n), (r, s) => r.GetInt16(s), flags, values).ConfigureAwait(false);
	}

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -128, 127, 123 })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, 0, 255, 123 })]
	[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -32768, 32767, 12345 })]
	[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, 0, 65535, 12345 })]
	[InlineData("Int24", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -8388608, 8388607, 1234567 })]
	[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, 0, 16777215, 1234567 })]
	[InlineData("Int32", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -2147483648, 2147483647, 123456789 })]
	[InlineData("UInt32", new[] { 1, 0, 0, 2, 0 }, new[] { 0, 0, 0, 0, 123456789 })]
	[InlineData("Int64", new[] { 1, 0, 2, 2, 2 }, new[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt64", new[] { 1, 0, 0, 2, 2 }, new[] { 0, 0, 0, 0, 0 })]
	public async Task GetInt32(string column, int[] flags, int[] values)
	{
		await DoGetValue(column, (r, n) => r.GetInt32(n), (r, s) => r.GetInt32(s), flags, values).ConfigureAwait(false);
	}

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -128, 127, 123 })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 255, 123 })]
	[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -32768, 32767, 12345 })]
	[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 65535, 12345 })]
	[InlineData("Int24", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -8388608, 8388607, 1234567 })]
	[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 16777215, 1234567 })]
	[InlineData("Int32", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -2147483648, 2147483647, 123456789 })]
	[InlineData("UInt32", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 4294967295L, 123456789 })]
	[InlineData("Int64", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -9223372036854775808, 9223372036854775807, 1234567890123456789 })]
	[InlineData("UInt64", new[] { 1, 0, 0, 2, 0 }, new[] { 0L, 0, 0, 0, 1234567890123456789 })]
	public async Task GetInt64(string column, int[] flags, long[] values)
	{
		await DoGetValue(column, (r, n) => r.GetInt64(n), (r, s) => r.GetInt64(s), flags, values).ConfigureAwait(false);
	}

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 2, 0, 0 }, new ushort[] { 0, 0, 0, 127, 123 })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new ushort[] { 0, 0, 0, 255, 123 })]
	[InlineData("Int16", new[] { 1, 0, 2, 0, 0 }, new ushort[] { 0, 0, 0, 32767, 12345 })]
	[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new ushort[] { 0, 0, 0, 65535, 12345 })]
	[InlineData("Int24", new[] { 1, 0, 2, 2, 2 }, new ushort[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt24", new[] { 1, 0, 0, 2, 2 }, new ushort[] { 0, 0, 0, 0, 0 })]
	[InlineData("Int32", new[] { 1, 0, 2, 2, 2 }, new ushort[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt32", new[] { 1, 0, 0, 2, 2 }, new ushort[] { 0, 0, 0, 0, 0 })]
	[InlineData("Int64", new[] { 1, 0, 2, 2, 2 }, new ushort[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt64", new[] { 1, 0, 0, 2, 2 }, new ushort[] { 0, 0, 0, 0, 0 })]
	public async Task GetUInt16(string column, int[] flags, ushort[] values)
	{
		await DoGetValue(column, (r, n) => r.GetUInt16(n), (r, s) => r.GetUInt16(s), flags, values).ConfigureAwait(false);
	}

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 2, 0, 0 }, new uint[] { 0, 0, 0, 127, 123 })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new uint[] { 0, 0, 0, 255, 123 })]
	[InlineData("Int16", new[] { 1, 0, 2, 0, 0 }, new uint[] { 0, 0, 0, 32767, 12345 })]
	[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new uint[] { 0, 0, 0, 65535, 12345 })]
	[InlineData("Int24", new[] { 1, 0, 2, 0, 0 }, new uint[] { 0, 0, 0, 8388607, 1234567 })]
	[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new uint[] { 0, 0, 0, 16777215, 1234567 })]
	[InlineData("Int32", new[] { 1, 0, 2, 0, 0 }, new uint[] { 0, 0, 0, 2147483647, 123456789 })]
	[InlineData("UInt32", new[] { 1, 0, 0, 0, 0 }, new uint[] { 0, 0, 0, 4294967295, 123456789 })]
	[InlineData("Int64", new[] { 1, 0, 2, 2, 2 }, new uint[] { 0, 0, 0, 0, 0 })]
	[InlineData("UInt64", new[] { 1, 0, 0, 2, 2 }, new uint[] { 0, 0, 0, 0, 0 })]
	public async Task GetUInt32(string column, int[] flags, uint[] values)
	{
		await DoGetValue(column, (r, n) => r.GetUInt32(n), (r, s) => r.GetUInt32(s), flags, values).ConfigureAwait(false);
	}

	[Theory]
	[InlineData("SByte", new[] { 1, 0, 2, 0, 0 }, new ulong[] { 0, 0, 0, 127, 123 })]
	[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new ulong[] { 0, 0, 0, 255, 123 })]
	[InlineData("Int16", new[] { 1, 0, 2, 0, 0 }, new ulong[] { 0, 0, 0, 32767, 12345 })]
	[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new ulong[] { 0, 0, 0, 65535, 12345 })]
	[InlineData("Int24", new[] { 1, 0, 2, 0, 0 }, new ulong[] { 0, 0, 0, 8388607, 1234567 })]
	[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new ulong[] { 0, 0, 0, 16777215, 1234567 })]
	[InlineData("Int32", new[] { 1, 0, 2, 0, 0 }, new ulong[] { 0, 0, 0, 2147483647, 123456789 })]
	[InlineData("UInt32", new[] { 1, 0, 0, 0, 0 }, new ulong[] { 0, 0, 0, 4294967295, 123456789 })]
	[InlineData("Int64", new[] { 1, 0, 2, 0, 0 }, new ulong[] { 0, 0, 0, 9223372036854775807, 1234567890123456789 })]
	[InlineData("UInt64", new[] { 1, 0, 0, 0, 0 }, new ulong[] { 0, 0, 0, 18446744073709551615, 1234567890123456789 })]
	public async Task GetUInt64(string column, int[] flags, ulong[] values)
	{
		await DoGetValue(column, (r, n) => r.GetUInt64(n), (r, s) => r.GetUInt64(s), flags, values).ConfigureAwait(false);
	}

	private async Task DoGetValue<T>(string column, Func<SingleStoreDataReader, int, T> getInt, Func<SingleStoreDataReader, string, T> getIntByName, int[] flags, T[] values)
	{
		using var cmd = Connection.CreateCommand();
		cmd.CommandText = $"select {column} from datatypes_integers order by rowid";
		using var reader = (SingleStoreDataReader) await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		for (int i = 0; i < flags.Length; i++)
		{
			Assert.True(await reader.ReadAsync().ConfigureAwait(false));
			switch (flags[i])
			{
			case 0: // normal
				Assert.Equal(values[i], getInt(reader, 0));
				Assert.Equal(values[i], getIntByName(reader, column));
				break;

			case 1: // null
				Assert.True(await reader.IsDBNullAsync(0).ConfigureAwait(false));
				break;

			case 2: // overflow
				Assert.Throws<OverflowException>(() => getInt(reader, 0));
				Assert.Throws<OverflowException>(() => getIntByName(reader, column));
				break;
			}
		}
		Assert.False(await reader.ReadAsync().ConfigureAwait(false));
		Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
	}

	[Theory]
	[InlineData("size", "ENUM", new object[] { null, "small", "medium" })]
	[InlineData("color", "ENUM", new object[] { "red", "orange", "green" })]
	public void QueryEnum(string column, string dataTypeName, object[] expected)
	{
#if BASELINE
		// mysql-connector-net incorrectly returns "VARCHAR" for "ENUM"
		dataTypeName = "VARCHAR";
#endif
		DoQuery("enums", column, dataTypeName, expected, reader => reader.GetString(0));
	}

	[Theory]
	[InlineData("value", "SET", new object[] { null, "", "one", "two", "one,two", "four", "one,four", "two,four", "one,two,four" })]
	public void QuerySet(string column, string dataTypeName, object[] expected)
	{
#if BASELINE
		// mysql-connector-net incorrectly returns "VARCHAR" for "ENUM"
		dataTypeName = "VARCHAR";
#endif
		DoQuery("set", column, dataTypeName, expected, reader => reader.GetString(column));
	}

	[Theory]
	[InlineData("Boolean", "BOOL", new object[] { null, false, true, false, true, true, true })]
	[InlineData("TinyInt1", "BOOL", new object[] { null, false, true, false, true, true, true })]
	public void QueryBoolean(string column, string dataTypeName, object[] expected)
	{
		dataTypeName = "TINYINT";
		DoQuery<InvalidCastException>("bools", column, dataTypeName, expected, reader => reader.GetBoolean(0));
	}

	[Theory]
	[InlineData("TinyInt1", "TINYINT", new object[] { null, (sbyte) 0, (sbyte) 1, (sbyte) 0, (sbyte) 1, (sbyte) -1, (sbyte) 123 })]
	[InlineData("Boolean", "TINYINT", new object[] { null, (sbyte) 0, (sbyte) 1, (sbyte) 0, (sbyte) 1, (sbyte) -1, (sbyte) 123 })]
	public void QueryTinyIntSbyte(string column, string dataTypeName, object[] expected)
	{
		var csb = CreateConnectionStringBuilder();
		csb.TreatTinyAsBoolean = false;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();
		DoQuery("bools", column, dataTypeName, expected, reader => reader.GetSByte(0), baselineCoercedNullValue: default(sbyte), connection: connection);
	}

	[Theory()]
	[InlineData("TinyInt1U", "TINYINT", new object[] { null, (byte) 0, (byte) 1, (byte) 0, (byte) 1, (byte) 255, (byte) 123 })]
	public void QueryTinyInt1Unsigned(string column, string dataTypeName, object[] expected)
	{
		DoQuery("bools", column, dataTypeName, expected, reader => reader.GetByte(0), baselineCoercedNullValue: default(byte));
	}

	[Theory]
	[InlineData("SByte", "TINYINT", new object[] { null, default(sbyte), sbyte.MinValue, sbyte.MaxValue, (sbyte) 123 })]
	public void QuerySByte(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetSByte(column), baselineCoercedNullValue: default(sbyte));
	}

	[Theory]
	[InlineData("Byte", "TINYINT", new object[] { null, default(byte), byte.MinValue, byte.MaxValue, (byte) 123 })]
	public void QueryByte(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetByte(0), baselineCoercedNullValue: default(byte));
	}

	[Theory]
	[InlineData("Int16", "SMALLINT", new object[] { null, default(short), short.MinValue, short.MaxValue, (short) 12345 })]
	public void QueryInt16(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetInt16(0));
	}

	[Theory]
	[InlineData("UInt16", "SMALLINT", new object[] { null, default(ushort), ushort.MinValue, ushort.MaxValue, (ushort) 12345 })]
	public void QueryUInt16(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetUInt16(column));
	}

	[Theory]
	[InlineData("Int24", "MEDIUMINT", new object[] { null, default(int), -8388608, 8388607, 1234567 })]
	[InlineData("Int32", "INT", new object[] { null, default(int), int.MinValue, int.MaxValue, 123456789 })]
	public void QueryInt32(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetInt32(0));
	}

	[Theory]
	[InlineData("UInt24", "MEDIUMINT", new object[] { null, default(uint), 0u, 16777215u, 1234567u })]
	[InlineData("UInt32", "INT", new object[] { null, default(uint), uint.MinValue, uint.MaxValue, 123456789u })]
	public void QueryUInt32(string column, string dataTypeName, object[] expected)
	{
#if BASELINE
		// mysql-connector-net incorrectly returns "INT" for "MEDIUMINT UNSIGNED"
		dataTypeName = "INT";
#endif
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetUInt32(column));
	}

	[Theory]
	[InlineData("Int64", "BIGINT", new object[] { null, default(long), long.MinValue, long.MaxValue, 1234567890123456789 })]
	public void QueryInt64(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetInt64(0));
	}

	[Theory]
	[InlineData("UInt64", "BIGINT", new object[] { null, default(ulong), ulong.MinValue, ulong.MaxValue, 1234567890123456789u })]
	public void QueryUInt64(string column, string dataTypeName, object[] expected)
	{
		DoQuery("integers", column, dataTypeName, expected, reader => reader.GetUInt64(0));
	}

	[Theory]
	[InlineData("Bit1", new object[] { null, 0UL, 1UL, 1UL })]
	[InlineData("Bit32", new object[] { null, 0UL, 1UL, (ulong) uint.MaxValue })]
	[InlineData("Bit64", new object[] { null, 0UL, 1UL, ulong.MaxValue })]
	public void QueryBits(string column, object[] expected)
	{
		DoQuery("bits", column, "BIT", expected, reader => reader.GetUInt64(column));
	}

	[Theory]
	[InlineData("Single", "FLOAT", new object[] { null, default(float), -3.40282e38f, -1.4013e-45f, 3.40282e38f, 1.4013e-45f })]
	public void QueryFloat(string column, string dataTypeName, object[] expected)
	{
		// don't perform exact queries for floating-point values; they may fail: http://dev.mysql.com/doc/refman/5.7/en/problems-with-float.html
		DoQuery("reals", column, dataTypeName, expected, reader => reader.GetFloat(0), omitWhereTest: true);
	}

	[Theory]
	[InlineData("`Double`", "DOUBLE", new object[] { null, default(double), -1.7976931348623157e308, -5e-324, 1.7976931348623157e308, 5e-324 })]
	public void QueryDouble(string column, string dataTypeName, object[] expected)
	{
		DoQuery("reals", column, dataTypeName, expected, reader => reader.GetDouble(0));
	}

	[Theory]
	[InlineData("SmallDecimal", new object[] { null, "0", "-999.99", "-0.01", "999.99", "0.01" })]
	[InlineData("MediumDecimal", new object[] { null, "0", "-999999999999.99999999", "-0.00000001", "999999999999.99999999", "0.00000001" })]
	// value exceeds the range of a decimal and cannot be deserialized
	// [InlineData("BigDecimal", new object[] { null, "0", "-99999999999999999999.999999999999999999999999999999", "-0.000000000000000000000000000001", "99999999999999999999.999999999999999999999999999999", "0.000000000000000000000000000001" })]
	public void QueryDecimal(string column, object[] expected)
	{
		for (int i = 0; i < expected.Length; i++)
			if (expected[i] is string expectedValue)
				expected[i] = decimal.Parse(expectedValue, CultureInfo.InvariantCulture);
		DoQuery("reals", column, "DECIMAL", expected, reader => reader.GetDecimal(0));
	}

	[Theory]
	[InlineData("utf8", new[] { null, "", "ASCII", "Ũńıċōđĕ", c_251ByteString })]
	[InlineData("utf8bin", new[] { null, "", "ASCII", "Ũńıċōđĕ", c_251ByteString })]
	public void QueryString(string column, string[] expected)
	{
		DoQuery("strings", column, "VARCHAR", expected, reader => reader.GetString(0));
#if !BASELINE
		DoQuery("strings", column, "VARCHAR", expected, reader => reader.GetTextReader(0), matchesDefaultType: false, assertEqual: (e, a) =>
		{
			using var actualReader = (TextReader) a;
			Assert.Equal(e, actualReader.ReadToEnd());
		}, getFieldValueType: typeof(TextReader));
#endif
	}
	const string c_251ByteString = "This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating \"this field is null\". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.";

	[Theory]
	[InlineData("guid", "CHAR(36)", new object[] { null, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-c000-000000000046", "fd24a0e8-c3f2-4821-a456-35da2dc4bb8f", "6A0E0A40-6228-11D3-A996-0050041896C8" })]
	[InlineData("guidbin", "CHAR(36)", new object[] { null, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-c000-000000000046", "fd24a0e8-c3f2-4821-a456-35da2dc4bb8f", "6A0E0A40-6228-11D3-A996-0050041896C8" })]
	public void QueryGuid(string column, string dataTypeName, object[] expected)
	{
		for (int i = 0; i < expected.Length; i++)
			if (expected[i] is string expectedValue)
				expected[i] = Guid.Parse(expectedValue);
		DoQuery<GetGuidWhenNullException>("strings", column, dataTypeName, expected, reader => reader.GetGuid(0));
	}

	[Theory]
	[InlineData("utf8", new[] { null, "", "ASCII", "Ũńıċōđĕ", c_251ByteString })]
	public void QueryChar(string column, string[] expected)
	{
		using var cmd = Connection.CreateCommand();
		cmd.CommandText = $@"select `{column}` from datatypes_strings order by rowid;";
		using var reader = cmd.ExecuteReader();
		for (var i = 0; i < expected.Length; i++)
		{
			Assert.True(reader.Read());
			if (expected[i] is null)
				Assert.True(reader.IsDBNull(0));
			else if (expected[i].Length == 0)
#if BASELINE
				Assert.Throws<IndexOutOfRangeException>(() => reader.GetChar(0));
#else
				Assert.Throws<InvalidCastException>(() => reader.GetChar(0));
#endif
			else
				Assert.Equal(expected[i][0], reader.GetChar(0));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void QueryBinaryGuid(bool oldGuids)
	{
		var csb = CreateConnectionStringBuilder();
		csb.OldGuids = oldGuids;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"select guidbin from datatypes_blobs order by rowid;";
		using (var reader = cmd.ExecuteReader())
		{
			Assert.True(reader.Read());
			Assert.Equal(DBNull.Value, reader.GetValue(0));
			Assert.True(reader.Read());
			var expectedBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
			var expectedGuid = new Guid(expectedBytes);
			if (oldGuids)
			{
				Assert.Equal(typeof(Guid), reader.GetFieldType(0));
				Assert.Equal(expectedGuid, reader.GetValue(0));
			}
			else
			{
				Assert.Equal(typeof(byte[]), reader.GetFieldType(0));
				Assert.Equal(expectedBytes, reader.GetValue(0));
			}
			Assert.Equal(expectedGuid, reader.GetGuid(0));
			Assert.Equal(expectedBytes, GetBytes(reader));
#if !BASELINE
			Assert.Equal(expectedBytes, GetStreamBytes(reader));
#endif
			Assert.False(reader.Read());
		}

		cmd.CommandText = @"select guidbin from datatypes_strings order by rowid;";
		using (var reader = cmd.ExecuteReader())
		{
			Assert.True(reader.Read());
			Assert.Equal(DBNull.Value, reader.GetValue(0));
			Assert.True(reader.Read());
			if (oldGuids)
			{
				Assert.Equal(typeof(string), reader.GetFieldType(0));
				Assert.Equal("00000000-0000-0000-0000-000000000000", reader.GetValue(0));
				Assert.Equal("00000000-0000-0000-0000-000000000000", reader.GetString("guidbin"));
			}
			else
			{
				Assert.Equal(typeof(Guid), reader.GetFieldType(0));
				Assert.Equal(Guid.Empty, reader.GetValue(0));
			}
			Assert.Equal(Guid.Empty, reader.GetGuid("guidbin"));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task QueryWithGuidParameter(bool oldGuids)
	{
		var csb = CreateConnectionStringBuilder();
		csb.OldGuids = oldGuids;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await connection.OpenAsync().ConfigureAwait(false);
		try
		{
			Assert.Equal(oldGuids ? 0L : 1L, (await connection.QueryAsync<long>(@"select count(*) from datatypes_strings where guid = @guid", new { guid = new Guid("fd24a0e8-c3f2-4821-a456-35da2dc4bb8f") }).ConfigureAwait(false)).SingleOrDefault());
			Assert.Equal(oldGuids ? 0L : 1L, (await connection.QueryAsync<long>(@"select count(*) from datatypes_strings where guidbin = @guid", new { guid = new Guid("fd24a0e8-c3f2-4821-a456-35da2dc4bb8f") }).ConfigureAwait(false)).SingleOrDefault());
		}
		catch (SingleStoreException ex) when (oldGuids && ex.Number is 1300 or 3854) // InvalidCharacterString, CannotConvertString
		{
			// new error in MySQL 8.0.24, MariaDB 10.5
		}
		Assert.Equal(oldGuids ? 1L : 0L, (await connection.QueryAsync<long>(@"select count(*) from datatypes_blobs where guidbin = @guid", new { guid = new Guid(0x33221100, 0x5544, 0x7766, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF) }).ConfigureAwait(false)).SingleOrDefault());
	}

	[Theory]
	[InlineData("char38", typeof(string))]
	[InlineData("char38bin", typeof(string))]
	[InlineData("text", typeof(string))]
	[InlineData("blob", typeof(byte[]))]
	public async Task GetGuid(string column, Type fieldType)
	{
		using var cmd = Connection.CreateCommand();
		cmd.CommandText = $"select `{column}` from datatypes_guids order by rowid";
		using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		Assert.Equal(fieldType, reader.GetFieldType(0));

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.True(reader.IsDBNull(0));
		Assert.Throws<GetGuidWhenNullException>(() => reader.GetGuid(0));

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.False(reader.IsDBNull(0));
		Assert.NotNull(reader.GetValue(0));
		Assert.IsType(fieldType, reader.GetValue(0));

		Type exceptionType = typeof(GetGuidWhenNullException);
#if BASELINE
		// baseline throws FormatException when conversion from string fails
		if (fieldType == typeof(string))
			exceptionType = typeof(FormatException);
#endif
		Assert.Throws(exceptionType, () => reader.GetGuid(0));

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.NotNull(reader.GetValue(0));
		Assert.IsType(fieldType, reader.GetValue(0));
		Assert.Equal(new Guid("33221100-5544-7766-8899-aabbccddeeff"), reader.GetGuid(0));

		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.NotNull(reader.GetValue(0));
		Assert.IsType(fieldType, reader.GetValue(0));
		Assert.Equal(new Guid("33221100-5544-7766-8899-aabbccddeeff"), reader.GetGuid(0));

		Assert.False(await reader.ReadAsync().ConfigureAwait(false));
	}

#if !BASELINE
	[Theory]
	[InlineData(SingleStoreGuidFormat.Default, false)]
	[InlineData(SingleStoreGuidFormat.Default, true)]
	[InlineData(SingleStoreGuidFormat.None, false)]
	[InlineData(SingleStoreGuidFormat.Char36, false)]
	[InlineData(SingleStoreGuidFormat.Char32, false)]
	[InlineData(SingleStoreGuidFormat.Binary16, false)]
	[InlineData(SingleStoreGuidFormat.TimeSwapBinary16, false)]
	[InlineData(SingleStoreGuidFormat.LittleEndianBinary16, false)]
	public void QueryGuidFormat(SingleStoreGuidFormat guidFormat, bool oldGuids)
	{
		bool isChar36 = guidFormat == SingleStoreGuidFormat.Char36 || (guidFormat == SingleStoreGuidFormat.Default && !oldGuids);
		bool isChar32 = guidFormat == SingleStoreGuidFormat.Char32;
		bool isBinary16 = guidFormat == SingleStoreGuidFormat.Binary16;
		bool isTimeSwapBinary16 = guidFormat == SingleStoreGuidFormat.TimeSwapBinary16;
		bool isLittleEndianBinary16 = guidFormat == SingleStoreGuidFormat.LittleEndianBinary16 || (guidFormat == SingleStoreGuidFormat.Default && oldGuids);

		Guid guid = new Guid("00112233-4455-6677-8899-AABBCCDDEEFF");
		string guidAsChar36 = "00112233-4455-6677-8899-AABBCCDDEEFF";
		string guidAsChar32 = "00112233445566778899AABBCCDDEEFF";
		byte[] guidAsBinary16 = { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
		byte[] guidAsTimeSwapBinary16 = { 0x66, 0x77, 0x44, 0x55, 0x00, 0x11, 0x22, 0x33, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
		byte[] guidAsLittleEndianBinary16 = { 0x33, 0x22, 0x11, 0x00, 0x55, 0x44, 0x77, 0x66, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };

		bool uuidToBin = AppConfig.SupportedFeatures.HasFlag(ServerFeatures.UuidToBin);
		string sql = $@"drop table if exists guid_format;
create table guid_format(
rowid integer not null primary key auto_increment,
c36 char(36),
c32 char(32),
b16 binary(16),
tsb16 binary(16),
leb16 binary(16),
t text,
b blob);
insert into guid_format(c36, c32, b16, tsb16, leb16, t, b) values(?, ?, ?, ?, ?, ?, ?);
insert into guid_format(c36, c32, b16, tsb16, leb16, t, b) values(?, ?, ?, ?, ?, ?, ?);
insert into guid_format(c36, c32, b16, tsb16, leb16, t, b) values(
'00112233-4455-6677-8899-AABBCCDDEEFF',
'00112233445566778899AABBCCDDEEFF',
{(uuidToBin ? "UUID_TO_BIN('00112233-4455-6677-8899-AABBCCDDEEFF', FALSE)" : "UNHEX('00112233445566778899AABBCCDDEEFF')")},
{(uuidToBin ? "UUID_TO_BIN('00112233-4455-6677-8899-AABBCCDDEEFF', TRUE)" : "UNHEX('66774455001122338899AABBCCDDEEFF')")},
UNHEX('33221100554477668899AABBCCDDEEFF'),
{(uuidToBin ? "BIN_TO_UUID(UNHEX('00112233445566778899AABBCCDDEEFF'))" : "'00112233-4455-6677-8899-AABBCCDDEEFF'")},
{(isBinary16 ? (uuidToBin ? "UUID_TO_BIN('00112233-4455-6677-8899-AABBCCDDEEFF')" : "UNHEX('00112233445566778899AABBCCDDEEFF')") : isTimeSwapBinary16 ? (uuidToBin ? "UUID_TO_BIN('00112233-4455-6677-8899-AABBCCDDEEFF', TRUE)" : "UNHEX('66774455001122338899AABBCCDDEEFF')") : "UNHEX('33221100554477668899AABBCCDDEEFF')")});
";

		var csb = CreateConnectionStringBuilder();
		csb.GuidFormat = guidFormat;
		csb.OldGuids = oldGuids;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();

		using var cmd = new SingleStoreCommand(sql, connection)
		{
			Parameters =
			{
				new() { Value = guidAsChar36 },
				new() { Value = guidAsChar32 },
				new() { Value = guidAsBinary16 },
				new() { Value = guidAsTimeSwapBinary16 },
				new() { Value = guidAsLittleEndianBinary16 },
				new() { Value = guidAsChar36 },
				new() { Value = isBinary16 ? guidAsBinary16 : isTimeSwapBinary16 ? guidAsTimeSwapBinary16 : guidAsLittleEndianBinary16 },
				new() { Value = isChar36 ? (object) guid : guidAsChar36 },
				new() { Value = isChar32 ? (object) guid : guidAsChar32 },
				new() { Value = isBinary16 ? (object) guid : guidAsBinary16 },
				new() { Value = isTimeSwapBinary16 ? (object) guid : guidAsTimeSwapBinary16 },
				new() { Value = isLittleEndianBinary16 ? (object) guid : guidAsLittleEndianBinary16 },
				new() { Value = guidAsChar32 },
				new() { Value = isBinary16 ? guidAsBinary16 : isTimeSwapBinary16 ? guidAsTimeSwapBinary16 : guidAsLittleEndianBinary16 },
			}
		};
		cmd.ExecuteNonQuery();
		cmd.CommandText = "select c36, c32, b16, tsb16, leb16, t, b from guid_format;";

		using var reader = cmd.ExecuteReader();
		for (int row = 0; row < 3; row++)
		{
			Assert.True(reader.Read());

			object c36 = reader.GetValue(0);
			if (isChar36)
				Assert.Equal(guid, (Guid) c36);
			else
				Assert.Equal(guidAsChar36, (string) c36);
			Assert.Equal(guid, reader.GetGuid(0));

			object c32 = reader.GetValue(1);
			if (isChar32)
				Assert.Equal(guid, (Guid) c32);
			else
				Assert.Equal(guidAsChar32, (string) c32);
			Assert.Equal(guid, reader.GetGuid(1));

			object b16 = reader.GetValue(2);
			if (isBinary16)
				Assert.Equal(guid, (Guid) b16);
			else if (isTimeSwapBinary16 || isLittleEndianBinary16)
				Assert.NotEqual(guid, (Guid) b16);
			else
				Assert.Equal(guidAsBinary16, (byte[]) b16);
			if (isBinary16)
				Assert.Equal(guid, reader.GetGuid(2));

			object tsb16 = reader.GetValue(3);
			if (isTimeSwapBinary16)
				Assert.Equal(guid, (Guid) tsb16);
			else if (isBinary16 || isLittleEndianBinary16)
				Assert.NotEqual(guid, (Guid) tsb16);
			else
				Assert.Equal(guidAsTimeSwapBinary16, (byte[]) tsb16);
			if (isTimeSwapBinary16)
				Assert.Equal(guid, reader.GetGuid(3));

			object leb16 = reader.GetValue(4);
			if (isLittleEndianBinary16)
				Assert.Equal(guid, (Guid) leb16);
			else if (isBinary16 || isTimeSwapBinary16)
				Assert.NotEqual(guid, (Guid) leb16);
			else
				Assert.Equal(guidAsLittleEndianBinary16, (byte[]) leb16);
			if (!isBinary16 && !isTimeSwapBinary16)
				Assert.Equal(guid, reader.GetGuid(4));

			Assert.IsType<string>(reader.GetValue(5));
			Assert.Equal(guid, reader.GetGuid(5));

			Assert.IsType<byte[]>(reader.GetValue(6));
			Assert.Equal(guid, reader.GetGuid(6));
		}

		Assert.False(reader.Read());
	}
#endif

	[Theory]
	[InlineData("`Date`", "DATE", new object[] { null, "1000 01 01", "9999 12 31", null, "2016 04 05" })]
	[InlineData("`DateTime`", "DATETIME", new object[] { null, "1000 01 01 0 0 0", "9999 12 31 23 59 59 999999", null, "2016 4 5 14 3 4 567890" })]
	[InlineData("`Timestamp`", "TIMESTAMP", new object[] { null, "1970 01 01 0 0 1", "2038 1 18 3 14 7 999999", null, "2016 4 5 14 3 4 567890" })]
	public void QueryDate(string column, string dataTypeName, object[] expected)
	{
		DoQuery("times", column, dataTypeName, ConvertToDateTime(expected, DateTimeKind.Unspecified), reader => reader.GetDateTime(column.Replace("`", "")));
#if !BASELINE
		DoQuery("times", column, dataTypeName, ConvertToDateTimeOffset(expected), reader => reader.GetDateTimeOffset(0), matchesDefaultType: false);
#endif
	}

#if NET6_0_OR_GREATER && !BASELINE
	[Theory]
	[InlineData("`Date`", "DATE", new object[] { null, "1000 01 01", "9999 12 31", null, "2016 04 05" })]
	public void QueryDateOnly(string column, string dataTypeName, object[] expected)
	{
		DoQuery("times", column, dataTypeName, ConvertToDateOnly(expected), reader => reader.GetDateOnly(column.Replace("`", "")),
			matchesDefaultType: false, assertEqual: (x, y) => Assert.Equal((DateOnly) x, y is DateTime dt ? DateOnly.FromDateTime(dt) : (DateOnly) y));
	}
#endif

	[SkippableTheory(ServerFeatures.ZeroDateTime)]
	[InlineData(false)]
	[InlineData(true)]
	public void QueryZeroDateTime(bool convertZeroDateTime)
	{
		var csb = CreateConnectionStringBuilder();
		csb.ConvertZeroDateTime = convertZeroDateTime;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"select cast(0 as date), cast(0 as datetime);";

		using var reader = (SingleStoreDataReader) cmd.ExecuteReader();
		Assert.True(reader.Read());
		if (convertZeroDateTime)
		{
			Assert.Equal(DateTime.MinValue, reader.GetDateTime(0));
			Assert.Equal(DateTime.MinValue, reader.GetDateTime(1));
#if !BASELINE
			Assert.Equal(DateTimeOffset.MinValue, reader.GetDateTimeOffset(0));
			Assert.Equal(DateTimeOffset.MinValue, reader.GetDateTimeOffset(1));
#endif
		}
		else
		{
#if BASELINE
			Assert.Throws<MySql.Data.Types.SingleStoreConversionException>(() => reader.GetDateTime(0));
			Assert.Throws<MySql.Data.Types.SingleStoreConversionException>(() => reader.GetDateTime(1));
#else
			Assert.Throws<InvalidCastException>(() => reader.GetDateTime(0));
			Assert.Throws<InvalidCastException>(() => reader.GetDateTime(1));
			Assert.Throws<InvalidCastException>(() => reader.GetDateTimeOffset(0));
			Assert.Throws<InvalidCastException>(() => reader.GetDateTimeOffset(1));
#endif
		}
	}

#if !BASELINE
	[Theory]
	[InlineData(SingleStoreDateTimeKind.Unspecified, DateTimeKind.Unspecified, true)]
	[InlineData(SingleStoreDateTimeKind.Unspecified, DateTimeKind.Local, true)]
	[InlineData(SingleStoreDateTimeKind.Unspecified, DateTimeKind.Utc, true)]
	[InlineData(SingleStoreDateTimeKind.Utc, DateTimeKind.Unspecified, true)]
	[InlineData(SingleStoreDateTimeKind.Utc, DateTimeKind.Local, false)]
	[InlineData(SingleStoreDateTimeKind.Utc, DateTimeKind.Utc, true)]
	[InlineData(SingleStoreDateTimeKind.Local, DateTimeKind.Unspecified, true)]
	[InlineData(SingleStoreDateTimeKind.Local, DateTimeKind.Local, true)]
	[InlineData(SingleStoreDateTimeKind.Local, DateTimeKind.Utc, false)]
	public void QueryDateTimeKind(SingleStoreDateTimeKind kindOption, DateTimeKind kindIn, bool success)
	{
		var csb = CreateConnectionStringBuilder();
		csb.DateTimeKind = kindOption;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();

		var dateTimeIn = new DateTime(2001, 2, 3, 14, 5, 6, 789, kindIn);
		using var cmd = new SingleStoreCommand(@"drop table if exists date_time_kind;
create table date_time_kind(
rowid integer not null primary key auto_increment,
d date,
dt0 datetime(0),
dt6 datetime(6));
insert into date_time_kind(d, dt0, dt6) values(?, ?, ?)", connection)
		{
			Parameters =
			{
				new() { Value = dateTimeIn },
				new() { Value = dateTimeIn },
				new() { Value = dateTimeIn },
			}
		};
		if (success)
		{
			cmd.ExecuteNonQuery();
			long lastInsertId = cmd.LastInsertedId;
			cmd.CommandText = $"select d, dt0, dt6 from date_time_kind where rowid = {lastInsertId};";

			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(new DateTime(2001, 2, 3), reader.GetValue(0));
			Assert.Equal(new DateTime(2001, 2, 3, 14, 5, 6, kindIn), reader.GetValue(1));
			Assert.Equal(dateTimeIn, reader.GetValue(2));
			for (int i = 0; i < 3; i++)
				Assert.Equal(kindOption, (SingleStoreDateTimeKind) reader.GetDateTime(i).Kind);
		}
		else
		{
			Assert.Throws<SingleStoreException>(() => cmd.ExecuteNonQuery());
		}
	}
#endif

	[Theory]
	[InlineData("`Time`", "TIME", new object[] { null, "-838 -59 -59", "838 59 59", "0 0 0", "0 14 3 4 567890" })]
	public void QueryTime(string column, string dataTypeName, object[] expected)
	{
		DoQuery("times", column, dataTypeName, ConvertToTimeSpan(expected), reader => reader.GetTimeSpan(0)
#if BASELINE // https://bugs.mysql.com/bug.php?id=103801
			, omitWherePrepareTest: true
#endif
		);
	}

#if NET6_0_OR_GREATER && !BASELINE
	[Theory]
	[InlineData("TimeOnly", "TIME", new object[] { null, "0 0 0", "0 23 59 59 999999", "0 0 0", "0 14 3 4 567890" })]
	public void QueryTimeOnly(string column, string dataTypeName, object[] expected)
	{
		DoQuery("times", column, dataTypeName, ConvertToTimeOnly(expected), reader => reader.GetTimeOnly(0),
			matchesDefaultType: false, assertEqual: (x, y) => Assert.Equal((TimeOnly) x, y is TimeSpan ts ? TimeOnly.FromTimeSpan(ts) : (TimeOnly) y));
	}
#endif

	[Theory]
	[InlineData("`Year`", "YEAR", new object[] { null, 1901, 2155, null, 2016 })]
	public void QueryYear(string column, string dataTypeName, object[] expected)
	{
		Func<SingleStoreDataReader, object> getValue = reader => reader.GetInt32(0);
#if BASELINE
		// Connector/NET incorrectly returns "SMALLINT" for "YEAR", and returns all YEAR values as short values
		dataTypeName = "SMALLINT";
		expected = expected.Select(x => x is null ? null : (object) (short) (int) x).ToArray();
		getValue = reader => reader.GetInt16(0);
#endif
		DoQuery("times", column, dataTypeName, expected, getValue);
	}

	[Theory]
	[InlineData("Binary", 100)]
	[InlineData("VarBinary", 0)]
	[InlineData("TinyBlob", 0)]
	[InlineData("Blob", 0)]
	[InlineData("MediumBlob", 0)]
	[InlineData("LongBlob", 0)]
	public void QueryBlob(string column, int padLength)
	{
		var data = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff };
		if (data.Length < padLength)
			Array.Resize(ref data, padLength);

		DoQuery<GetBytesWhenNullException>("blobs", "`" + column + "`", "BLOB", new object[] { null, data }, GetBytes);
		DoQuery("blobs", "`" + column + "`", "BLOB", new object[] { null, data }, GetStreamBytes);
		DoQuery("blobs", "`" + column + "`", "BLOB", new object[] { null, data }, reader => reader.GetStream(0), matchesDefaultType: false, assertEqual: (e, a) =>
		{
			using var stream = (Stream) a;
			Assert.True(stream.CanRead);
			Assert.False(stream.CanWrite);
			var bytes = new byte[stream.Length];
			Assert.Equal(bytes.Length, stream.Read(bytes, 0, bytes.Length));
			Assert.Equal(e, bytes);
		}, getFieldValueType: typeof(Stream));
	}

	[Theory]
	[InlineData("TinyBlob", 255)]
	[InlineData("Blob", 65535)]
	[InlineData("MediumBlob", 16777215)]
	[InlineData("LongBlob", 33554432)]
	[InlineData("LongBlob", 67108864)]
	public async Task InsertLargeBlobAsync(string column, int size)
	{
		// NOTE: MySQL Server will reset the connection when it receives an oversize packet, so we need to create a test-specific connection here
		using var connection = new SingleStoreConnection(CreateConnectionStringBuilder().ConnectionString);
		await connection.OpenAsync();

		var data = CreateByteArray(size);
		var isSupported = size < 1048576 || AppConfig.SupportedFeatures.HasFlag(ServerFeatures.LargePackets);

		long lastInsertId;
		using (var cmd = new SingleStoreCommand($"insert into datatypes_blob_insert(`{column}`) values(@data)", connection))
		{
			try
			{
				cmd.Parameters.AddWithValue("@data", data);
				await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
				lastInsertId = cmd.LastInsertedId;
				Assert.True(isSupported);
			}
			catch (SingleStoreException ex)
			{
				Console.WriteLine(ex.Message);
				lastInsertId = -1;
				Assert.False(isSupported);
				Assert.True(ex.Message.IndexOf("packet") >= 0 || ex.Message.IndexOf("innodb_log_file_size") >= 0);
			}
		}

		if (isSupported)
		{
			var queryResult = (await connection.QueryAsync<byte[]>($"select `{column}` from datatypes_blob_insert where rowid = {lastInsertId}").ConfigureAwait(false)).Single();
			TestUtilities.AssertEqual(data, queryResult);

			await connection.ExecuteAsync($"delete from datatypes_blob_insert where rowid = {lastInsertId}").ConfigureAwait(false);
		}
	}

	[Theory]
	[InlineData("TinyBlob", 255)]
	[InlineData("Blob", 65535)]
	[InlineData("MediumBlob", 16777215)]
	[InlineData("LongBlob", 67108864)]
	public void InsertLargeBlobSync(string column, int size)
	{
		// NOTE: MySQL Server will reset the connection when it receives an oversize packet, so we need to create a test-specific connection here
		using var connection = new SingleStoreConnection(CreateConnectionStringBuilder().ConnectionString);
		connection.Open();

		var data = CreateByteArray(size);
		var isSupported = size < 1048576 || AppConfig.SupportedFeatures.HasFlag(ServerFeatures.LargePackets);

		long lastInsertId;
		using (var cmd = new SingleStoreCommand($"insert into datatypes_blob_insert(`{column}`) values(@data)", connection))
		{
			try
			{
				cmd.Parameters.AddWithValue("@data", data);
				cmd.ExecuteNonQuery();
				lastInsertId = cmd.LastInsertedId;
				Assert.True(isSupported);
			}
			catch (SingleStoreException ex)
			{
				Console.WriteLine(ex.Message);
				lastInsertId = -1;
				Assert.False(isSupported);
				Assert.True(ex.Message.IndexOf("packet") >= 0 || ex.Message.IndexOf("innodb_log_file_size") >= 0);
			}
		}

		if (isSupported)
		{
			var queryResult = connection.Query<byte[]>($"select `{column}` from datatypes_blob_insert where rowid = {lastInsertId}").Single();
			TestUtilities.AssertEqual(data, queryResult);

			connection.Execute($"delete from datatypes_blob_insert where rowid = {lastInsertId}");
		}
	}

	[Theory]
	[InlineData(false, "Date", typeof(DateTime), "1000 01 01")]
	[InlineData(true, "Date", typeof(SingleStoreDateTime), "1000 01 01")]
	[InlineData(false, "DateTime", typeof(DateTime), "1000 01 01")]
	[InlineData(true, "DateTime", typeof(SingleStoreDateTime), "1000 01 01")]
	[InlineData(false, "TimeStamp", typeof(DateTime), "1970 01 01 0 0 1")]
	[InlineData(true, "TimeStamp", typeof(SingleStoreDateTime), "1970 01 01 0 0 1")]
	[InlineData(false, "Time", typeof(TimeSpan), null)]
	[InlineData(true, "Time", typeof(TimeSpan), null)]
	public void AllowZeroDateTime(bool allowZeroDateTime, string columnName, Type expectedType, string expectedDateTime)
	{
		var csb = CreateConnectionStringBuilder();
		csb.AllowZeroDateTime = allowZeroDateTime;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();

		using var cmd = new SingleStoreCommand($"SELECT `{columnName}` FROM datatypes_times WHERE `{columnName}` IS NOT NULL ORDER BY rowid", connection);
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(expectedType, reader.GetFieldType(0));
		Assert.IsType(expectedType, reader.GetValue(0));
		var dt = reader.GetSchemaTable();
		Assert.Equal(expectedType, dt.Rows[0]["DataType"]);

		if (expectedDateTime is not null)
		{
			var expected = (DateTime) ConvertToDateTime(new object[] { expectedDateTime }, DateTimeKind.Unspecified)[0];
			Assert.Equal(expected, reader.GetDateTime(0));
			Assert.Equal(new SingleStoreDateTime(expected), reader.GetSingleStoreDateTime(0));
		}
	}

	[Theory]
	[InlineData("Date")]
	[InlineData("DateTime")]
	[InlineData("Timestamp")]
	public void GetSingleStoreDateTime(string columnName)
	{
		using var cmd = new SingleStoreCommand($"SELECT `{columnName}` FROM datatypes_times WHERE `{columnName}` IS NOT NULL", Connection);
		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			var dt = reader.GetDateTime(0);
			var msdt = reader.GetSingleStoreDateTime(0);
			Assert.True(msdt.IsValidDateTime);
			Assert.Equal(dt, msdt.GetDateTime());
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReadNewDate(bool prepare)
	{
		// returns a NEWDATE in MySQL < 5.7.22; see https://github.com/mysql-net/MySqlConnector/issues/1007
		using var cmd = new SingleStoreCommand($"SELECT `Date` FROM datatypes_times UNION ALL SELECT `Date` FROM datatypes_times", Connection);
		if (prepare)
			cmd.Prepare();
		using var reader = cmd.ExecuteReader();

#if !BASELINE
		var columnSchema = reader.GetColumnSchema()[0];
		Assert.Equal("Date", columnSchema.ColumnName);
		Assert.Equal(typeof(DateTime), columnSchema.DataType);
		Assert.Equal("DATE", columnSchema.DataTypeName);
#endif

		var schemaRow = reader.GetSchemaTable().Rows[0];
		Assert.Equal("Date", schemaRow["ColumnName"]);
		Assert.Equal(typeof(DateTime), schemaRow["DataType"]);

		while (reader.Read())
		{
			if (!reader.IsDBNull(0))
				reader.GetDateTime(0);
		}
	}

	[Theory]
	[InlineData("Date", false, "9999 12 31")]
	[InlineData("Date", true, "9999 12 31")]
	[InlineData("DateTime", false, "9999 12 31 23 59 59 999999")]
	[InlineData("DateTime", true, "9999 12 31 23 59 59 999999")]
	[InlineData("Time", false, null)]
	[InlineData("Time", true, null)]
	public void ReadVarCharFromNestedQueryAsDate(string columnName, bool prepare, string expectedValue)
	{
		var expectedDate = (DateTime?) ConvertToDateTime(new object[] { expectedValue }, DateTimeKind.Unspecified)[0];

		// returns VARCHAR in MySQL 5.7; DATE in MySQL 8.0
		using var cmd = new SingleStoreCommand($@"SELECT MAX(CASE WHEN 1 = t.`Key` THEN t.`{columnName}` END) AS `Max`
FROM (SELECT `{columnName}`, 1 AS `Key` FROM datatypes_times) t
GROUP BY t.`Key`
ORDER BY t.`Key`", Connection);
		if (prepare)
			cmd.Prepare();

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		if (expectedDate.HasValue)
			Assert.Equal(expectedDate.Value, reader.GetDateTime(0));
		else
			Assert.ThrowsAny<Exception>(() => reader.GetDateTime(0));
	}

	[Theory]
#if !BASELINE
	[InlineData("1001-02", false, null)]
	[InlineData("1001-02", true, null)]
	[InlineData("2000-01-02 03-04-05", true, null)]
	[InlineData("2000-01-02 18:19:20.9876543", false, null)]
	[InlineData("2000-01-02 18:19:20.9876543", true, null)]
	[InlineData("2000-01-02 03-04-05 123456", false, null)]
#endif
	[InlineData("1001-02-03", false, "1001 2 3")]
	[InlineData("1001-02-0A", true, null)]
	[InlineData("2000-01-02 03:04:05", false, "2000 1 2 3 4 5")]
	[InlineData("2000-01-02T03:04:05", false, null)]
	[InlineData("2000-01-02 2003-04-05", true, null)]
	[InlineData("2000-01-02 18:19:20.9", true, "2000 1 2 18 19 20 900000")]
	[InlineData("2000-01-02 18:19:20.98", false, "2000 1 2 18 19 20 980000")]
	[InlineData("2000-01-02 18:19:20.987", true, "2000 1 2 18 19 20 987000")]
	[InlineData("2000-01-02 18:19:20.9876", false, "2000 1 2 18 19 20 987600")]
	[InlineData("2000-01-02 18:19:20.98765", true, "2000 1 2 18 19 20 987650")]
	[InlineData("2000-01-02 18:19:20.987654", false, "2000 1 2 18 19 20 987654")]
	public void ReadVarCharAsDate(string value, bool prepare, string expectedValue)
	{
		var expectedDate = (DateTime?) ConvertToDateTime(new object[] { expectedValue }, DateTimeKind.Unspecified)[0];

		// returns VARCHAR in MySQL 5.7; DATE in MySQL 8.0
		using var cmd = new SingleStoreCommand($@"SELECT '{value}' AS value", Connection);
		if (prepare)
			cmd.Prepare();

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		if (expectedDate.HasValue)
			Assert.Equal(expectedDate.Value, reader.GetDateTime(0));
		else
#if BASELINE
			Assert.ThrowsAny<Exception>(() => reader.GetDateTime(0));
#else
			Assert.Throws<FormatException>(() => reader.GetDateTime(0));
#endif
	}

	[Theory]
	// [InlineData("Geography", "GEOGRAPHY", "POINT(1.00000000 1.00000000)")] those two are failing bc we use string to represent geospatial types => SS returns POINT(0.99999998 1.00000003)
	// [InlineData("Point", "POINT", "POINT(1.00000000 1.00000000)")]
	[InlineData("LineString", "GEOGRAPHY", "LINESTRING(0.00000000 0.00000000, 1.00000000 1.00000000, 2.00000000 2.00000000)")]
	[InlineData("Polygon", "GEOGRAPHY", "POLYGON((0.00000000 0.00000000, 1.00000000 0.00000000, 1.00000000 1.00000000, 0.00000000 1.00000000, 0.00000000 0.00000000))")]
	public void QueryGeography(string columnName, string dataTypeName, string expectedGeography)
	{
		// Disable the test for SingleStore versions earlier than 8.7 due to difference in metadata values
		if (Connection.Session.S2ServerVersion.Version < new Version(8, 7, 0))
		{
			return;
		}

		var csb = CreateConnectionStringBuilder();
		csb.TreatChar48AsGeographyPoint = true;
		using var connectionWithParam = new SingleStoreConnection(csb.ConnectionString);
		connectionWithParam.Open();

		var geographyData = new string[]
		{
			null,
			expectedGeography
		};

		DoQuery("geography", columnName, dataTypeName, geographyData, reader => reader.GetString(0), connection: connectionWithParam);

		if (dataTypeName == "GEOGRAPHY")
		{
			DoQuery<GetGeographyWhenNullException>("geography", columnName, dataTypeName,
				geographyData.Select(CreateGeography).ToArray(),
				reader => reader.GetSingleStoreGeography(0),
				matchesDefaultType: false,
				connection: connectionWithParam,
				assertEqual: (x, y) =>
					Assert.Equal(((SingleStoreGeography) x)?.Value, ((SingleStoreGeography) y)?.Value));
		}
		else
		{
			DoQuery<GetGeographyWhenNullException>("geography", columnName, dataTypeName,
				geographyData.Select(CreateGeographyPoint).ToArray(),
				reader => reader.GetSingleStoreGeographyPoint(0),
				matchesDefaultType: false,
				connection: connectionWithParam,
				assertEqual: (x, y) =>
					Assert.Equal(((SingleStoreGeographyPoint) x)?.Value, ((SingleStoreGeographyPoint) y)?.Value));
		}

		connectionWithParam.Close();
	}

	private static object CreateGeography(string data)
	{
		if (data is null)
			return null;

		return new SingleStoreGeography(data);
	}

	private static object CreateGeographyPoint(string data)
	{
		if(data is null)
			return null;

		return new SingleStoreGeographyPoint(data);
	}

	[Theory]
	[InlineData("Bit1", "datatypes_bits", SingleStoreDbType.Bit, 1, typeof(ulong), "N", 0, 0)]
	[InlineData("Bit32", "datatypes_bits", SingleStoreDbType.Bit, 32, typeof(ulong), "N", 0, 0)]
	[InlineData("Bit64", "datatypes_bits", SingleStoreDbType.Bit, 64, typeof(ulong), "N", 0, 0)]
	[InlineData("Binary", "datatypes_blobs", SingleStoreDbType.Binary, 100, typeof(byte[]), "N", 0, 0)]
	[InlineData("VarBinary", "datatypes_blobs", SingleStoreDbType.VarBinary, 100, typeof(byte[]), "N", 0, 0)]
	[InlineData("TinyBlob", "datatypes_blobs", SingleStoreDbType.TinyBlob, 255, typeof(byte[]), "N", 0, 0)]
	[InlineData("Blob", "datatypes_blobs", SingleStoreDbType.Blob, 65535, typeof(byte[]), "LN", 0, 0)]
	[InlineData("MediumBlob", "datatypes_blobs", SingleStoreDbType.MediumBlob, 16777215, typeof(byte[]), "LN", 0, 0)]
	[InlineData("LongBlob", "datatypes_blobs", SingleStoreDbType.LongBlob, int.MaxValue, typeof(byte[]), "LN", 0, 0)]
	[InlineData("guidbin", "datatypes_blobs", SingleStoreDbType.Binary, 16, typeof(byte[]), "N", 0, 0)]
	[InlineData("rowid", "datatypes_bools", SingleStoreDbType.Int64, 20, typeof(long), "AK", 0, 0)]
#if BASELINE
	[InlineData("Boolean", "datatypes_bools", SingleStoreDbType.Byte, 1, typeof(bool), "N", 0, 0)]
	[InlineData("TinyInt1", "datatypes_bools", SingleStoreDbType.Byte, 1, typeof(bool), "N", 0, 0)]
#else
	[InlineData("Boolean", "datatypes_bools", SingleStoreDbType.Byte, 4, typeof(sbyte), "N", 0, 0)]
	[InlineData("TinyInt1", "datatypes_bools", SingleStoreDbType.Byte, 4, typeof(sbyte), "N", 0, 0)]
#endif
	[InlineData("TinyInt1U", "datatypes_bools", SingleStoreDbType.UByte, 3, typeof(byte), "N", 0, 0)]
	[InlineData("size", "datatypes_enums", SingleStoreDbType.Enum, 7, typeof(string), "N", 0, 0)]
	[InlineData("color", "datatypes_enums", SingleStoreDbType.Enum, 6, typeof(string), "", 0, 0)]
	[InlineData("char38", "datatypes_guids", SingleStoreDbType.String, 38, typeof(string), "N", 0, 0)]
	[InlineData("char38bin", "datatypes_guids", SingleStoreDbType.String, 38, typeof(string), "N", 0, 0)]
	[InlineData("text", "datatypes_guids", SingleStoreDbType.Text, 65535, typeof(string), "LN", 0, 0)]
	[InlineData("blob", "datatypes_guids", SingleStoreDbType.Blob, 65535, typeof(byte[]), "LN", 0, 0)]
	[InlineData("SByte", "datatypes_integers", SingleStoreDbType.Byte, 4, typeof(sbyte), "N", 0, 0)]
	[InlineData("Byte", "datatypes_integers", SingleStoreDbType.UByte, 3, typeof(byte), "N", 0, 0)]
	[InlineData("Int16", "datatypes_integers", SingleStoreDbType.Int16, 6, typeof(short), "N", 0, 0)]
	[InlineData("UInt16", "datatypes_integers", SingleStoreDbType.UInt16, 5, typeof(ushort), "N", 0, 0)]
	[InlineData("Int24", "datatypes_integers", SingleStoreDbType.Int24, 9, typeof(int), "N", 0, 0)]
	[InlineData("UInt24", "datatypes_integers", SingleStoreDbType.UInt24, 8, typeof(uint), "N", 0, 0)]
	[InlineData("Int32", "datatypes_integers", SingleStoreDbType.Int32, 11, typeof(int), "N", 0, 0)]
	[InlineData("UInt32", "datatypes_integers", SingleStoreDbType.UInt32, 10, typeof(uint), "N", 0, 0)]
	[InlineData("Int64", "datatypes_integers", SingleStoreDbType.Int64, 20, typeof(long), "N", 0, 0)]
	[InlineData("UInt64", "datatypes_integers", SingleStoreDbType.UInt64, 20, typeof(ulong), "N", 0, 0)]
	[InlineData("value", "datatypes_json_core", SingleStoreDbType.JSON, int.MaxValue, typeof(string), "LN", 0, 0)]
	[InlineData("Single", "datatypes_reals", SingleStoreDbType.Float, 12, typeof(float), "N", 0, 31)]
	[InlineData("Double", "datatypes_reals", SingleStoreDbType.Double, 22, typeof(double), "N", 0, 31)]
	[InlineData("SmallDecimal", "datatypes_reals", SingleStoreDbType.NewDecimal, 7, typeof(decimal), "N", 5, 2)]
	[InlineData("MediumDecimal", "datatypes_reals", SingleStoreDbType.NewDecimal, 30, typeof(decimal), "N", 28, 8)]
	[InlineData("BigDecimal", "datatypes_reals", SingleStoreDbType.NewDecimal, 52, typeof(decimal), "N", 50, 30)]
	[InlineData("value", "datatypes_set", SingleStoreDbType.Set, 12, typeof(string), "N", 0, 0)]
	[InlineData("utf8", "datatypes_strings", SingleStoreDbType.VarChar, 300, typeof(string), "N", 0, 0)]
	[InlineData("utf8bin", "datatypes_strings", SingleStoreDbType.VarChar, 300, typeof(string), "N", 0, 0)]
	[InlineData("guid", "datatypes_strings", SingleStoreDbType.Guid, 36, typeof(Guid), "N", 0, 0)]
	[InlineData("guidbin", "datatypes_strings", SingleStoreDbType.Guid, 36, typeof(Guid), "N", 0, 0)]
	[InlineData("Date", "datatypes_times", SingleStoreDbType.Date, 10, typeof(DateTime), "N", 0, 0)]
	[InlineData("DateTime", "datatypes_times", SingleStoreDbType.DateTime, 26, typeof(DateTime), "N", 0, 6)]
	[InlineData("Timestamp", "datatypes_times", SingleStoreDbType.Timestamp, 26, typeof(DateTime), "N", 0, 6)]
	[InlineData("Time", "datatypes_times", SingleStoreDbType.Time, 18, typeof(TimeSpan), "N", 0, 6)]
#if BASELINE
	[InlineData("Year", "datatypes_times", SingleStoreDbType.Year, 4, typeof(short), "N", 0, 0)]
#else
	[InlineData("Year", "datatypes_times", SingleStoreDbType.Year, 4, typeof(int), "N", 0, 0)]
#endif
	[InlineData("Geography", "datatypes_geography", SingleStoreDbType.Geography, 1073741823, typeof(string), "N", 0, 0)]
	[InlineData("Point", "datatypes_geography", SingleStoreDbType.GeographyPoint, 48, typeof(string), "N", 0, 0)]
	[InlineData("LineString", "datatypes_geography", SingleStoreDbType.Geography, 1073741823, typeof(string), "N", 0, 0)]
	[InlineData("Polygon", "datatypes_geography", SingleStoreDbType.Geography, 1073741823, typeof(string), "N", 0, 0)]
	public void GetSchemaTable(string column, string table, SingleStoreDbType mySqlDbType, int columnSize, Type dataType, string flags, int precision, int scale)
	{
		// Test mock: Geography types are properly supported in SingleStoreConnector 1.2.0+ only with SingleStore 8.7 or later
		// If using SingleStoreConnector <1.2.0, stick to SingleStore 8.5 or earlier
		if (mySqlDbType == SingleStoreDbType.Geography && Connection.Session.S2ServerVersion.Version < new Version(8, 7, 0))
		{
			columnSize = 1431655765;
		}

		SingleStoreConnection connectionWithParam = null;
		if (mySqlDbType == SingleStoreDbType.GeographyPoint)
		{
			var csb = CreateConnectionStringBuilder();
			csb.TreatChar48AsGeographyPoint = true;
			connectionWithParam = new SingleStoreConnection(csb.ConnectionString);
			connectionWithParam.Open();
		}

		DoGetSchemaTable(column, table, mySqlDbType, columnSize, dataType, flags, precision, scale, connection: connectionWithParam);
		if(connectionWithParam != null)
			connectionWithParam.Close();
	}

	[Theory]
	[InlineData("`decimal-type` decimal(10,0) NOT NULL", "decimal-type", SingleStoreDbType.NewDecimal, 11, typeof(decimal), "", 10, 0)]
	[InlineData("`decimal-type` decimal(10,1) NOT NULL", "decimal-type", SingleStoreDbType.NewDecimal, 12, typeof(decimal), "", 10, 1)]
	[InlineData("`decimal-type` decimal(10,0) UNSIGNED NOT NULL", "decimal-type", SingleStoreDbType.NewDecimal, 10, typeof(decimal), "", 10, 0)]
	[InlineData("`decimal-type` decimal(10,1) UNSIGNED NOT NULL", "decimal-type", SingleStoreDbType.NewDecimal, 11, typeof(decimal), "", 10, 1)]
	[InlineData("`decimal-type` decimal(65,30) NOT NULL", "decimal-type", SingleStoreDbType.NewDecimal, 67, typeof(decimal), "", 65, 30)]
	[InlineData("`decimal-type` decimal(1,1) NOT NULL", "decimal-type", SingleStoreDbType.NewDecimal, 3, typeof(decimal), "", 1, 1)]
	public void GetSchemaTableForNewColumn(string createColumn, string column, SingleStoreDbType mySqlDbType, int columnSize, Type dataType, string flags, int precision, int scale)
	{
		Connection.Execute($@"drop table if exists schema_table;
create table schema_table({createColumn});");

		DoGetSchemaTable(column, "schema_table", mySqlDbType, columnSize, dataType, flags, precision, scale);
	}

	private void DoGetSchemaTable(string column, string table, SingleStoreDbType mySqlDbType, int columnSize, Type dataType, string flags, int precision, int scale, SingleStoreConnection connection = null)
	{
		if (table == "datatypes_json_core" && !AppConfig.SupportsJson)
			return;

		connection = connection ?? Connection;
		var isAutoIncrement = flags.IndexOf('A') != -1;
		var isKey = flags.IndexOf('K') != -1;
		var isLong = flags.IndexOf('L') != -1;
		var allowDbNull = flags.IndexOf('N') != -1;

		using var command = connection.CreateCommand();
		command.CommandText = $"select `{column}` from `{table}`;";

		using var reader = command.ExecuteReader(CommandBehavior.SchemaOnly);
		Assert.False(reader.Read());
		var schemaTable = reader.GetSchemaTable();
		Assert.Single(schemaTable.Rows);
		var schema = schemaTable.Rows[0];
		Assert.Equal(column, schema["ColumnName"]);
#if BASELINE
		int ordinal = 1; // https://bugs.mysql.com/bug.php?id=61477
#else
		int ordinal = 0;
#endif
		Assert.Equal(ordinal, schema["ColumnOrdinal"]);
		Assert.Equal(dataType, schema["DataType"]);
#if BASELINE
		// https://bugs.mysql.com/bug.php?id=87876
		if (columnSize != int.MaxValue)
			Assert.Equal(columnSize, schema["ColumnSize"]);
#else
		// TODO: PLAT-6085 remove this if
		if (column != "Single" && column != "Double")

			// this if is here because wrong charset is reported in 7.5 and 7.6 for utf8mb4 fields
			if ((column == "utf8" || column == "utf8bin") &&
				connection.Session.S2ServerVersion.Version.CompareTo(new Version(7, 5, 0)) >=0 &&
				connection.Session.S2ServerVersion.Version.CompareTo(new Version(7, 8, 0)) < 0)
			{
			}
			else
				Assert.Equal(columnSize, schema["ColumnSize"]);
#endif
		Assert.Equal(isLong, schema["IsLong"]);
		Assert.Equal(isAutoIncrement, schema["IsAutoIncrement"]);

		// TODO: PLAT-6086: investigate rowid reported non-primary key but unique in columnstore
		if (column != "rowid")
			Assert.Equal(isKey, schema["IsKey"]);
		Assert.Equal(allowDbNull, schema["AllowDBNull"]);
		Assert.Equal(precision, schema["NumericPrecision"]);

		// TODO: PLAT-6087 remove this if
		if (column != "Date")
			Assert.Equal(scale, schema["NumericScale"]);
#if BASELINE
		if (mySqlDbType == SingleStoreDbType.Enum || mySqlDbType == SingleStoreDbType.Set)
			mySqlDbType = SingleStoreDbType.String;
#endif
		Assert.Equal(mySqlDbType, (SingleStoreDbType) schema["ProviderType"]);
		Assert.Equal(connection.Database, schema["BaseSchemaName"]);
		Assert.Equal(table, schema["BaseTableName"]);
		Assert.Equal(column, schema["BaseColumnName"]);
		Assert.False((bool) schema["IsUnique"]);
		Assert.False((bool) schema["IsRowVersion"]);
		Assert.False((bool) schema["IsReadOnly"]);
	}

	[Fact]
	public void GetSchemaTableTwice()
	{
		using var command = Connection.CreateCommand();
		command.CommandText = "select Int16 from datatypes_integers; select Int32 from datatypes_integers;";

		using var reader = command.ExecuteReader();
		var table = reader.GetSchemaTable();
		Assert.Equal("Int16", table.Rows[0]["ColumnName"]);

		while (reader.Read())
		{
		}

		Assert.True(reader.NextResult());

		table = reader.GetSchemaTable();
		Assert.Equal("Int32", table.Rows[0]["ColumnName"]);
	}

	[Fact]
	public void GetSchemaTableAfterNextResult()
	{
		using var command = Connection.CreateCommand();
		command.CommandText = "select Int16 from datatypes_integers;";

		using var reader = command.ExecuteReader();
		var table = reader.GetSchemaTable();
		Assert.NotNull(table);
		Assert.Equal("Int16", table.Rows[0]["ColumnName"]);

		while (reader.Read())
		{
		}

		Assert.False(reader.NextResult());
		Assert.Null(reader.GetSchemaTable());
	}

#if !BASELINE
	[Theory]
	[InlineData("Bit1", "datatypes_bits", SingleStoreDbType.Bit, "BIT", 1, typeof(ulong), "N", -1, 0)]
	[InlineData("Bit32", "datatypes_bits", SingleStoreDbType.Bit, "BIT", 32, typeof(ulong), "N", -1, 0)]
	[InlineData("Bit64", "datatypes_bits", SingleStoreDbType.Bit, "BIT", 64, typeof(ulong), "N", -1, 0)]
	[InlineData("Binary", "datatypes_blobs", SingleStoreDbType.Binary, "BLOB", 100, typeof(byte[]), "N", -1, 0)]
	[InlineData("VarBinary", "datatypes_blobs", SingleStoreDbType.VarBinary, "BLOB", 100, typeof(byte[]), "N", -1, 0)]
	[InlineData("TinyBlob", "datatypes_blobs", SingleStoreDbType.TinyBlob, "BLOB", 255, typeof(byte[]), "N", -1, 0)]
	[InlineData("Blob", "datatypes_blobs", SingleStoreDbType.Blob, "BLOB", 65535, typeof(byte[]), "LN", -1, 0)]
	[InlineData("MediumBlob", "datatypes_blobs", SingleStoreDbType.MediumBlob, "BLOB", 16777215, typeof(byte[]), "LN", -1, 0)]
	[InlineData("LongBlob", "datatypes_blobs", SingleStoreDbType.LongBlob, "BLOB", int.MaxValue, typeof(byte[]), "LN", -1, 0)]
	[InlineData("guidbin", "datatypes_blobs", SingleStoreDbType.Binary, "BLOB", 16, typeof(byte[]), "N", -1, 0)]
	[InlineData("rowid", "datatypes_bools", SingleStoreDbType.Int64, "BIGINT", 20, typeof(long), "AK", -1, 0)]
	[InlineData("Boolean", "datatypes_bools", SingleStoreDbType.Byte, "TINYINT", 4, typeof(sbyte), "N", -1, 0)]
	[InlineData("TinyInt1", "datatypes_bools", SingleStoreDbType.Byte, "TINYINT", 4, typeof(sbyte), "N", -1, 0)]
	[InlineData("TinyInt1U", "datatypes_bools", SingleStoreDbType.UByte, "TINYINT", 3, typeof(byte), "N", -1, 0)]
	[InlineData("size", "datatypes_enums", SingleStoreDbType.Enum, "ENUM", 7, typeof(string), "N", -1, 0)]
	[InlineData("color", "datatypes_enums", SingleStoreDbType.Enum, "ENUM", 6, typeof(string), "", -1, 0)]
	[InlineData("char38", "datatypes_guids", SingleStoreDbType.String, "CHAR(38)", 38, typeof(string), "N", -1, 0)]
	[InlineData("char38bin", "datatypes_guids", SingleStoreDbType.String, "CHAR(38)", 38, typeof(string), "N", -1, 0)]
	[InlineData("text", "datatypes_guids", SingleStoreDbType.Text, "VARCHAR", 65535, typeof(string), "LN", -1, 0)]
	[InlineData("blob", "datatypes_guids", SingleStoreDbType.Blob, "BLOB", 65535, typeof(byte[]), "LN", -1, 0)]
	[InlineData("SByte", "datatypes_integers", SingleStoreDbType.Byte, "TINYINT", 4, typeof(sbyte), "N", -1, 0)]
	[InlineData("Byte", "datatypes_integers", SingleStoreDbType.UByte, "TINYINT", 3, typeof(byte), "N", -1, 0)]
	[InlineData("Int16", "datatypes_integers", SingleStoreDbType.Int16, "SMALLINT", 6, typeof(short), "N", -1, 0)]
	[InlineData("UInt16", "datatypes_integers", SingleStoreDbType.UInt16, "SMALLINT", 5, typeof(ushort), "N", -1, 0)]
	[InlineData("Int24", "datatypes_integers", SingleStoreDbType.Int24, "MEDIUMINT", 9, typeof(int), "N", -1, 0)]
	[InlineData("UInt24", "datatypes_integers", SingleStoreDbType.UInt24, "MEDIUMINT", 8, typeof(uint), "N", -1, 0)]
	[InlineData("Int32", "datatypes_integers", SingleStoreDbType.Int32, "INT", 11, typeof(int), "N", -1, 0)]
	[InlineData("UInt32", "datatypes_integers", SingleStoreDbType.UInt32, "INT", 10, typeof(uint), "N", -1, 0)]
	[InlineData("Int64", "datatypes_integers", SingleStoreDbType.Int64, "BIGINT", 20, typeof(long), "N", -1, 0)]
	[InlineData("UInt64", "datatypes_integers", SingleStoreDbType.UInt64, "BIGINT", 20, typeof(ulong), "N", -1, 0)]
	[InlineData("value", "datatypes_json_core", SingleStoreDbType.JSON, "JSON", int.MaxValue, typeof(string), "LN", -1, 0)]
	[InlineData("Single", "datatypes_reals", SingleStoreDbType.Float, "FLOAT", 12, typeof(float), "N", -1, 31)]
	[InlineData("Double", "datatypes_reals", SingleStoreDbType.Double, "DOUBLE", 22, typeof(double), "N", -1, 31)]
	[InlineData("SmallDecimal", "datatypes_reals", SingleStoreDbType.NewDecimal, "DECIMAL", 7, typeof(decimal), "N", 5, 2)]
	[InlineData("MediumDecimal", "datatypes_reals", SingleStoreDbType.NewDecimal, "DECIMAL", 30, typeof(decimal), "N", 28, 8)]
	[InlineData("BigDecimal", "datatypes_reals", SingleStoreDbType.NewDecimal, "DECIMAL", 52, typeof(decimal), "N", 50, 30)]
	[InlineData("value", "datatypes_set", SingleStoreDbType.Set, "SET", 12, typeof(string), "N", -1, 0)]
	[InlineData("utf8", "datatypes_strings", SingleStoreDbType.VarChar, "VARCHAR", 300, typeof(string), "N", -1, 0)]
	[InlineData("utf8bin", "datatypes_strings", SingleStoreDbType.VarChar, "VARCHAR", 300, typeof(string), "N", -1, 0)]
	[InlineData("guid", "datatypes_strings", SingleStoreDbType.Guid, "CHAR(36)", 36, typeof(Guid), "N", -1, 0)]
	[InlineData("guidbin", "datatypes_strings", SingleStoreDbType.Guid, "CHAR(36)", 36, typeof(Guid), "N", -1, 0)]
	[InlineData("Date", "datatypes_times", SingleStoreDbType.Date, "DATE", 10, typeof(DateTime), "N", -1, 0)]
	[InlineData("DateTime", "datatypes_times", SingleStoreDbType.DateTime, "DATETIME", 26, typeof(DateTime), "N", -1, 6)]
	[InlineData("Timestamp", "datatypes_times", SingleStoreDbType.Timestamp, "TIMESTAMP", 26, typeof(DateTime), "N", -1, 6)]
	[InlineData("Time", "datatypes_times", SingleStoreDbType.Time, "TIME", 18, typeof(TimeSpan), "N", -1, 6)]
	[InlineData("Year", "datatypes_times", SingleStoreDbType.Year, "YEAR", 4, typeof(int), "N", -1, 0)]
	[InlineData("Geography", "datatypes_geography", SingleStoreDbType.Geography, "GEOGRAPHY", 1073741823, typeof(string), "N", -1, 0)]
	[InlineData("Point", "datatypes_geography", SingleStoreDbType.GeographyPoint, "POINT", 48, typeof(string), "N", -1, 0)]
	[InlineData("LineString", "datatypes_geography", SingleStoreDbType.Geography, "GEOGRAPHY", 1073741823, typeof(string), "N", -1, 0)]
	[InlineData("Polygon", "datatypes_geography", SingleStoreDbType.Geography, "GEOGRAPHY", 1073741823, typeof(string), "N", -1, 0)]
	public void GetColumnSchema(string column, string table, SingleStoreDbType mySqlDbType, string dataTypeName, int columnSize, Type dataType, string flags, int precision, int scale)
	{
		// Disable the test for SingleStore versions earlier than 8.7 due to difference in metadata values for geography types
		if (mySqlDbType == SingleStoreDbType.Geography && Connection.Session.S2ServerVersion.Version < new Version(8, 7, 0))
		{
			return;
		}

		SingleStoreConnection connectionWithParam = null;
		if (mySqlDbType == SingleStoreDbType.GeographyPoint)
		{
			var csb = CreateConnectionStringBuilder();
			csb.TreatChar48AsGeographyPoint = true;
			connectionWithParam = new SingleStoreConnection(csb.ConnectionString);
			connectionWithParam.Open();
		}

		DoGetColumnSchema(column, table, mySqlDbType, dataTypeName, columnSize, dataType, flags, precision, scale, connection: connectionWithParam);
		if(connectionWithParam != null)
			connectionWithParam.Close();
	}

	private void DoGetColumnSchema(string column, string table, SingleStoreDbType mySqlDbType, string dataTypeName, int columnSize, Type dataType, string flags, int precision, int scale, SingleStoreConnection connection = null)
	{
		if (table == "datatypes_json_core" && !AppConfig.SupportsJson)
			return;

		connection = connection ?? Connection;
		var isAutoIncrement = flags.IndexOf('A') != -1;
		var isKey = flags.IndexOf('K') != -1;
		var isLong = flags.IndexOf('L') != -1;
		var allowDbNull = flags.IndexOf('N') != -1;
		var realPrecision = precision == -1 ? default(int?) : precision;

		using var command = connection.CreateCommand();
		command.CommandText = $"select `{column}` from `{table}`;";

		using var reader = command.ExecuteReader();
		var columns = reader.GetColumnSchema();
		Assert.Single(columns);
		var schema = (SingleStoreDbColumn) columns[0];
		Assert.Equal(allowDbNull, schema.AllowDBNull);
		Assert.Equal(column, schema.BaseColumnName);
		Assert.Equal(connection.Database, schema.BaseSchemaName);
		Assert.Equal(table, schema.BaseTableName);
		Assert.Equal(column, schema.ColumnName);
		Assert.Equal(0, schema.ColumnOrdinal);
		Assert.Equal(dataType, schema.DataType);
		Assert.Equal(dataTypeName, schema.DataTypeName);

		// the condition below accounts for wrong charset reported in SingleStore 7.5 and 7.6
		// when dealing with utf8mb4 data
 		if ( !( (column == "utf8" || column == "utf8bin") &&
			connection.Session.S2ServerVersion.Version.CompareTo(new Version(7,5,0)) > 0 &&
			connection.Session.S2ServerVersion.Version.CompareTo(new Version(7,8,0)) < 0 ))
		{
			// TODO: PLAT-6085: remove this if
			if (column != "Single" && column != "Double")
				Assert.Equal(columnSize, schema.ColumnSize);
		}
		Assert.False(schema.IsAliased.Value);
		Assert.Equal(isAutoIncrement, schema.IsAutoIncrement);
		Assert.False(schema.IsExpression.Value);
		Assert.False(schema.IsHidden.Value);

		// TODO: PLAT-6086: investigate rowid reported non-primary key but unique in columnstore
		if (column != "rowid")
			Assert.Equal(isKey, schema.IsKey);
		Assert.Equal(isLong, schema.IsLong);
		Assert.False(schema.IsReadOnly.Value);

		// TODO: PLAT-6086
		if (column != "rowid")
			Assert.False(schema.IsUnique.Value);
		Assert.Equal(realPrecision, schema.NumericPrecision);

		// TODO: PLAT-6087 remove this if
		if (column != "Date")
			Assert.Equal(scale, schema.NumericScale);
		Assert.Equal(mySqlDbType, schema.ProviderType);
	}
#endif

	[Theory]
	[InlineData("Bit1", "datatypes_bits", "BIT", typeof(ulong), 3, 1ul)]
	[InlineData("Bit32", "datatypes_bits", "BIT", typeof(ulong), 3, 1ul)]
	[InlineData("Bit64", "datatypes_bits", "BIT", typeof(ulong), 3, 1ul)]
	[InlineData("Binary", "datatypes_blobs", "BINARY(100)", typeof(byte[]), 2, null)]
	[InlineData("VarBinary", "datatypes_blobs", "VARBINARY(100)", typeof(byte[]), 2, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })]
	[InlineData("TinyBlob", "datatypes_blobs", "TINYBLOB", typeof(byte[]), 2, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })]
	[InlineData("Blob", "datatypes_blobs", "BLOB", typeof(byte[]), 2, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })]
	[InlineData("MediumBlob", "datatypes_blobs", "MEDIUMBLOB", typeof(byte[]), 2, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })]
	[InlineData("LongBlob", "datatypes_blobs", "LONGBLOB", typeof(byte[]), 2, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })]
	[InlineData("guidbin", "datatypes_blobs", "BINARY(16)", typeof(byte[]), 2, new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF })]
	[InlineData("Boolean", "datatypes_bools", "BOOL", typeof(sbyte), 3, true)]
	[InlineData("TinyInt1", "datatypes_bools", "TINYINT(1)", typeof(sbyte), 3, true)]
	[InlineData("TinyInt1U", "datatypes_bools", "TINYINT(1) UNSIGNED", typeof(byte), 3, (byte) 1)]
	[InlineData("char38", "datatypes_guids",  "CHAR(38)", typeof(string), 2, "0")]
	[InlineData("char38bin", "datatypes_guids", "CHAR(38)", typeof(string), 2, "0")]
	[InlineData("SByte", "datatypes_integers", "TINYINT", typeof(sbyte), 4, (sbyte) 127)]
	[InlineData("Byte", "datatypes_integers", "TINYINT UNSIGNED", typeof(byte), 4, (byte) 255)]
	[InlineData("Int16", "datatypes_integers", "SMALLINT", typeof(short), 4, (short) 32767)]
	[InlineData("UInt16", "datatypes_integers", "SMALLINT UNSIGNED", typeof(ushort), 4, (ushort) 65535)]
	[InlineData("Int24", "datatypes_integers", "MEDIUMINT", typeof(int), 4, 8388607)]
	[InlineData("UInt24", "datatypes_integers", "MEDIUMINT UNSIGNED", typeof(uint), 4, 16777215u)]
	[InlineData("Int32", "datatypes_integers", "INT", typeof(int), 4, 2147483647)]
	[InlineData("UInt32", "datatypes_integers", "INT UNSIGNED", typeof(uint), 4, 4294967295u)]
	[InlineData("Int64", "datatypes_integers", "BIGINT", typeof(long), 4, 9223372036854775807L)]
	[InlineData("UInt64", "datatypes_integers", "BIGINT UNSIGNED", typeof(ulong), 4, 18446744073709551615ul)]
	[InlineData("Single", "datatypes_reals", "FLOAT", typeof(float), 3, -3.40282e38f)]
	[InlineData("Double", "datatypes_reals", "DOUBLE", typeof(double), 3, -1.7976931348623157e308)]
	[InlineData("SmallDecimal", "datatypes_reals", "DECIMAL(5,2)", typeof(decimal), 3, null)]
	[InlineData("MediumDecimal", "datatypes_reals", "DECIMAL(28,8)", typeof(decimal), 3, null)]
	[InlineData("BigDecimal", "datatypes_reals", "DECIMAL(50,30)", typeof(decimal), 3, null)]
	[InlineData("utf8", "datatypes_strings", "VARCHAR(300)", typeof(string), 3, "ASCII")]
	[InlineData("Date", "datatypes_times", "DATE", typeof(DateTime), 2, null)]
	[InlineData("DateTime", "datatypes_times", "DATETIME", typeof(DateTime), 2, null)]
	[InlineData("Timestamp", "datatypes_times", "TIMESTAMP", typeof(DateTime), 2, null)]
	[InlineData("Time", "datatypes_times", "TIME", typeof(TimeSpan), 2, null)]
#if BASELINE
	[InlineData("Year", "datatypes_times", "YEAR", typeof(short), 2, (short) 1901)]
#else
	[InlineData("Year", "datatypes_times", "YEAR", typeof(int), 2, 1901)]
#endif
#if !BASELINE
	[InlineData("value", "datatypes_json_core", "JSON", typeof(string), 4, "[]")]
	[InlineData("Geography", "datatypes_geography", "GEOGRAPHY", typeof(string), 2, null)]
	[InlineData("Point", "datatypes_geography", "GEOGRAPHY", typeof(string), 2, null)]
#endif
	public void StoredProcedureParameter(string column, string table, string dataTypeName, Type dataType, int rowid, object expectedValue)
	{
		// We're skipping this test block when we're running tests on Managed Service due to the specifics of
		// how AUTO_INCREMENT works (https://docs.singlestore.com/cloud/reference/sql-reference/data-definition-language-ddl/create-table/#auto-increment-behavior)
		// We can't know 'rowid' beforehand, so therefore this test doesn't make any sense while running on Managed Service
		if ((table == "datatypes_json_core" && !AppConfig.SupportsJson) || AppConfig.ManagedService)
			return;

		using (var command = Connection.CreateCommand())
		{
			command.CommandText = $@"create or replace procedure sp_{column} (row_id INTEGER) as
declare
outparam {dataTypeName};
begin
ECHO SELECT `{column}` as Value FROM {table} WHERE rowid = row_id;
end;";
			command.ExecuteNonQuery();
		}

		using (var command = Connection.CreateCommand())
		{
			command.CommandText = $"sp_{column}";
			command.CommandType = CommandType.StoredProcedure;

			var parameter = command.CreateParameter();
			parameter.ParameterName = "row_id";
			parameter.Value = rowid;
			command.Parameters.Add(parameter);

			var reader = command.ExecuteReader();
			var result = reader.Read();
			Assert.True(result);
			var value = reader.GetValue(0);
			Assert.IsType(dataType, value);

			if (expectedValue is not null)
			{
				if (expectedValue.GetType() == typeof(bool))
					Assert.Equal(expectedValue, Convert.ToBoolean(value));
				else
					Assert.Equal(expectedValue, value);
			}
		}
	}

#if !BASELINE
	[Theory]
	[InlineData("Bit1", "datatypes_bits", "BIT(1)")]
	[InlineData("Bit32", "datatypes_bits", "BIT(32)")]
	[InlineData("Bit64", "datatypes_bits", "BIT(64)")]
	[InlineData("Binary", "datatypes_blobs", "BINARY(100)")]
	[InlineData("VarBinary", "datatypes_blobs", "VARBINARY(100)")]
	[InlineData("TinyBlob", "datatypes_blobs", "TINYBLOB")]
	[InlineData("Blob", "datatypes_blobs", "BLOB")]
	[InlineData("guidbin", "datatypes_blobs", "BINARY(16)")]
	[InlineData("Boolean", "datatypes_bools", "BOOL")]
	[InlineData("TinyInt1", "datatypes_bools", "TINYINT(1)")]
	[InlineData("TinyInt1U", "datatypes_bools", "TINYINT(1) UNSIGNED")]
	[InlineData("char38", "datatypes_guids", "CHAR(38)")]
	[InlineData("char38bin", "datatypes_guids", "CHAR(38)")]
	[InlineData("SByte", "datatypes_integers", "TINYINT")]
	[InlineData("Byte", "datatypes_integers", "TINYINT UNSIGNED")]
	[InlineData("Int16", "datatypes_integers", "SMALLINT")]
	[InlineData("UInt16", "datatypes_integers", "SMALLINT UNSIGNED")]
	[InlineData("Int24", "datatypes_integers", "MEDIUMINT")]
	[InlineData("UInt24", "datatypes_integers", "MEDIUMINT UNSIGNED")]
	[InlineData("Int32", "datatypes_integers", "INT")]
	[InlineData("UInt32", "datatypes_integers", "INT UNSIGNED")]
	[InlineData("Int64", "datatypes_integers", "BIGINT")]
	[InlineData("UInt64", "datatypes_integers", "BIGINT UNSIGNED")]
	[InlineData("Single", "datatypes_reals", "FLOAT")]
	[InlineData("Double", "datatypes_reals", "DOUBLE")]
	[InlineData("SmallDecimal", "datatypes_reals", "DECIMAL(5,2)")]
	[InlineData("MediumDecimal", "datatypes_reals", "DECIMAL(28,8)")]
	[InlineData("BigDecimal", "datatypes_reals", "DECIMAL(50,30)")]
	[InlineData("utf8", "datatypes_strings", "VARCHAR(300) CHARSET utf8mb4")]
	[InlineData("Date", "datatypes_times", "DATE")]
	[InlineData("DateTime", "datatypes_times", "DATETIME(6)")]
	[InlineData("Timestamp", "datatypes_times", "TIMESTAMP(6)")]
	[InlineData("Time", "datatypes_times", "TIME(6)")]
	// [InlineData("Year", "datatypes_times", "YEAR")]
	[InlineData("value", "datatypes_json_core", "JSON")]
	public void BulkCopyDataReader(string column, string table, string dataTypeName)
	{
		if (table == "datatypes_json_core" && !AppConfig.SupportsJson)
			return;

		using var connection1 = new SingleStoreConnection(AppConfig.ConnectionString);
		connection1.Open();

		// Starting with version 8.0, SingleStore has 'data_conversion_compatibility_level' variable that controls the way
		// certain data conversions are performed. SingleStoreDataReader will round values that are stored in 'BigDecimal' column.
		// For instance, a value like 99999999999999999999.999999999999999999999999999999 will be rounded to 100000000000000000000.00000000
		// However, this rounded value exceeds the storage capacity of a DECIMAL(50, 30) field, leading to an error,
		// because truncating values is prohibited by 'data_conversion_compatibility_level' parameter
		if (dataTypeName == "DECIMAL(50,30)" && connection1.Session.S2ServerVersion.Version.CompareTo(S2Versions.HasDataConversionCompatibilityLevelParameter) >= 0)
			return;

		var bulkCopyTable = "bulk_copy_" + table;
		using (var command = new SingleStoreCommand($@"drop table if exists `{bulkCopyTable}`; create table `{bulkCopyTable}`
(`{column}` {dataTypeName} NULL);", connection1))
		{
			command.ExecuteNonQuery();
		}

		using (var command = new SingleStoreCommand($@"select `{column}` from `{table}` order by rowid;", connection1))
		{
			using var reader = command.ExecuteReader();
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.AllowLoadLocalInfile = true;
			var bulkCopy = new SingleStoreBulkCopy(new SingleStoreConnection(csb.ConnectionString))
			{
				DestinationTableName = bulkCopyTable,
			};
			bulkCopy.WriteToServer(reader);
		}

		using var connection2 = new SingleStoreConnection(AppConfig.ConnectionString);
		connection2.Open();

		using (var command1 = new SingleStoreCommand($@"select `{column}` from `{table}` order by `{column}`;", connection1))
		using (var reader1 = command1.ExecuteReader())
		using (var command2 = new SingleStoreCommand($@"select `{column}` from `{bulkCopyTable}` order by `{column}`;", connection2))
		using (var reader2 = command2.ExecuteReader())
		{
			while (reader1.Read())
			{
				Assert.True(reader2.Read());
				if (reader1.IsDBNull(0))
					Assert.True(reader2.IsDBNull(0));
				else
					Assert.Equal(reader1.GetValue(0), reader2.GetValue(0));
			}
			Assert.False(reader2.Read());
		}
	}
#endif

	private static byte[] CreateByteArray(int size)
	{
		var data = new byte[size];
		var random = new Random(size);
		random.NextBytes(data);

		// ensure each byte value is used at least once
		for (int i = 0; i < Math.Min(255, size); i++)
			data[i] = (byte) i;

		return data;
	}

	[SkippableTheory(ServerFeatures.Json)]
	[InlineData(new object[] { new[] { null, "NULL", "BOOLEAN", "ARRAY", "ARRAY", "ARRAY", "DOUBLE", "DOUBLE", "OBJECT", "OBJECT" } })]
	public void JsonType(string[] expectedTypes)
	{
		var types = Connection.Query<string>(@"select UPPER(JSON_GET_TYPE(value)) from datatypes_json_core order by rowid;").ToList();
		for (int i = 0; i < expectedTypes.Length; ++i)
			Assert.Equal(expectedTypes[i], types[i]);
	}

	[SkippableTheory(ServerFeatures.Json)]
	[InlineData("value", new[] { null, "null", "true", "[]", "[0]", "[1]", "0", "1", "{}", "{\"a\":\"b\"}" })]
	public void QueryJson(string column, string[] expected)
	{
		string dataTypeName = "JSON";
#if BASELINE
		// mysql-connector-net returns "VARCHAR" for "JSON"
		dataTypeName = "VARCHAR";
#endif
		DoQuery("json_core", column, dataTypeName, expected, reader => reader.GetString(0), omitWhereTest: true);
	}

	[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=97067")]
	[InlineData(false, "MIN", 0)]
	[InlineData(false, "MAX", uint.MaxValue)]
	[InlineData(true, "MIN", 0)]
	[InlineData(true, "MAX", uint.MaxValue)]
	public void QueryAggregateBit(bool shouldPrepare, string aggregation, ulong expected)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();
		using var command = new SingleStoreCommand($@"SELECT {aggregation}(Bit32) FROM datatypes_bits;", connection);
		if (shouldPrepare)
			command.Prepare();
		var result = command.ExecuteScalar();
		Assert.Equal(expected, result);
	}

	private static byte[] GetBytes(DbDataReader reader)
	{
		var size = reader.GetBytes(0, 0, null, 0, 0);
		var result = new byte[size];
		reader.GetBytes(0, 0, result, 0, result.Length);
		return result;
	}

	private static byte[] GetStreamBytes(DbDataReader reader)
	{
		using var stream = reader.GetStream(0);
		Assert.True(stream.CanRead);
		Assert.False(stream.CanWrite);
		var bytes = new byte[stream.Length];
		Assert.Equal(bytes.Length, stream.Read(bytes, 0, bytes.Length));
		return bytes;
	}

	private void DoQuery(
		string table,
		string column,
		string dataTypeName,
		object[] expected,
		Func<SingleStoreDataReader, object> getValue,
		object baselineCoercedNullValue = null,
		bool omitWhereTest = false,
		bool omitWherePrepareTest = false,
		bool matchesDefaultType = true,
		SingleStoreConnection connection = null,
		Action<object, object> assertEqual = null,
		Type getFieldValueType = null,
		bool omitGetFieldValueTest = false)
	{
		DoQuery<GetValueWhenNullException>(table, column, dataTypeName, expected, getValue, baselineCoercedNullValue, omitWhereTest, omitWherePrepareTest, matchesDefaultType, connection, assertEqual, getFieldValueType, omitGetFieldValueTest);
	}

	// NOTE: baselineCoercedNullValue is to work around inconsistencies in mysql-connector-net; DBNull.Value will
	// be coerced to 0 by some reader.GetX() methods, but not others.
	private void DoQuery<TException>(
		string table,
		string column,
		string dataTypeName,
		object[] expected,
		Func<SingleStoreDataReader, object> getValue,
		object baselineCoercedNullValue = null,
		bool omitWhereTest = false,
		bool omitWherePrepareTest = false,
		bool matchesDefaultType = true,
		SingleStoreConnection connection = null,
		Action<object, object> assertEqual = null,
		Type getFieldValueType = null,
		bool omitGetFieldValueTest = false)
		where TException : Exception
	{
		connection = connection ?? Connection;
		assertEqual = assertEqual ?? Assert.Equal;
		using var cmd = connection.CreateCommand();
		cmd.CommandText = $"select {column} from datatypes_{table} order by rowid";
		using (var reader = cmd.ExecuteReader())
		{
			Assert.Equal(dataTypeName, reader.GetDataTypeName(0));
			foreach (var value in expected)
			{
				Assert.True(reader.Read());
				if (value is null)
				{
					Assert.Equal(DBNull.Value, reader.GetValue(0));
#if BASELINE
					if (baselineCoercedNullValue is not null)
						Assert.Equal(baselineCoercedNullValue, getValue(reader));
					else
						Assert.Throws<TException>(() => getValue(reader));
#else
					Assert.Throws<TException>(() => getValue(reader));
#endif
				}
				else
				{
					assertEqual(value, getValue(reader));

					// test `reader.GetValue` and `reader.GetFieldType` if value matches default type
					if (matchesDefaultType)
					{
						// for bool we store tinyint
						if (value.GetType() == typeof(bool))
						{
							assertEqual(value, Convert.ToBoolean(reader.GetValue(0)));
						}
						else
						{
							assertEqual(value, reader.GetValue(0));
							Assert.Equal(value.GetType(), reader.GetFieldType(0));
							Assert.Equal(value.GetType(), reader.GetFieldType(column.Replace("`", "")));
						}
					}

					if (!omitGetFieldValueTest)
					{
						// test `reader.GetFieldValue<value.GetType()>`
						var syncMethod = typeof(SingleStoreDataReader)
							.GetMethod("GetFieldValue")
							.MakeGenericMethod(getFieldValueType ?? value.GetType());
						assertEqual(value, syncMethod.Invoke(reader, new object[] { 0 }));

						// test `reader.GetFieldValueAsync<value.GetType()>`
						var asyncMethod = typeof(SingleStoreDataReader)
							.GetMethod("GetFieldValueAsync", new[] { typeof(int) })
							.MakeGenericMethod(getFieldValueType ?? value.GetType());
						var asyncMethodValue = asyncMethod.Invoke(reader, new object[] { 0 });
						var asyncMethodGetAwaiter = asyncMethodValue.GetType()
							.GetMethod("GetAwaiter");
						var asyncMethodGetAwaiterValue = asyncMethodGetAwaiter.Invoke(asyncMethodValue, new object[] { });
						var asyncMethodGetResult = asyncMethodGetAwaiterValue.GetType()
							.GetMethod("GetResult");
						var asyncMethodGetResultValue = asyncMethodGetResult.Invoke(asyncMethodGetAwaiterValue, new object[] { });
						assertEqual(value, asyncMethodGetResultValue);
					}
				}
			}
			Assert.False(reader.Read());
			Assert.False(reader.NextResult());
		}

		// We're adding this assert, because SingleStore's AUTO_INCREMENT only guarantees that automatically-generated values are unique
		string countTotalRows = $"SELECT COUNT(*) FROM datatypes_{table}";
		string countUniqueRowIds = $"SELECT COUNT(DISTINCT rowid) FROM datatypes_{table}";

		using (var command = new SingleStoreCommand(countTotalRows, connection))
		{
			var totalRows = Convert.ToInt32(command.ExecuteScalar());

			cmd.CommandText = countUniqueRowIds;
			var uniqueRowIds = Convert.ToInt32(command.ExecuteScalar());
			Assert.Equal(totalRows, uniqueRowIds);
		}
	}

#if NET6_0_OR_GREATER
	private static object[] ConvertToDateOnly(object[] input)
	{
		var output = new object[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			var value = SplitAndParse(input[i]);
			if (value?.Length == 3)
				output[i] = new DateOnly(value[0], value[1], value[2]);
			else if (value is not null)
				throw new NotSupportedException("Can't convert to DateOnly");
		}
		return output;
	}
#endif

	private static object[] ConvertToDateTime(object[] input, DateTimeKind kind)
	{
		var output = new object[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			var value = SplitAndParse(input[i]);
			if (value?.Length == 3)
				output[i] = new DateTime(value[0], value[1], value[2], 0, 0, 0, kind);
			else if (value?.Length == 6)
				output[i] = new DateTime(value[0], value[1], value[2], value[3], value[4], value[5], kind);
			else if (value?.Length == 7)
				output[i] = new DateTime(value[0], value[1], value[2], value[3], value[4], value[5], value[6] / 1000, kind).AddTicks(value[6] % 1000 * 10);
		}
		return output;
	}

	private static object[] ConvertToDateTimeOffset(object[] input)
	{
		var output = new object[input.Length];
		var dateTimes = ConvertToDateTime(input, DateTimeKind.Utc);
		for (int i = 0; i < dateTimes.Length; i++)
		{
			if (dateTimes[i] is DateTime dateTime)
				output[i] = new DateTimeOffset(dateTime);
		}
		return output;
	}

#if NET6_0_OR_GREATER
	private static object[] ConvertToTimeOnly(object[] input)
	{
		var output = new object[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			var value = SplitAndParse(input[i]);
			if (value?.Length == 3)
			{
				output[i] = new TimeOnly(value[0], value[1], value[2]);
			}
			else if (value?.Length == 5)
			{
				Assert.Equal(0, value[0]);
				output[i] = new TimeOnly(value[1], value[2], value[3], value[4] / 1000).Add(TimeSpan.FromTicks(value[4] % 1000 * 10));
			}
		}
		return output;
	}
#endif

	private static object[] ConvertToTimeSpan(object[] input)
	{
		var output = new object[input.Length];
		for (int i = 0; i < input.Length; i++)
		{
			var value = SplitAndParse(input[i]);
			if (value?.Length == 3)
				output[i] = new TimeSpan(value[0], value[1], value[2]);
			else if (value?.Length == 5)
				output[i] = new TimeSpan(value[0], value[1], value[2], value[3], value[4] / 1000) + TimeSpan.FromTicks(value[4] % 1000 * 10);
		}
		return output;
	}

	private static int[] SplitAndParse(object obj)
	{
		if (obj is not string value)
			return null;

		var split = value.Split();
		var output = new int[split.Length];
		for (int i = 0; i < split.Length; i++)
			output[i] = int.Parse(split[i], CultureInfo.InvariantCulture);
		return output;
	}

	private SingleStoreConnection Connection { get; }

	private SingleStoreConnectionStringBuilder CreateConnectionStringBuilder() => new SingleStoreConnectionStringBuilder(AppConfig.ConnectionString);
}

