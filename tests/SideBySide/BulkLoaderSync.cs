using SingleStoreConnector.Core;
using Xunit.Sdk;

namespace SideBySide;

[Collection("BulkLoaderCollection")]
public class BulkLoaderSync : IClassFixture<DatabaseFixture>
{
	public BulkLoaderSync(DatabaseFixture database)
	{
		m_testTable = "BulkLoaderSyncTest";
		var initializeTable = $@"
			drop table if exists {m_testTable};
			create table {m_testTable}
			(
				one int primary key
				, ignore_one int
				, two varchar(200)
				, ignore_two varchar(200)
				, three varchar(200)
				, four datetime
				, five blob
			) CHARACTER SET = UTF8;";
		database.Connection.Execute(initializeTable);

		m_memoryStreamBytes = System.Text.Encoding.UTF8.GetBytes(@"1,'two-1','three-1'
2,'two-2','three-2'
3,'two-3','three-3'
4,'two-4','three-4'
5,'two-5','three-5'
");
	}

#if !BASELINE
	[Fact]
	public void FileNameAndSourceStream()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		var bl = new SingleStoreBulkLoader(connection)
		{
			FileName = "test.dat",
			SourceStream = new MemoryStream(),
			TableName = m_testTable,
		};
		Assert.Throws<InvalidOperationException>(() => bl.Load());
	}
#endif

