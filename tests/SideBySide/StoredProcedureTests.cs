namespace SideBySide;

public class StoredProcedureTests : IClassFixture<StoredProcedureFixture>
{
	public StoredProcedureTests(StoredProcedureFixture database)
	{
		m_database = database;
	}

	[Theory]
	/* See PLAT-6053
	[InlineData("FUNCTION", "NonQuery", false)]
	[InlineData("FUNCTION", "Scalar", false)]
	[InlineData("FUNCTION", "Reader", false)] */
	[InlineData("PROCEDURE", "NonQuery", true)]
	[InlineData("PROCEDURE", "NonQuery", false)]
	[InlineData("PROCEDURE", "Scalar", true)]
	[InlineData("PROCEDURE", "Scalar", false)]
	[InlineData("PROCEDURE", "Reader", true)]
	[InlineData("PROCEDURE", "Reader", false)]
	public async Task StoredProcedureEcho(string procedureType, string executorType, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
		cmd.CommandType = CommandType.StoredProcedure;

		cmd.Parameters.Add(new()
		{
			ParameterName = "@name",
			DbType = DbType.String,
			Direction = ParameterDirection.Input,
			Value = "hello",
		});

		// we make the assumption that Stored Procedures with ParameterDirection.ReturnValue are functions
		if (procedureType == "FUNCTION")
		{
			cmd.Parameters.Add(new()
			{
				ParameterName = "@result",
				DbType = DbType.String,
				Direction = ParameterDirection.ReturnValue,
			});
		}

		if (prepare)
			await cmd.PrepareAsync();
		var result = await ExecuteCommandAsync(cmd, executorType);
		if (procedureType == "PROCEDURE" && executorType != "NonQuery")
			Assert.Equal(cmd.Parameters["@name"].Value, result);
		if (procedureType == "FUNCTION")
			Assert.Equal(cmd.Parameters["@name"].Value, cmd.Parameters["@result"].Value);
	}

	[Fact]
	public void CallFailingFunction()
	{
		using var command = m_database.Connection.CreateCommand();

		command.CommandType = CommandType.StoredProcedure;
		command.CommandText = "failing_function";

		var returnParameter = command.CreateParameter();
		returnParameter.DbType = DbType.Int32;
		returnParameter.Direction = ParameterDirection.ReturnValue;
		command.Parameters.Add(returnParameter);

		Assert.Throws<SingleStoreException>(() => command.ExecuteNonQuery());
	}

	[Fact]
	public void CallFailingFunctionInTransaction()
	{
		using var transaction = m_database.Connection.BeginTransaction();
		using var command = m_database.Connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandType = CommandType.StoredProcedure;
		command.CommandText = "failing_function";

		var returnParameter = command.CreateParameter();
		returnParameter.DbType = DbType.Int32;
		returnParameter.Direction = ParameterDirection.ReturnValue;
		command.Parameters.Add(returnParameter);

		Assert.Throws<SingleStoreException>(() => command.ExecuteNonQuery());
		transaction.Commit();
	}

