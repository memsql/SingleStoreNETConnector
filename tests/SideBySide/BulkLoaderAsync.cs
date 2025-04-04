using SingleStoreConnector.Core;
using Xunit.Sdk;

namespace SideBySide;

[Collection("BulkLoaderCollection")]
public class BulkLoaderAsync : IClassFixture<DatabaseFixture>
{
	public BulkLoaderAsync(DatabaseFixture database)
	{
		m_testTable = "BulkLoaderAsyncTest";
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

	[SkippableFact(ConfigSettings.TsvFile)]
	public async Task BulkLoadTsvFile()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderTsvFile;
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
		int rowCount = await bl.LoadAsync();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalTsvFile)]
	public async Task BulkLoadLocalTsvFile()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderLocalTsvFile;
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = true;
		int rowCount = await bl.LoadAsync();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.CsvFile)]
	public async Task BulkLoadCsvFile()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.FileName = AppConfig.SingleStoreBulkLoaderCsvFile;
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
		int rowCount = await bl.LoadAsync();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public async Task BulkLoadLocalCsvFile()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
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
		int rowCount = await bl.LoadAsync();
		Assert.Equal(20, rowCount);
	}

	[Fact]
	public async Task BulkLoadCsvFileNotFound()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		await connection.OpenAsync();

		var secureFilePath = await connection.ExecuteScalarAsync<string>(@"select @@global.secure_file_priv;");
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
			int rowCount = await bl.LoadAsync();
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
	public async Task BulkLoadLocalCsvFileNotFound()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
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
			int rowCount = await bl.LoadAsync();
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
	public async Task BulkLoadLocalCsvFileInTransactionWithCommit()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
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

			var rowCount = await bulkLoader.LoadAsync();
			Assert.Equal(20, rowCount);

			transaction.Commit();
		}

		Assert.Equal(20, await connection.ExecuteScalarAsync<int>($@"select count(*) from {m_testTable};"));
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public async Task BulkLoadLocalCsvFileInTransactionWithRollback()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
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

			var rowCount = await bulkLoader.LoadAsync();
			Assert.Equal(20, rowCount);

			transaction.Rollback();
		}

		Assert.Equal(0, await connection.ExecuteScalarAsync<int>($@"select count(*) from {m_testTable};"));
	}

	[Fact]
	public async Task BulkLoadMissingFileName()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		await connection.OpenAsync();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
#if BASELINE
		await Assert.ThrowsAsync<System.NullReferenceException>(async () =>
		{
			int rowCount = await bl.LoadAsync();
		});
#else
		await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
		{
			int rowCount = await bl.LoadAsync();
		});
#endif
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public async Task BulkLoadMissingTableName()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		await connection.OpenAsync();
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
		await Assert.ThrowsAsync<SingleStoreException>(async () =>
		{
			int rowCount = await bl.LoadAsync();
		});
#else
		await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
		{
			int rowCount = await bl.LoadAsync();
		});
#endif
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public async Task BulkLoadFileStreamInvalidOperation()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		var bl = new SingleStoreBulkLoader(connection);
		using var fileStream = new FileStream(AppConfig.SingleStoreBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
#if !BASELINE
		bl.SourceStream = fileStream;
#endif
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = false;
#if !BASELINE
		await Assert.ThrowsAsync<InvalidOperationException>(async () => { var rowCount = await bl.LoadAsync(); });
#else
		await Assert.ThrowsAsync<MySqlException>(async () => { var rowCount = await bl.LoadAsync(fileStream); });
#endif
	}

	[SkippableFact(ConfigSettings.LocalCsvFile)]
	public async Task BulkLoadLocalFileStream()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var fileStream = new FileStream(AppConfig.SingleStoreBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
#if !BASELINE
		bl.SourceStream = fileStream;
#endif
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
		bl.NumberOfLinesToSkip = 1;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Expressions.Add("five = UNHEX(five)");
		bl.Local = true;
#if !BASELINE
		int rowCount = await bl.LoadAsync();
#else
		int rowCount = await bl.LoadAsync(fileStream);
#endif
		Assert.Equal(20, rowCount);
	}

	[Fact]
	public async Task BulkLoadMemoryStreamInvalidOperation()
	{
		using var connection = new SingleStoreConnection(GetConnectionString());
		await connection.OpenAsync();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var memoryStream = new MemoryStream(m_memoryStreamBytes, false);
#if !BASELINE
		bl.SourceStream = memoryStream;
#endif
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three" });
		bl.NumberOfLinesToSkip = 0;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Local = false;
#if !BASELINE
		await Assert.ThrowsAsync<InvalidOperationException>(async () => { var rowCount = await bl.LoadAsync(); });
#else
		await Assert.ThrowsAsync<SingleStoreException>(async () => { var rowCount = await bl.LoadAsync(memoryStream); });
#endif
	}

	[Fact]
	public async Task BulkLoadLocalMemoryStream()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
		SingleStoreBulkLoader bl = new SingleStoreBulkLoader(connection);
		using var memoryStream = new MemoryStream(m_memoryStreamBytes, false);
#if !BASELINE
		bl.SourceStream = memoryStream;