	[SkippableFact(ConfigSettings.TsvFile)]
	public void BulkLoadTsvFile()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderTsvFile;
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
		int rowCount = bl.Load();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalTsvFile)]
	public void BulkLoadLocalTsvFile()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderLocalTsvFile;
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = true;
		int rowCount = bl.Load();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.CsvFile)]
	public void BulkLoadCsvFile()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderCsvFile;
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
		int rowCount = bl.Load();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadLocalCsvFile()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile;
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = true;
		int rowCount = bl.Load();
		Assert.Equal(20, rowCount);
	}

	[Fact]
	public void BulkLoadCsvFileNotFound()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		var secureFilePath = connection.ExecuteScalar<string>(@"select @@global.secure_file_priv;");
		if (string.IsNullOrEmpty(secureFilePath) || secureFilePath == "NULL")
			return;

		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = Path.Combine(secureFilePath, AppConfig.SingleStoreBulkLoaderCsvFile + "-junk");
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
		try
		{
			int rowCount = bl.Load();
		}
		catch (Exception exception)
		{
			while (exception.InnerException is not null)
				exception = exception.InnerException;

			if (exception is not FileNotFoundException)
			{
				try
				{
					Assert.Contains("Errcode: 2 ", exception.Message, StringComparison.OrdinalIgnoreCase);
				}
				catch (ContainsException)
				{
					Assert.Contains("OS errno 2 ", exception.Message, StringComparison.OrdinalIgnoreCase);
				}
				Assert.Contains("No such file or directory", exception.Message);
			}
		}
	}

	[Fact]
	public void BulkLoadLocalCsvFileNotFound()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.Timeout = 3; //Set a short timeout for this test because the file not found exception takes a long time otherwise, the timeout does not change the result
		bl.FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile + "-junk";
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = true;
		try
		{
			int rowCount = bl.Load();
		}
		catch (SingleStoreException mySqlException)
		{
			while (mySqlException.InnerException is not null)
			{
				if (mySqlException.InnerException is SingleStoreException innerException)
				{
					mySqlException = innerException;
				}
				else
				{
					Assert.IsType<System.IO.FileNotFoundException>(mySqlException.InnerException);
					break;
				}
			}
			if (mySqlException.InnerException is null)
			{
				Assert.IsType<System.IO.FileNotFoundException>(mySqlException);
			}
		}
		catch (Exception exception)
		{
			//We know that the exception is not a SingleStoreException, just use the assertion to fail the test
			Assert.IsType<SingleStoreException>(exception);
		}
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadLocalCsvFileInTransactionWithCommit()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var transaction = connection.BeginTransaction())
		{
			var bulkLoader = new SingleStoreBulkLoader(connection)
			{
				FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile,
				TableName = m_testTable,
				CharacterSet = "UTF8",
				NumberOfLinesToSkip = 1,
				FieldTerminator = ",",
				FieldQuotationCharacter = '"',
				FieldQuotationOptional = true,
				Local = true,
			};
			bulkLoader.Expressions.Add("five = UNHEX(five)");
			bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

			var rowCount = bulkLoader.Load();
			Assert.Equal(20, rowCount);

			transaction.Commit();
		}

		Assert.Equal(20, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadLocalCsvFileBeforeTransactionWithCommit()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		var bulkLoader = new SingleStoreBulkLoader(connection)
		{
			FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile,
			TableName = m_testTable,
			CharacterSet = "UTF8",
			NumberOfLinesToSkip = 1,
			FieldTerminator = ",",
			FieldQuotationCharacter = '"',
			FieldQuotationOptional = true,
			Local = true,
		};
		bulkLoader.Expressions.Add("five = UNHEX(five)");
		bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

		using (var transaction = connection.BeginTransaction())
		{
			var rowCount = bulkLoader.Load();
			Assert.Equal(20, rowCount);

			transaction.Commit();
		}

		Assert.Equal(20, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadLocalCsvFileInTransactionWithRollback()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var transaction = connection.BeginTransaction())
		{
			var bulkLoader = new SingleStoreBulkLoader(connection)
			{
				FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile,
				TableName = m_testTable,
				CharacterSet = "UTF8",
				NumberOfLinesToSkip = 1,
				FieldTerminator = ",",
				FieldQuotationCharacter = '"',
				FieldQuotationOptional = true,
				Local = true,
			};
			bulkLoader.Expressions.Add("five = UNHEX(five)");
			bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

			var rowCount = bulkLoader.Load();
			Assert.Equal(20, rowCount);

			transaction.Rollback();
		}

		Assert.Equal(0, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadLocalCsvFileBeforeTransactionWithRollback()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		var bulkLoader = new SingleStoreBulkLoader(connection)
		{
			FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile,
			TableName = m_testTable,
			CharacterSet = "UTF8",
			NumberOfLinesToSkip = 1,
			FieldTerminator = ",",
			FieldQuotationCharacter = '"',
			FieldQuotationOptional = true,
			Local = true,
		};
		bulkLoader.Expressions.Add("five = UNHEX(five)");
		bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

		using (var transaction = connection.BeginTransaction())
		{
			var rowCount = bulkLoader.Load();
			Assert.Equal(20, rowCount);

			transaction.Rollback();
		}

		Assert.Equal(0, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
	}

	[Fact]
	public void BulkLoadMissingFileName()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
#if BASELINE
		Assert.Throws<SingleStoreException>(() => { var rowCount = bl.Load(); });
#else
		Assert.Throws<InvalidOperationException>(() => { var rowCount = bl.Load(); });
#endif
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadMissingTableName()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderLocalCsvFile;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
#if BASELINE
		Assert.Throws<SingleStoreException>(() =>
		{
			int rowCount = bl.Load();
		});
#else
		Assert.Throws<System.InvalidOperationException>(() =>
		{
			int rowCount = bl.Load();
		});
#endif
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadFileStreamInvalidOperation()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var fileStream = new FileStream(AppConfig.SingleStoreBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
#if !BASELINE
		bl.SourceStream = fileStream;
#endif
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
#if !BASELINE
		Assert.Throws<InvalidOperationException>(() => { int rowCount = bl.Load(); });
#else
		Assert.Throws<SingleStoreException>(() => { int rowCount = bl.Load(fileStream); });
#endif
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public void BulkLoadLocalFileStream()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var fileStream = new FileStream(AppConfig.SingleStoreBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
#if !BASELINE
		bl.SourceStream = fileStream;
#endif
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = true;
#if !BASELINE
		int rowCount = bl.Load();
#else
		int rowCount = bl.Load(fileStream);
#endif
		Assert.Equal(20, rowCount);
	}

	[Fact]
	public void BulkLoadMemoryStreamInvalidOperation()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var memoryStream = new MemoryStream(m_memoryStreamBytes, false);
#if !BASELINE
		bl.SourceStream = memoryStream;
#endif
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three" });
		bl.NumberOfLinesToSkip = 0;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Local = false;
#if !BASELINE
		Assert.Throws<InvalidOperationException>(() => bl.Load());
#else
		Assert.Throws<MySqlException>(() => bl.Load(memoryStream));
#endif
	}

	[Fact]
	public void BulkLoadLocalMemoryStream()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var memoryStream = new MemoryStream(m_memoryStreamBytes, false);
#if !BASELINE
		bl.SourceStream = memoryStream;
#endif
		bl.TableName = m_testTable;
		bl.CharacterSet = "UTF8";
		bl.Columns.AddRange(new string[] { "one", "two", "three" });
		bl.NumberOfLinesToSkip = 0;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Local = true;
#if !BASELINE
		int rowCount = bl.Load();
#else
		int rowCount = bl.Load(memoryStream);
#endif
		Assert.Equal(5, rowCount);
	}

#if !BASELINE
	[Fact]
	public void BulkCopyDataReader()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		using var connection2 = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		connection2.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_reader_source;
drop table if exists bulk_load_data_reader_destination;
create table bulk_load_data_reader_source(value int, name text);
create table bulk_load_data_reader_destination(value int, name text);
insert into bulk_load_data_reader_source values(0, 'zero'),(1,'one'),(2,'two'),(3,'three'),(4,'four'),(5,'five'),(6,'six');", connection))
		{
			cmd.ExecuteNonQuery();
		}

		using (var cmd = new SingleStoreCommand("select * from bulk_load_data_reader_source;", connection))
		using (var reader = cmd.ExecuteReader())
		{
			var bulkCopy = new SingleStoreBulkCopy(connection2) { DestinationTableName = "bulk_load_data_reader_destination", };
			var result = bulkCopy.WriteToServer(reader);
			Assert.Equal(7, result.RowsInserted);
			Assert.Empty(result.Warnings);
		}

		using var cmd1 = new SingleStoreCommand("select * from bulk_load_data_reader_source order by value;", connection);
		using var cmd2 = new SingleStoreCommand("select * from bulk_load_data_reader_destination order by value;", connection2);
		using var reader1 = cmd1.ExecuteReader();
		using var reader2 = cmd2.ExecuteReader();
		while (reader1.Read())
		{
			Assert.True(reader2.Read());
			Assert.Equal(reader1.GetInt32(0), reader2.GetInt32(0));
			Assert.Equal(reader1.GetString(1), reader2.GetString(1));
		}
		Assert.False(reader2.Read());
	}

	[Fact]
	public void BulkCopyNullDataTable()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		var bulkCopy = new SingleStoreBulkCopy(connection);
		Assert.Throws<ArgumentNullException>(() => bulkCopy.WriteToServer(default(DataTable)));
	}

	[Fact]
	public void BulkCopyDataTableWithSingleStoreDecimal()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("data", typeof(SingleStoreDecimal)),
			},
			Rows =
			{
				new object[] { 1, new SingleStoreDecimal("1.234") },
				new object[] { 2, new SingleStoreDecimal("2.345") },
			},
		};

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, b decimal(20, 10));", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(2, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand(@"select sum(b) from bulk_load_data_table;", connection))
		{
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(3.579m, reader.GetValue(0));
			Assert.Equal("3.579", reader.GetSingleStoreDecimal(0).ToString().TrimEnd('0'));
		}
	}