	[SkippableTheory(ServerFeatures.StoredProcedures)]
	// [InlineData("FUNCTION", false)] see PLAT-6053
	[InlineData("PROCEDURE", true)]
	[InlineData("PROCEDURE", false)]
	public async Task StoredProcedureEchoException(string procedureType, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "echo" + (procedureType == "FUNCTION" ? "f" : "p");
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
		{
#if BASELINE
			await Assert.ThrowsAsync<ArgumentException>(async () => await cmd.PrepareAsync());
#else
			await cmd.PrepareAsync();
#endif
		}

		if (procedureType == "FUNCTION")
			await Assert.ThrowsAsync<InvalidOperationException>(async () => await cmd.ExecuteNonQueryAsync());
		else
			await Assert.ThrowsAsync<ArgumentException>(async () => await cmd.ExecuteNonQueryAsync());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StoredProcedureNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		using (var reader = await cmd.ExecuteReaderAsync())
		{
			Assert.False(await reader.ReadAsync());
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task FieldCountForNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		using (var reader = await cmd.ExecuteReaderAsync())
		{
			Assert.Equal(0, reader.FieldCount);
			Assert.False(reader.HasRows);
			Assert.False(await reader.ReadAsync());
			Assert.Equal(0, reader.FieldCount);
			Assert.False(reader.HasRows);
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetSchemaTableForNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.False(await reader.ReadAsync());
		var table = reader.GetSchemaTable();
		Assert.Null(table);
		Assert.False(await reader.NextResultAsync());
	}

#if !BASELINE
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetColumnSchemaForNoResultSet(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "out_string";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.False(await reader.ReadAsync());
		Assert.Empty(reader.GetColumnSchema());
		Assert.False(await reader.NextResultAsync());
	}
#endif

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StoredProcedureReturnsNull(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "echo_null";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		var reader = (SingleStoreDataReader) await cmd.ExecuteReaderAsync();
		var result = await reader.ReadAsync();
		Assert.True(result);
        Assert.Equal(DBNull.Value, reader.GetValue(0));
        Assert.Equal(DBNull.Value, reader.GetValue(1));
	}

	[Theory]
	[InlineData("NonQuery", true)]
	[InlineData("NonQuery", false)]
	[InlineData("Scalar", true)]
	[InlineData("Scalar", false)]
	[InlineData("Reader", true)]
	[InlineData("Reader", false)]
	public async Task StoredProcedureCircle(string executorType, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "circle";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@radius",
			DbType = DbType.Double,
			Direction = ParameterDirection.Input,
			Value = 1.0,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@height",
			DbType = DbType.Double,
			Direction = ParameterDirection.Input,
			Value = 2.0,
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@name",
			DbType = DbType.String,
			Direction = ParameterDirection.Input,
			Value = "awesome",
		});

		if (prepare)
			await cmd.PrepareAsync();
		await CircleAssertions(cmd, executorType);
	}

	[SkippableTheory(ServerFeatures.StoredProcedures)]
	[InlineData("NonQuery", true)]
	[InlineData("NonQuery", false)]
	[InlineData("Scalar", true)]
	[InlineData("Scalar", false)]
	[InlineData("Reader", true)]
	[InlineData("Reader", false)]
	public async Task StoredProcedureCircleCached(string executorType, bool prepare)
	{
		// reorder parameters
		// remove return types
		// remove directions (SingleStoreConnector only, MySql.Data does not fix these up)
		// CachedProcedure class should fix everything up based on parameter names
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "circle";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new()
		{
			ParameterName = "@name",
			Value = "awesome",
#if BASELINE
			Direction = ParameterDirection.Input,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@radius",
			Value = 1.5,
#if BASELINE
			Direction = ParameterDirection.Input,
#endif
		});
		cmd.Parameters.Add(new()
		{
			ParameterName = "@height",
			Value = 2.0,
#if BASELINE
			Direction = ParameterDirection.Input,
#endif
		});