#endif
		bl.TableName = m_testTable;
		bl.Columns.AddRange(new string[] { "one", "two", "three" });
		bl.NumberOfLinesToSkip = 0;
		bl.FieldTerminator = ",";
		bl.FieldQuotationCharacter = '"';
		bl.FieldQuotationOptional = true;
		bl.Local = true;
#if !BASELINE
		int rowCount = await bl.LoadAsync();
#else
		int rowCount = await bl.LoadAsync(memoryStream);
#endif
		Assert.Equal(5, rowCount);
	}

#if !BASELINE
	[Fact]
	public async Task BulkCopyDataReader()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		using var connection2 = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
		await connection2.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_reader_source;
drop table if exists bulk_load_data_reader_destination;
create table bulk_load_data_reader_source(value int, name text);
create table bulk_load_data_reader_destination(value int, name text);
insert into bulk_load_data_reader_source values(0, 'zero'),(1,'one'),(2,'two'),(3,'three'),(4,'four'),(5,'five'),(6,'six');", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		using (var cmd = new SingleStoreCommand("select * from bulk_load_data_reader_source;", connection))
		using (var reader = await cmd.ExecuteReaderAsync())
		{
			var bulkCopy = new SingleStoreBulkCopy(connection2) { DestinationTableName = "bulk_load_data_reader_destination", };
			var result = await bulkCopy.WriteToServerAsync(reader);
			Assert.Equal(7, result.RowsInserted);
			Assert.Empty(result.Warnings);
		}

		using var cmd1 = new SingleStoreCommand("select * from bulk_load_data_reader_source order by value;", connection);
		using var cmd2 = new SingleStoreCommand("select * from bulk_load_data_reader_destination order by value;", connection2);
		using var reader1 = await cmd1.ExecuteReaderAsync();
		using var reader2 = await cmd2.ExecuteReaderAsync();
		while (await reader1.ReadAsync())
		{
			Assert.True(await reader2.ReadAsync());
			Assert.Equal(reader1.GetInt32(0), reader2.GetInt32(0));
			Assert.Equal(reader1.GetString(1), reader2.GetString(1));
		}
		Assert.False(await reader2.ReadAsync());
	}

	[Fact]
	public void BulkCopyNullDataTable()
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		connection.Open();
		var bulkCopy = new SingleStoreBulkCopy(connection);
		Assert.ThrowsAsync<ArgumentNullException>(async () => await bulkCopy.WriteToServerAsync(default(DataTable)));
	}

	[Fact]
	public async Task BulkCopyDataTableWithLongData()
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
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, b longblob);", connection))
		{
			await cmd.ExecuteNonQueryAsync();
		}

		var bulkCopy = new SingleStoreBulkCopy(connection)
		{
			DestinationTableName = "bulk_load_data_table",
		};
		var result = await bulkCopy.WriteToServerAsync(dataTable);
		Assert.Equal(2, result.RowsInserted);
		Assert.Empty(result.Warnings);
	}

	[Theory]
	[InlineData(0, 15, 0, 0)]
	[InlineData(5, 15, 3, 15)]
	[InlineData(5, 16, 3, 15)]
	[InlineData(int.MaxValue, 0, 0, 0)]
	public async Task BulkCopyNotifyAfter(int notifyAfter, int rowCount, int expectedEventCount, int expectedRowsCopied)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_notify_after;
			create table bulk_copy_notify_after(value int);", connection))
		{
			await cmd.ExecuteNonQueryAsync();
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

		var result = await bulkCopy.WriteToServerAsync(dataTable);
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
	public async Task BulkCopyAbort(int notifyAfter, int rowCount, int abortAfter, int expectedEventCount, int expectedRowsCopied, long expectedCount)
	{
		using var connection = new SingleStoreConnection(GetLocalConnectionString());
		await connection.OpenAsync();
		using (var cmd = new SingleStoreCommand(@"drop table if exists bulk_copy_abort;
			create table bulk_copy_abort(value longtext);", connection))
		{
			await cmd.ExecuteNonQueryAsync();
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

		var result = await bulkCopy.WriteToServerAsync(dataTable);
		Assert.Equal(expectedEventCount, eventCount);
		Assert.Equal(expectedRowsCopied, rowsCopied);
		Assert.Equal(expectedCount, result.RowsInserted);
		Assert.Empty(result.Warnings);

		using (var cmd = new SingleStoreCommand("select count(value) from bulk_copy_abort;", connection))
			Assert.Equal(expectedCount, await cmd.ExecuteScalarAsync());
	}

	[Fact]
	public async Task BulkCopyDataTableWithWarnings()
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
			await Assert.ThrowsAsync<SingleStoreException>(async () => await bulkCopy.WriteToServerAsync(dataTable));
		}
		else
		{
			var result = await bulkCopy.WriteToServerAsync(dataTable);
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
		Assert.ThrowsAsync<ArgumentNullException>(async () => await bulkCopy.WriteToServerAsync(default(DbDataReader)));
	}
#endif

	private static string GetConnectionString() => BulkLoaderSync.GetConnectionString();
	private static string GetLocalConnectionString() => BulkLoaderSync.GetLocalConnectionString();

	readonly string m_testTable;
	readonly byte[] m_memoryStreamBytes;
}