#if NET6_0_OR_GREATER
	[Fact]
	public void BulkCopyDataTableWithDateOnly()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("date1", typeof(DateOnly)),
			},
			Rows =
			{
				new object[] { 1, new DateOnly(2021, 3, 4) },
			},
		};

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, date1 date);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(1, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand(@"select * from bulk_load_data_table;", connection))
		{
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(new DateOnly(2021, 3, 4), reader.GetDateOnly(1));
		}
	}

	[Fact]
	public void BulkCopyDataTableWithTimeOnly()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("time1", typeof(TimeOnly)),
				new DataColumn("time2", typeof(TimeOnly)),
			},
			Rows =
			{
				new object[] { 1, new TimeOnly(1, 2, 3, 456), new TimeOnly(1, 2, 3, 456) },
			},
		};

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, time1 time, time2 time(6));", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(1, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand(@"select * from bulk_load_data_table;", connection))
		{
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(new TimeOnly(1, 2, 3), reader.GetTimeOnly(1));
			Assert.Equal(new TimeOnly(1, 2, 3, 456), reader.GetTimeOnly(2));
		}
	}
#endif


	public static IEnumerable<object[]> GetBulkCopyData() =>
		new object[][]
		{
			new object[] { "datetime(6)", new object[] { new DateTime(2021, 3, 4, 5, 6, 7, 890), new DateTime(2020, 1, 2, 3, 4, 5, 678) } },
			new object[] { "float", new object[] { 1.0f, 0.1f, 0.000001f } },
			new object[] { "double", new object[] { 1.0, 0.1, 0.000001 } },
			new object[] { "time(6)", new object[] { TimeSpan.Zero, new TimeSpan(1, 2, 3, 4, 5), new TimeSpan(-1, -3, -5, -7, -9) } },
		};

	[Theory]
	[MemberData(nameof(GetBulkCopyData))]
	public void BulkCopyDataTable(string columnType, object[] rows)
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("data", rows[0].GetType()),
			},
		};
		for (var i = 0; i < rows.Length; i++)
			dataTable.Rows.Add(i + 1, rows[i]);

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand($"""
			drop table if exists bulk_load_data_table;
			create table bulk_load_data_table(id int, data {columnType});
			""", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(rows.Length, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand(@"select data from bulk_load_data_table order by id;", connection))
		{
			using var reader = cmd.ExecuteReader();
			for (var i = 0; i < rows.Length; i++)
			{
				Assert.True(reader.Read());
				Assert.Equal(rows[i], reader.GetValue(0));
			}
			Assert.False(reader.Read());
			Assert.False(reader.NextResult());
		}
	}

	[Fact]
	public void BulkCopyToColumnNeedingQuoting()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("@a", typeof(string)),
			},
		};
		dataTable.Rows.Add(2, "two");

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand($"""
			drop table if exists bulk_load_quoted_identifier;
			create table bulk_load_quoted_identifier(id int, `@a` text);
			insert into bulk_load_quoted_identifier values (1, 'one');
			""", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_quoted_identifier",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(1, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand(@"select `@a` from bulk_load_quoted_identifier order by id;", connection))
		{
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal("one", reader.GetString(0));
			Assert.True(reader.Read());
			Assert.Equal("two", reader.GetString(0));
			Assert.False(reader.Read());
			Assert.False(reader.NextResult());
		}
	}

	[Fact]
	public void BulkCopyDataTableWithLongBlob()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("data", typeof(byte[])),
			},
			Rows =
			{
				new object[] { 1, new byte[524200] },
				new object[] { 12345678, new byte[524200] },
			},
		};

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, b longblob);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(2, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand(@"select sum(length(b)) from bulk_load_data_table;", connection))
		{
			Assert.Equal(1_048_400m, cmd.ExecuteScalar());
		}
	}

	[SkippableTheory(ServerFeatures.LargePackets)]
	[InlineData(6)]
	[InlineData(12)]
	[InlineData(21)]
	[InlineData(50)]
	[InlineData(100)]
	public void BulkCopyDataTableWithLongData(int rows)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();

		// create a string that will be about 120,000 UTF-8 bytes
		int codePoints1, codePoints2;
		string collation;
		if (connection.Session.S2ServerVersion.Version.CompareTo(new Version(7, 5, 0)) > 0)
		{
			codePoints1 = 0x2000;
			codePoints2 = 0x10780;
			collation = "utf8mb4_bin";
		}
		else
		{
			codePoints1 = 0x1000;
			codePoints2 = 0x2000;
			collation = "utf8_bin";
		}

		var sb = new StringBuilder { Capacity = 121_000 };
		for (var j = 0; j < 4; j++)
		{
			for (var i = 0x20; i < codePoints1; i++)
				sb.Append((char) i);
			for (var i = 0x10000; i < codePoints2; i++)
				sb.Append(char.ConvertFromUtf32(i));
		}
		var originalStr = sb.ToString();

		var originalBytes = new byte[50_000];
		var random = new Random(1);
		random.NextBytes(originalBytes);

		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("data1", typeof(string)),
				new DataColumn("data2", typeof(byte[])),
			},
		};
		for (var i = 0; i < rows; i++)
			dataTable.Rows.Add(originalStr, originalBytes);

		using (var cmd = new SingleStoreCommand(@$"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a mediumtext collate `{collation}`, b mediumblob);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(rows, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand("select a, b from bulk_load_data_table;", connection))
		using (var reader = cmd.ExecuteReader())
		{
			var readRows = 0;
			var readBytes = new byte[50_000];
			while (reader.Read())
			{
				readRows++;
				Assert.Equal(originalStr, reader.GetString(0));
				reader.GetBytes(1, 0, readBytes, 0, readBytes.Length);
				Assert.Equal(originalBytes, readBytes);
			}
			Assert.Equal(rows, readRows);
		}
	}

	[Fact]
	public void BulkCopyDataTableWithSpecialCharacters()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("data", typeof(string)),
			},
		};

		var strings = new[] { " ", "\t", ",", "\n", "\r", "\\", "ab\t", "\tcd", "ab\tcd", "\tab\ncd\t" };
		for (var i = 0; i < strings.Length; i++)
			dataTable.Rows.Add(i, strings[i]);

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, b text);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(strings.Length, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand("select * from bulk_load_data_table order by a;", connection))
		using (var reader = cmd.ExecuteReader())
		{
			for (int i = 0; i < strings.Length; i++)
			{
				Assert.True(reader.Read());
				Assert.Equal(i, reader.GetInt32(0));
				Assert.Equal(strings[i], reader.GetString(1));
			}
			Assert.False(reader.Read());
		}
	}

	[Theory]
	[InlineData(0, 15, 0, 0)]
	[InlineData(5, 15, 3, 15)]
	[InlineData(5, 16, 3, 15)]
	[InlineData(int.MaxValue, 0, 0, 0)]
	public void BulkCopyNotifyAfter(int notifyAfter, int rowCount, int expectedEventCount, int expectedRowsCopied)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_notify_after;
			create table bulk_copy_notify_after(value int);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			NotifyAfter = notifyAfter,
			DestinationTableName = "bulk_copy_notify_after",
		};
		int eventCount = 0;
		long rowsCopied = 0;
		bulkCopy.SingleStoreRowsCopied += (s, e) =>
		{
			eventCount++;
			rowsCopied = e.RowsCopied;
		};

		var dataTable = new DataTable()
		{
			Columns = { new DataColumn("value", typeof(int)) },
		};
		foreach (var x in Enumerable.Range(1, rowCount))
			dataTable.Rows.Add(new object[] { x });

		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(expectedEventCount, eventCount);
		Assert.Equal(expectedRowsCopied, rowsCopied);
		Assert.Equal(rowCount, result.RowsInserted);
		Assert.Empty(result.Warnings);
	}

	[Theory]
	[InlineData(0, 40, 0, 0, 0, 40)]
	[InlineData(5, 40, 15, 3, 15, 0)]
	[InlineData(5, 40, 20, 4, 20, 17)]
	[InlineData(int.MaxValue, 20, 0, 0, 0, 20)]
	public void BulkCopyAbort(int notifyAfter, int rowCount, int abortAfter, int expectedEventCount, int expectedRowsCopied, long expectedCount)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_abort;
			create table bulk_copy_abort(value longtext);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			NotifyAfter = notifyAfter,
			DestinationTableName = "bulk_copy_abort",
		};
		int eventCount = 0;
		long rowsCopied = 0;
		bulkCopy.SingleStoreRowsCopied += (s, e) =>
		{
			eventCount++;
			rowsCopied = e.RowsCopied;
			if (e.RowsCopied >= abortAfter)
				e.Abort = true;
		};

		var dataTable = new DataTable()
		{
			Columns = { new DataColumn("value", typeof(string)) },
		};
		var str = new string('a', 62500);
		foreach (var x in Enumerable.Range(1, rowCount))
			dataTable.Rows.Add(new object[] { str });

		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(expectedEventCount, eventCount);
		Assert.Equal(expectedRowsCopied, rowsCopied);
		Assert.Equal(expectedCount, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand("select count(value) from bulk_copy_abort;", connection))
			Assert.Equal(expectedCount, cmd.ExecuteScalar());
	}

	[Fact]
	public void BulkCopyColumnMappings()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_column_mapping;
			create table bulk_copy_column_mapping(intvalue int, `text` text, data blob);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_copy_column_mapping",
			ColumnMappings =
			{
				new(1, "@val", "intvalue = @val + 1"),
				new(3, "text"),
				new(4, "data"),
			},
		};

		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("c1", typeof(int)),
				new DataColumn("c2", typeof(int)),
				new DataColumn("c3", typeof(string)),
				new DataColumn("c4", typeof(string)),
				new DataColumn("c5", typeof(byte[])),
			},
			Rows =
			{
				new object[] { 1, 100, "a", "A", new byte[] { 0x33, 0x30 } },
				new object[] { 2, 200, "bb", "BB", new byte[] { 0x33, 0x31 } },
				new object[] { 3, 300, "ccc", "CCC", new byte[] { 0x33, 0x32 } },
			}
		};

		var result = bulkCopy.WriteToServer(dataTable);
		Assert.Equal(3, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using var reader = connection.ExecuteReader(@"select * from bulk_copy_column_mapping order by intvalue;");
		Assert.True(reader.Read());
		Assert.Equal(101, reader.GetValue(0));
		Assert.Equal("A", reader.GetValue(1));
		Assert.Equal(new byte[] { 0x33, 0x30 }, reader.GetValue(2));
		Assert.True(reader.Read());
		Assert.Equal(201, reader.GetValue(0));
		Assert.Equal("BB", reader.GetValue(1));
		Assert.Equal(new byte[] { 0x33, 0x31 }, reader.GetValue(2));
		Assert.True(reader.Read());
		Assert.Equal(301, reader.GetValue(0));
		Assert.Equal("CCC", reader.GetValue(1));
		Assert.Equal(new byte[] { 0x33, 0x32 }, reader.GetValue(2));
		Assert.False(reader.Read());
	}

	[Fact]
	public void BulkCopyColumnMappingsInvalidSourceOrdinal()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_column_mapping;
			create table bulk_copy_column_mapping(intvalue int, `text` text, data blob);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_copy_column_mapping",
			ColumnMappings =
			{
				new(6, "@val", "intvalue = @val + 1"),
			},
		};

		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("c1", typeof(int)),
			},
			Rows =
			{
				new object[] { 1 },
				new object[] { 2 },
				new object[] { 3 },
			}
		};

		Assert.Throws<InvalidOperationException>(() => bulkCopy.WriteToServer(dataTable));
	}

	[Fact]
	public void BulkCopyColumnMappingsInvalidDestinationColumn()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_column_mapping;
			create table bulk_copy_column_mapping(intvalue int, `text` text, data blob);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_copy_column_mapping",
			ColumnMappings =
			{
				new() { SourceOrdinal = 0 },
			},
		};

		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("c1", typeof(int)),
			},
			Rows =
			{
				new object[] { 1 },
				new object[] { 2 },
				new object[] { 3 },
			}
		};

		Assert.Throws<InvalidOperationException>(() => bulkCopy.WriteToServer(dataTable));
	}

	[Fact]
	public void BulkCopyDoesNotInsertAllRows()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();

		connection.Execute(@"drop table if exists bulk_copy_duplicate_pk;
create table bulk_copy_duplicate_pk(id integer primary key, value text not null);");

		var bcp = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_copy_duplicate_pk"
		};

		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("value", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "a" },
				new object[] { 1, "b" },
				new object[] { 3, "c" },
			}
		};

		var ex = Assert.Throws<SingleStoreException>(() => bcp.WriteToServer(dataTable));
		Assert.Equal(SingleStoreErrorCode.DuplicateKeyEntry, ex.ErrorCode);
	}

	[Fact]
	public void BulkCopyDataTableWithWarnings()
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("str", typeof(string)),
				new DataColumn("number", typeof(int)),
			},
			Rows =
			{
				new object[] { "1", 1000 },
				new object[] { "12345678", 1 },
			},
		};

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(str varchar(5), number tinyint);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};

		// Starting with version 8.0, SingleStore has 'data_conversion_compatibility_level' variable that controls the way
		// certain data conversions are performed, so it won't allow the truncation of the data described in this test
		if (connection.Session.S2ServerVersion.Version.CompareTo(S2Versions.HasDataConversionCompatibilityLevelParameter) >= 0)
		{
			Assert.Throws<SingleStoreException>(() => bulkCopy.WriteToServer(dataTable));
		}
		else
		{
			var result = bulkCopy.WriteToServer(dataTable);
			Assert.Equal(2, result.RowsInserted);
			Assert.Empty(result.Warnings);
		}

		// SingleStore doesn't show warnings on data conversion in LOAD DATA
		// Assert.Equal(2, result.Warnings.Count);
		// Assert.Equal(SingleStoreErrorCode.WarningDataOutOfRange, result.Warnings[0].ErrorCode);
		// Assert.Equal(SingleStoreErrorCode.WarningDataTruncated, result.Warnings[1].ErrorCode);
	}

	[Fact]
	public void BulkCopyNullDataReader()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		var bulkCopy = new SingleStoreBulkCopy(connection);
		Assert.Throws<ArgumentNullException>(() => bulkCopy.WriteToServer(default(DbDataReader)));
	}

	[Theory]
	[InlineData(SingleStoreBulkLoaderConflictOption.None, 0, "")]
	[InlineData(SingleStoreBulkLoaderConflictOption.Ignore, 1, "one")]
	[InlineData(SingleStoreBulkLoaderConflictOption.Replace, 3, "two")]
	public void BulkCopyDataTableConflictOption(SingleStoreBulkLoaderConflictOption conflictOption, int expectedRowsInserted, string expected)
	{
		var dataTable = new DataTable()
		{
			Columns =
			{
				new DataColumn("id", typeof(int)),
				new DataColumn("data", typeof(string)),
			},
			Rows =
			{
				new object[] { 1, "one" },
				new object[] { 1, "two" },
			},
		};

		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int not null primary key auto_increment, b text);", connection))
		{
			cmd.ExecuteNonQuery();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			ConflictOption = conflictOption,
			DestinationTableName = "bulk_load_data_table",
		};

		switch (conflictOption)
		{
		case SingleStoreBulkLoaderConflictOption.None:
			var exception = Assert.Throws<SingleStoreException>(() => bulkCopy.WriteToServer(dataTable));
			Assert.Equal(SingleStoreErrorCode.DuplicateKeyEntry, exception.ErrorCode);
			return;

		case SingleStoreBulkLoaderConflictOption.Replace:
			var replaceResult = bulkCopy.WriteToServer(dataTable);
			Assert.Equal(expectedRowsInserted, replaceResult.RowsInserted);
			Assert.Empty(replaceResult.Warnings);
			break;

		case SingleStoreBulkLoaderConflictOption.Ignore:
			var ignoreResult = bulkCopy.WriteToServer(dataTable);
			Assert.Equal(expectedRowsInserted, ignoreResult.RowsInserted);
			break;
		}

		using (var cmd = new SingleStoreCommand("select b from bulk_load_data_table;", connection))
			Assert.Equal(expected, cmd.ExecuteScalar());
	}
#endif

	internal static string GetConnectionString() => AppConfig.ConnectionString;

	internal static string GetLocalConnectionString()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.AllowLoadLocalInfile = true;
		return csb.ConnectionString;
	}

	readonly string m_testTable;
	readonly byte[] m_memoryStreamBytes;
}