		if (prepare)
			await cmd.PrepareAsync();
		await CircleAssertions(cmd, executorType);
	}

	private async Task CircleAssertions(DbCommand cmd, string executorType)
	{
		if (executorType == "Reader")
		{
		    var reader = (SingleStoreDataReader) await cmd.ExecuteReaderAsync();
		    var result = await reader.ReadAsync();
		    Assert.True(result);
			Assert.Equal(2 * (double) cmd.Parameters["@radius"].Value, reader.GetDouble("diameter"));
			Assert.Equal(2.0 * Math.PI * (double) cmd.Parameters["@radius"].Value, reader.GetDouble("circumference"));
			Assert.Equal(Math.PI * Math.Pow((double) cmd.Parameters["@radius"].Value, 2), reader.GetDouble("area"));
			Assert.Equal(reader.GetDouble("area") * (double) cmd.Parameters["@height"].Value, reader.GetDouble("volume"));
		} else {
			var result = await ExecuteCommandAsync(cmd, executorType);
			if (executorType != "NonQuery")
				Assert.Equal((string) cmd.Parameters["@name"].Value + "circle", result);
		}
	}

	private async Task<object> ExecuteCommandAsync(DbCommand cmd, string executorType)
	{
		switch (executorType)
		{
		case "NonQuery":
			await cmd.ExecuteNonQueryAsync();
			return null;
		case "Scalar":
			return await cmd.ExecuteScalarAsync();
		default:
			using (var reader = await cmd.ExecuteReaderAsync())
			{
				if (await reader.ReadAsync())
					return reader.GetValue(0);
				return null;
			}
		}
	}

	[Theory]
	[InlineData("factor", true)]
	[InlineData("factor", false)]
	[InlineData("@factor", true)]
	[InlineData("@factor", false)]
	[InlineData("?factor", true)]
	[InlineData("?factor", false)]
	public async Task MultipleRows(string paramaterName, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "number_multiples";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new() { ParameterName = paramaterName, Value = 3 });

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal("six", reader.GetString(0));
		Assert.True(await reader.ReadAsync());
		Assert.Equal("three", reader.GetString(0));
		Assert.False(await reader.ReadAsync());
		Assert.False(await reader.NextResultAsync());
	}

	[Theory]
	[InlineData(1, new string[0], new[] { "eight", "five", "four", "seven", "six", "three", "two" }, true)]
	[InlineData(1, new string[0], new[] { "eight", "five", "four", "seven", "six", "three", "two" }, false)]
	[InlineData(4, new[] { "one", "three", "two" }, new[] { "eight", "five", "seven", "six" }, true)]
	[InlineData(4, new[] { "one", "three", "two" }, new[] { "eight", "five", "seven", "six" }, false)]
	[InlineData(8, new[] { "five", "four", "one", "seven", "six", "three", "two" }, new string[0], true)]
	[InlineData(8, new[] { "five", "four", "one", "seven", "six", "three", "two" }, new string[0], false)]
	public async Task MultipleResultSets(int pivot, string[] firstResultSet, string[] secondResultSet, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "multiple_result_sets";
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.Add(new() { ParameterName = "@pivot", Value = pivot });

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		foreach (var result in firstResultSet)
		{
			Assert.True(await reader.ReadAsync());
			Assert.Equal(result, reader.GetString(0));
		}
		Assert.False(await reader.ReadAsync());

		Assert.True(await reader.NextResultAsync());

		foreach (var result in secondResultSet)
		{
			Assert.True(await reader.ReadAsync());
			Assert.Equal(result, reader.GetString(0));
		}
		Assert.False(await reader.ReadAsync());

		Assert.False(await reader.NextResultAsync());
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ParameterLoop(bool prepare)
	{
		using var connection = CreateOpenConnection();
		var parameter = new SingleStoreParameter
		{
			ParameterName = "high",
			DbType = DbType.Int32,
			Direction = ParameterDirection.Input,
			Value = 1
		};
		while ((int) parameter.Value < 8)
		{
			using var cmd = connection.CreateCommand();
			var nextValue = (int) parameter.Value + 1;
			cmd.CommandText = "number_lister";
			cmd.CommandType = CommandType.StoredProcedure;
			cmd.Parameters.Add(parameter);
			if (prepare)
				await cmd.PrepareAsync();
			using (var reader = await cmd.ExecuteReaderAsync())
			{
				for (var i = 0; i < (int) parameter.Value; i++)
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal(i + 1, reader.GetInt32(0));
					Assert.True(reader.GetString(1).Length > 0);
				}
				await reader.NextResultAsync();
			}
			parameter.Value = nextValue;
		}
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(false, false)]
	[InlineData(true, true)]
	[InlineData(true, false)]
	public async Task DottedName(bool useDatabaseName, bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var cmd = connection.CreateCommand();
		cmd.CommandText = (useDatabaseName ? $"{connection.Database}." : "") + "`dotted.name`";
		cmd.CommandType = CommandType.StoredProcedure;

		if (prepare)
			await cmd.PrepareAsync();
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.True(await reader.ReadAsync());
		Assert.Equal(1, reader.GetInt32(0));
		Assert.Equal(2, reader.GetInt32(1));
		Assert.Equal(3, reader.GetInt32(2));
		Assert.False(await reader.ReadAsync());
		Assert.False(await reader.NextResultAsync());
	}

	[Fact]
	public void DeriveParametersCircle()
	{
		using var cmd = new SingleStoreCommand("circle", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		SingleStoreCommandBuilder.DeriveParameters(cmd);

		Assert.Collection(cmd.Parameters.Cast<SingleStoreParameter>(),
			AssertParameter("@radius", ParameterDirection.Input, SingleStoreDbType.Double),
			AssertParameter("@height", ParameterDirection.Input, SingleStoreDbType.Double),
			AssertParameter("@name", ParameterDirection.Input, SingleStoreDbType.VarChar));
	}

	[Fact]
	public void DeriveParametersNumberLister()
	{
		using var cmd = new SingleStoreCommand("number_lister", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		SingleStoreCommandBuilder.DeriveParameters(cmd);

		Assert.Collection(cmd.Parameters.Cast<SingleStoreParameter>(),
			AssertParameter("@high", ParameterDirection.Input, SingleStoreDbType.Int32));
	}

	[Fact]
	public void DeriveParametersRemovesExisting()
	{
		using var cmd = new SingleStoreCommand("number_lister", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddWithValue("test1", 1);
		cmd.Parameters.AddWithValue("test2", 2);
		cmd.Parameters.AddWithValue("test3", 3);

		SingleStoreCommandBuilder.DeriveParameters(cmd);
		Assert.Collection(cmd.Parameters.Cast<SingleStoreParameter>(),
			AssertParameter("@high", ParameterDirection.Input, SingleStoreDbType.Int32));
	}

	[Fact]
	public void DeriveParametersDoesNotExist()
	{
		using var cmd = new SingleStoreCommand("xx_does_not_exist", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		Assert.Throws<SingleStoreException>(() => SingleStoreCommandBuilder.DeriveParameters(cmd));
	}

	[Fact]
	public void DeriveParametersDoesNotExistThenIsCreated()
	{
		using (var cmd = new SingleStoreCommand("drop procedure if exists xx_does_not_exist_2;", m_database.Connection))
			cmd.ExecuteNonQuery();

		using (var cmd = new SingleStoreCommand("xx_does_not_exist_2", m_database.Connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			Assert.Throws<SingleStoreException>(() => SingleStoreCommandBuilder.DeriveParameters(cmd));
		}

		using (var cmd = new SingleStoreCommand(@"create procedure xx_does_not_exist_2(param1 INT) AS
			DECLARE param2 VARCHAR(100);
			BEGIN
				param2 = 'test';
			END", m_database.Connection))
		{
			cmd.ExecuteNonQuery();
		}

		using (var cmd = new SingleStoreCommand("xx_does_not_exist_2", m_database.Connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			SingleStoreCommandBuilder.DeriveParameters(cmd);
			Assert.Collection(cmd.Parameters.Cast<SingleStoreParameter>(),
				AssertParameter("@param1", ParameterDirection.Input, SingleStoreDbType.Int32));
		}
	}

	[Theory]
	[InlineData("bit(1)", 1)]
	[InlineData("bit(10)", 10)]
#if !BASELINE
	[InlineData("bool", 1)]
	[InlineData("tinyint(1)", 1)]
	[InlineData("decimal(10)", 10)]
#endif
	[InlineData("char(30)", 30)]
	[InlineData("mediumtext", 0)]
	[InlineData("varchar(50)", 50)]
	// These return nonzero sizes for some versions of MySQL Server 8.0
	// [InlineData("bit", 0)]
	// [InlineData("tinyint", 0)]
	// [InlineData("bigint", 0)]
	// [InlineData("bigint unsigned", 0)]
	public void DeriveParametersParameterSize(string parameterType, int expectedSize)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = false;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();

		using (var cmd = new SingleStoreCommand($"create or replace procedure parameter_size(param1 {parameterType}) as begin end;", connection))
			cmd.ExecuteNonQuery();

		using (var cmd = new SingleStoreCommand("parameter_size", connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			SingleStoreCommandBuilder.DeriveParameters(cmd);
			var parameter = (SingleStoreParameter) Assert.Single(cmd.Parameters);
			Assert.Equal(expectedSize, parameter.Size);
		}
	}

	[Theory]
	[InlineData("bit", SingleStoreDbType.Bit)]
	[InlineData("bit(1)", SingleStoreDbType.Bit)]
#if BASELINE
	[InlineData("bool", SingleStoreDbType.Byte)]
	[InlineData("tinyint(1)", MySqlDbType.Byte)]
#else
	[InlineData("bool", SingleStoreDbType.Bool)]
	[InlineData("tinyint(1)", SingleStoreDbType.Bool)]
#endif
	[InlineData("tinyint", SingleStoreDbType.Byte)]
	[InlineData("bigint", SingleStoreDbType.Int64)]
	[InlineData("bigint unsigned", SingleStoreDbType.UInt64)]
	[InlineData("char(30)", SingleStoreDbType.String)]
	[InlineData("mediumtext", SingleStoreDbType.MediumText)]
	[InlineData("varchar(50)", SingleStoreDbType.VarChar)]
	[InlineData("decimal(10, 0)", SingleStoreDbType.NewDecimal)]
	[InlineData("decimal(10, 0) unsigned", SingleStoreDbType.NewDecimal)]
	public void DeriveParametersParameterType(string parameterType, SingleStoreDbType expectedType)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = false;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.Open();

		using (var cmd = new SingleStoreCommand($"create or replace procedure parameter_size(param1 {parameterType}) as begin end;", connection))
			cmd.ExecuteNonQuery();

		using (var cmd = new SingleStoreCommand("parameter_size", connection))
		{
			cmd.CommandType = CommandType.StoredProcedure;
			SingleStoreCommandBuilder.DeriveParameters(cmd);
			var parameter = (SingleStoreParameter) Assert.Single(cmd.Parameters);
			Assert.Equal(expectedType, parameter.SingleStoreDbType);
		}
	}

	[SkippableFact(ServerFeatures.Json, Baseline = "https://bugs.mysql.com/bug.php?id=89335")]
	public void DeriveParametersSetJson()
	{
		using var cmd = new SingleStoreCommand("SetJson", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		SingleStoreCommandBuilder.DeriveParameters(cmd);

		Assert.Collection(cmd.Parameters.Cast<SingleStoreParameter>(),
			AssertParameter("@vJson", ParameterDirection.Input, SingleStoreDbType.JSON));
	}

	[SkippableFact(ServerFeatures.Json, Baseline = "https://bugs.mysql.com/bug.php?id=101485")]
	public void PassJsonParameter()
	{
		using var cmd = new SingleStoreCommand("SetJson", m_database.Connection);
		cmd.CommandType = CommandType.StoredProcedure;
		var json = "{\"prop\":[null]}";
		cmd.Parameters.AddWithValue("@vJson", json).SingleStoreDbType = SingleStoreDbType.JSON;
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(json, reader.GetString(0).Replace(" ", ""));
		Assert.False(reader.Read());
	}

	private static Action<SingleStoreParameter> AssertParameter(string name, ParameterDirection direction, SingleStoreDbType mySqlDbType)
	{
		return x =>
		{
			Assert.Equal(name, x.ParameterName);
			Assert.Equal(direction, x.Direction);
			Assert.Equal(mySqlDbType, x.SingleStoreDbType);
		};
	}

	[Theory]
	[InlineData("echof", "FUNCTION", "varchar(63)", " BEGIN RETURN name; END", "", "")]
	[InlineData("echop", "PROCEDURE", "void", " BEGIN ECHO SELECT name; END", "", "")]
	[InlineData("failing_function", "FUNCTION", "decimal(10,5)", " DECLARE v1 DECIMAL(10,5); BEGIN v1 = 1/0; RETURN v1; END", "", "")]
	public void ProceduresSchema(string procedureName, string procedureType, string dtdIdentifier, string routineDefinition, string isDeterministic, string dataAccess)
	{
		var dataTable = m_database.Connection.GetSchema("Procedures");
		var schema = m_database.Connection.Database;
		var row = dataTable.Rows.Cast<DataRow>().Single(x => schema.Equals(x["ROUTINE_SCHEMA"]) && procedureName.Equals(x["ROUTINE_NAME"]));

		Assert.Equal(procedureName, row["SPECIFIC_NAME"]);
		Assert.Equal(procedureType, row["ROUTINE_TYPE"]);
		Assert.Equal(dtdIdentifier, ((string) row["DTD_IDENTIFIER"]).Split(' ')[0]);
		Assert.Equal(routineDefinition, NormalizeSpaces((string) row["ROUTINE_DEFINITION"]));
		Assert.Equal(isDeterministic, row["IS_DETERMINISTIC"]);
		Assert.Equal(dataAccess, ((string) row["SQL_DATA_ACCESS"]).Replace('_', ' '));
	}

	[Fact]
	public void CallNonExistentStoredProcedure()
	{
		using var command = new SingleStoreCommand("NonExistentStoredProcedure", m_database.Connection);
		command.CommandType = CommandType.StoredProcedure;
		Assert.Throws<SingleStoreException>(() => command.ExecuteNonQuery());
	}

	[Fact]
	public void PrepareNonExistentStoredProcedure()
	{
		using var connection = CreateOpenConnection();
		using var command = new SingleStoreCommand("NonExistentStoredProcedure", connection);
		command.CommandType = CommandType.StoredProcedure;
		Assert.Throws<SingleStoreException>(command.Prepare);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ReturnTimeParameter(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var command = new SingleStoreCommand("GetTime", connection);
		command.CommandType = CommandType.StoredProcedure;
		var parameter = command.CreateParameter();

		if (prepare)
			command.Prepare();
		var result = command.ExecuteScalar();
		Assert.IsType<TimeSpan>(result);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void EnumProcedure(bool prepare)
	{
		using var connection = CreateOpenConnection();
		using var command = new SingleStoreCommand("EnumProcedure", connection);
		command.CommandType = CommandType.StoredProcedure;
		command.Parameters.AddWithValue("@input", "One");
		if (prepare)
			command.Prepare();
		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal("One", reader.GetString(0));
		Assert.False(reader.Read());
	}

	[Theory]
	[InlineData("`a b`")]
	[InlineData("`a.b`")]
	[InlineData("`a``b`")]
	[InlineData("`a b.c ``d`")]
	public void SprocNameSpecialCharacters(string sprocName)
	{
		using var connection = CreateOpenConnection();

		using (var command = new SingleStoreCommand($@"CREATE OR REPLACE PROCEDURE {sprocName} () AS
BEGIN
	ECHO SELECT 'test' AS Result;
END;", connection))
		{
			command.ExecuteNonQuery();
		}

		using (var command = new SingleStoreCommand(sprocName, connection))
		{
			command.CommandType = CommandType.StoredProcedure;

			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal("test", reader.GetString(0));
			Assert.False(reader.Read());
		}
	}

	private static string NormalizeSpaces(string input)
	{
		input = input.Replace('\r', ' ');
		input = input.Replace('\n', ' ');
		input = input.Replace('\t', ' ');
		int startingLength;
		do
		{
			startingLength = input.Length;
			input = input.Replace("  ", " ");
		} while (input.Length != startingLength);
		return input;
	}

	private static SingleStoreConnection CreateOpenConnection()
	{
		var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		connection.Open();
		return connection;
	}

	readonly DatabaseFixture m_database;
}
