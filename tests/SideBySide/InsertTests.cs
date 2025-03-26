using System.Globalization;
using System.Numerics;
#if BASELINE
using MySql.Data.Types;
#endif

namespace SideBySide;

public class InsertTests : IClassFixture<DatabaseFixture>
{
	public InsertTests(DatabaseFixture database)
	{
		m_database = database;
	}

	[Fact]
	public async Task LastInsertedIdNegative()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment);");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new SingleStoreCommand("INSERT INTO insert_ai(rowid) VALUES (@rowid);", m_database.Connection);
			command.Parameters.AddWithValue("@rowid", -1);
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(0, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdInsertIgnore()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);
");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new SingleStoreCommand(@"INSERT IGNORE INTO insert_ai (rowid, text) VALUES (2, 'test');", m_database.Connection);
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(2L, command.LastInsertedId);
			Assert.Equal(0, await command.ExecuteNonQueryAsync());
			Assert.Equal(0L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task RowsAffected()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_rows_affected;
create table insert_rows_affected(id integer not null primary key auto_increment, value text null);");

		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new SingleStoreCommand(@"
INSERT INTO insert_rows_affected (value) VALUES (null);
INSERT INTO insert_rows_affected (value) VALUES (null);", m_database.Connection);
			var rowsAffected = await command.ExecuteNonQueryAsync();
			Assert.Equal(2, rowsAffected);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Theory]
	[InlineData(0)]
	[InlineData(6)]
	public void InsertTime(int precision)
	{
		m_database.Connection.Execute($@"drop table if exists insert_time;
create table insert_time(value TIME({precision}));");

		try
		{
			m_database.Connection.Open();
			using (var command = new SingleStoreCommand("INSERT INTO insert_time (value) VALUES (@Value);", m_database.Connection))
			{
				command.Parameters.Add(new() { ParameterName = "@value", Value = TimeSpan.FromMilliseconds(10) });
				command.ExecuteNonQuery();
			}

			using (var command = new SingleStoreCommand("SELECT value FROM insert_time;", m_database.Connection))
			using (var reader = command.ExecuteReader())
			{
				Assert.True(reader.Read());
				if(precision == 0)
					Assert.Equal(TimeSpan.Zero, reader.GetValue(0));
				else
					Assert.Equal(TimeSpan.FromMilliseconds(10), reader.GetValue(0));
				Assert.False(reader.Read());
			}
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=73788")]
	public void InsertDateTimeOffset()
	{
		m_database.Connection.Execute(@"drop table if exists insert_datetimeoffset;
create table insert_datetimeoffset(rowid integer not null primary key auto_increment, datetimeoffset1 datetime null);");
		var value = new DateTimeOffsetValues { datetimeoffset1 = new DateTimeOffset(2017, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(678)) };

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_datetimeoffset(datetimeoffset1) values(@datetimeoffset1);";
			cmd.Parameters.Add(new()
			{
				ParameterName = "@datetimeoffset1",
				DbType = DbType.DateTimeOffset,
				Value = value.datetimeoffset1
			});
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		var datetime = m_database.Connection.ExecuteScalar<DateTime>(@"select datetimeoffset1 from insert_datetimeoffset order by rowid;");

		DateTime.SpecifyKind(datetime, DateTimeKind.Utc);

		Assert.Equal(value.datetimeoffset1.Value.UtcDateTime, datetime);
	}

	[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=91199")]
	public void InsertSingleStoreDateTime()
	{
		m_database.Connection.Execute(@"drop table if exists insert_mysqldatetime;
create table insert_mysqldatetime(rowid integer not null primary key auto_increment, ts timestamp(6) null);");
		var value = new DateTimeOffsetValues { datetimeoffset1 = new DateTimeOffset(2017, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(678)) };

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_mysqldatetime(ts) values(@ts);";
			cmd.Parameters.AddWithValue("@ts", new SingleStoreDateTime(2018, 6, 9, 12, 34, 56, 123456));
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		var datetime = m_database.Connection.ExecuteScalar<DateTime>(@"select ts from insert_mysqldatetime order by rowid;");
		Assert.Equal(new DateTime(2018, 6, 9, 12, 34, 56, 123).AddTicks(4560), datetime);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertGeography(bool prepare)
	{
		m_database.Connection.Execute(@"drop table if exists insert_singlestoregeography;
create rowstore table insert_singlestoregeography(rowid integer not null primary key auto_increment, shape geography not null);");

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_singlestoregeography(shape) values(@shape);";
			cmd.Parameters.AddWithValue("@shape", new SingleStoreGeography("POLYGON((3 3,4 3,4 4,3 4,3 3))"));
			if(prepare)
				cmd.Prepare();
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		using var reader = m_database.Connection.ExecuteReader(@"select shape from insert_singlestoregeography order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal("POLYGON((3.00000000 3.00000000, 4.00000000 3.00000000, 4.00000000 4.00000000, 3.00000000 4.00000000, 3.00000000 3.00000000))", reader.GetValue(0));
	}

	[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=102593")]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertMemoryStream(bool prepare)
	{
		m_database.Connection.Execute(@"drop table if exists insert_stream;
create table insert_stream(rowid integer not null primary key auto_increment, str text, blb blob);");

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_stream(str, blb) values(@str, @blb);";
			cmd.Parameters.AddWithValue("@str", new MemoryStream(new byte[] { 97, 98, 99, 100 }));
			cmd.Parameters.AddWithValue("@blb", new MemoryStream(new byte[] { 97, 98, 99, 100 }, 0, 4, false, true));
			if (prepare)
				cmd.Prepare();
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		using var reader = m_database.Connection.ExecuteReader(@"select str, blb from insert_stream order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal("abcd", reader.GetValue(0));
		Assert.Equal(new byte[] { 97, 98, 99, 100 }, reader.GetValue(1));
	}

	[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=103819")]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertStringBuilder(bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();

		Version utf8mb4_binSupportVersion = new(7, 5, 0);
		if (connection.Session.S2ServerVersion.Version.CompareTo(utf8mb4_binSupportVersion) < 0)
			return;

		connection.Execute(@"drop table if exists insert_string_builder;
create table insert_string_builder(rowid integer not null primary key auto_increment, str text collate utf8mb4_bin);");

		var value = new StringBuilder("\aAB\\12'ab\\'\\'");
		for (var i = 0; i < 100; i++)
			value.Append("\U0001F600\uD800\'\U0001F601\uD800");

		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_string_builder(str) values(@str);";
		cmd.Parameters.AddWithValue("@str", value);
		if (prepare)
			cmd.Prepare();
		Assert.Equal(1, cmd.ExecuteNonQuery());

		using var reader = connection.ExecuteReader(@"select str from insert_string_builder order by rowid;");
		Assert.True(reader.Read());

		// all unpaired high-surrogates will be converted to the Unicode Replacement Character when converted to UTF-8 to be transmitted to the server
		var expected = value.ToString().Replace('\uD800', '\uFFFD');
		Assert.Equal(expected, reader.GetValue(0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertBigInteger(bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_big_integer;
create table insert_big_integer(rowid integer not null primary key auto_increment, value bigint);");

		var value = 1_000_000_000_000_000L;
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_big_integer(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new BigInteger(value));
		if (prepare)
			cmd.Prepare();
		Assert.Equal(1, cmd.ExecuteNonQuery());

		using var reader = connection.ExecuteReader(@"select value from insert_big_integer order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal(value, reader.GetValue(0));
	}

#if !BASELINE
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertSingleStoreDecimal(bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_decimal;
			create table insert_mysql_decimal(rowid integer not null primary key auto_increment, value decimal(65,0));");

		string value = "22";
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_mysql_decimal(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new SingleStoreDecimal(value));
		if (prepare)
			cmd.Prepare();
		cmd.ExecuteNonQuery();

		using var reader = connection.ExecuteReader(@"select value from insert_mysql_decimal order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal(value, reader.GetValue(0).ToString());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertSingleStoreDecimalAsDecimal(bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_decimal;
			create table insert_mysql_decimal(rowid integer not null primary key auto_increment, value decimal(65, 30));");

		string value = "-123456789012345678901234.01234";
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_mysql_decimal(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new SingleStoreDecimal(value));
		if (prepare)
			cmd.Prepare();
		Assert.Equal(1, cmd.ExecuteNonQuery());

		using var reader = connection.ExecuteReader(@"select value from insert_mysql_decimal order by rowid;");
		Assert.True(reader.Read());
		var val = ((decimal) reader.GetValue(0)).ToString(CultureInfo.InvariantCulture);
		Assert.Equal(value, val);
	}
#endif

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReadSingleStoreDecimalUsingReader(bool prepare)
	{
		using SingleStoreConnection connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_decimal;
			create table insert_mysql_decimal(rowid integer not null primary key auto_increment, value decimal(65, 30));");

		string value = "-12345678901234567890123456789012345.012345678901234567890123456789";
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_mysql_decimal(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", value);
		Assert.Equal(1, cmd.ExecuteNonQuery());

		cmd.CommandText = @"select value from insert_mysql_decimal order by rowid;";
		if (prepare)
			cmd.Prepare();
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		var val = reader.GetSingleStoreDecimal("value");
		Assert.Equal(value, val.ToString());

#if !BASELINE
		val = reader.GetFieldValue<SingleStoreDecimal>(0);
		Assert.Equal(value, val.ToString());
#endif

		// value is too large to read as a regular decimal
#if BASELINE
		Assert.Throws<OverflowException>(() => reader.GetValue(0));
#else
		Assert.Throws<FormatException>(() => reader.GetValue(0));
#endif
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertBigIntegerAsDecimal(bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_big_integer;
create table insert_big_integer(rowid integer not null primary key auto_increment, value decimal(40, 2));");

		var value = long.MaxValue * 1000m;
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_big_integer(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new BigInteger(value));
		if (prepare)
			cmd.Prepare();
		cmd.ExecuteNonQuery();

		using var reader = connection.ExecuteReader(@"select value from insert_big_integer order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal(value, reader.GetValue(0));
	}

	[Fact]
	public void InsertOldGuid()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.OldGuids = true;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists old_guids;
create table old_guids(id integer not null primary key auto_increment, guid binary(16) null);");

		var guid = new Guid(1, 2, 3, 0x27, 0x5C, 0x7B, 0x7D, 0x22, 0x25, 0x26, 0x2C);

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = @"insert into old_guids(guid) values(@guid)";
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = "@guid";
			parameter.Value = guid;
			cmd.Parameters.Add(parameter);
			cmd.ExecuteNonQuery();
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = @"select guid from old_guids;";
			var selected = (Guid) cmd.ExecuteScalar();
			Assert.Equal(guid, selected);
		}
	}

	[Fact]
	public void InsertEnumValue()
	{
		m_database.Connection.Execute(@"drop table if exists insert_enum_value;
create table insert_enum_value(rowid integer not null primary key auto_increment, Enum16 int null, Enum32 int null, Enum64 bigint null);");
		m_database.Connection.Execute(@"insert into insert_enum_value(Enum16, Enum32, Enum64) values(@e16a, @e32a, @e64a), (@e16b, @e32b, @e64b);",
			new { e16a = default(Enum16?), e32a = default(Enum32?), e64a = default(Enum64?), e16b = Enum16.On, e32b = Enum32.Off, e64b = Enum64.On });
		var results = m_database.Connection.Query<EnumValues>(@"select Enum16, Enum32, Enum64 from insert_enum_value order by rowid;").ToList();
		Assert.Equal(2, results.Count);
		Assert.Null(results[0].Enum16);
		Assert.Null(results[0].Enum32);
		Assert.Null(results[0].Enum64);
		Assert.Equal(Enum16.On, results[1].Enum16);
		Assert.Equal(Enum32.Off, results[1].Enum32);
		Assert.Equal(Enum64.On, results[1].Enum64);
	}

	[Fact]
	public async Task EnumParametersAreParsedCorrectly()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_enum_value2;
create table insert_enum_value2(rowid integer not null primary key auto_increment, `Varchar` varchar(10), `String` varchar(10), `Int` int null);");

		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new SingleStoreCommand("INSERT INTO insert_enum_value2 (`Varchar`, `String`, `Int`) VALUES (@Varchar, @String, @Int);", m_database.Connection);
			command.Parameters.Add(new("@String", SingleStoreColor.Orange)).SingleStoreDbType = SingleStoreDbType.String;
			command.Parameters.Add(new("@Varchar", SingleStoreColor.Green)).SingleStoreDbType = SingleStoreDbType.VarChar;
			command.Parameters.Add(new("@Int", SingleStoreColor.None));

			await command.ExecuteNonQueryAsync();
			var result = (await m_database.Connection.QueryAsync<ColorEnumValues>(@"select `Varchar`, `String`, `Int` from insert_enum_value2;")).ToArray();
			Assert.Single(result);
			Assert.Equal(SingleStoreColor.Orange.ToString("G"), result[0].String);
			Assert.Equal(SingleStoreColor.Green.ToString("G"), result[0].Varchar);
			Assert.Equal((int) SingleStoreColor.None, result[0].Int);
		}
		finally
		{
			m_database.Connection.Close();
		}

	}

	enum Enum16 : short
	{
		Off,
		On,
	}

	enum Enum32 : int
	{
		Off,
		On,
	}

	enum Enum64 : long
	{
		Off,
		On,
	}

	class DateTimeOffsetValues
	{
		public DateTimeOffset? datetimeoffset1 { get; set; }
	}

	class ColorEnumValues
	{
		public string Varchar { get; set; }
		public string String { get; set; }
		public int Int { get; set; }
	}

	class EnumValues
	{
		public Enum16? Enum16 { get; set; }
		public Enum32? Enum32 { get; set; }
		public Enum64? Enum64 { get; set; }
	}

	[Fact]
	public void InsertSingleStoreEnum()
	{
		m_database.Connection.Execute(@"drop table if exists insert_mysql_enums;
create table insert_mysql_enums(
	rowid integer not null primary key auto_increment,
	size enum('x-small', 'small', 'medium', 'large', 'x-large'),
	color enum('red', 'orange', 'yellow', 'green', 'blue', 'indigo', 'violet') not null
);");
		m_database.Connection.Execute(@"insert into insert_mysql_enums(size, color) values(@size, @color);", new { size = SingleStoreSize.Large, color = SingleStoreColor.Blue });
		Assert.Equal(new[] { "large" }, m_database.Connection.Query<string>(@"select size from insert_mysql_enums"));
		Assert.Equal(new[] { "blue" }, m_database.Connection.Query<string>(@"select color from insert_mysql_enums"));
	}

	enum SingleStoreSize
	{
		None,
		XSmall,
		Small,
		Medium,
		Large,
		XLarge
	}

	enum SingleStoreColor
	{
		None,
		Red,
		Orange,
		Yellow,
		Green,
		Blue,
		Indigo,
		Violet
	}

	[Fact]
	public void InsertSingleStoreSet()
	{
		m_database.Connection.Execute(@"drop table if exists insert_mysql_set;
create table insert_mysql_set(
	rowid integer not null primary key auto_increment,
	value set('""one""', '""two""', '""four""', '""eight""') null
);");
		m_database.Connection.Execute(@"insert into insert_mysql_set(value) values('""one""'), ('""two""'), ('""one"",""two""'), ('""four""'), ('""four"",""one""'), ('""four"",""two""'), ('""four"",""two"",""one""'), ('""eight""');");
		Assert.Equal(new[] { "\"one\"", "\"one\",\"two\"", "\"one\",\"four\"", "\"one\",\"two\",\"four\"" }, m_database.Connection.Query<string>(@"select value from insert_mysql_set where JSON_ARRAY_CONTAINS_STRING(concat('[', value, ']'), 'one') order by rowid"));
	}


#if !BASELINE
	[Theory]
	[MemberData(nameof(GetBlobs))]
	public void InsertBlob(object data, bool prepare)
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_blob;
create table insert_mysql_blob(
rowid integer not null primary key auto_increment,
value mediumblob null
);");

		using (var cmd = new SingleStoreCommand("insert into insert_mysql_blob(value) values(@data);", connection))
		{
			cmd.Parameters.AddWithValue("@data", data);
			if (prepare)
				cmd.Prepare();
			cmd.ExecuteNonQuery();
		}
		Assert.Equal(new byte[] { 1, 0, 2, 39, 3, 92, 4, 34, 5, 6  }, connection.Query<byte[]>(@"select value from insert_mysql_blob;").Single());
	}

	public static IEnumerable<object[]> GetBlobs()
	{
		foreach (var blob in new object[]
		{
			new byte[] { 1, 0, 2, 39, 3, 92, 4, 34, 5, 6 },
			new ReadOnlyMemory<byte>(new byte[] { 0, 1, 0, 2, 39, 3, 92, 4, 34, 5, 6, 7, 8 }, 1, 10),
			new Memory<byte>(new byte[] { 0, 1, 0, 2, 39, 3, 92, 4, 34, 5, 6, 7, 8 }, 1, 10),
			new ArraySegment<byte>(new byte[] { 0, 1, 0, 2, 39, 3, 92, 4, 34, 5, 6, 7, 8 }, 1, 10),
		})
		{
			yield return new[] { blob, false };
			yield return new[] { blob, true };
		}
	}
#endif

	readonly DatabaseFixture m_database;
}
