using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SingleStoreConnector.Core;

namespace SingleStoreConnector;

public sealed class SingleStoreDataAdapter : DbDataAdapter
{
	public SingleStoreDataAdapter()
	{
		GC.SuppressFinalize(this);
	}

	public SingleStoreDataAdapter(SingleStoreCommand selectCommand)
		: this()
	{
		SelectCommand = selectCommand;
	}

	public SingleStoreDataAdapter(string selectCommandText, SingleStoreConnection connection)
		: this(new SingleStoreCommand(selectCommandText, connection))
	{
	}

	public SingleStoreDataAdapter(string selectCommandText, string connectionString)
		: this(new SingleStoreCommand(selectCommandText, new SingleStoreConnection(connectionString)))
	{
	}

	public event SingleStoreRowUpdatingEventHandler? RowUpdating;

	public event SingleStoreRowUpdatedEventHandler? RowUpdated;

	public new SingleStoreCommand? DeleteCommand
	{
		get => (SingleStoreCommand?) base.DeleteCommand;
		set => base.DeleteCommand = value;
	}

	public new SingleStoreCommand? InsertCommand
	{
		get => (SingleStoreCommand?) base.InsertCommand;
		set => base.InsertCommand = value;
	}

	public new SingleStoreCommand? SelectCommand
	{
		get => (SingleStoreCommand?) base.SelectCommand;
		set => base.SelectCommand = value;
	}

	public new SingleStoreCommand? UpdateCommand
	{
		get => (SingleStoreCommand?) base.UpdateCommand;
		set => base.UpdateCommand = value;
	}

	protected override void OnRowUpdating(RowUpdatingEventArgs value) => RowUpdating?.Invoke(this, (SingleStoreRowUpdatingEventArgs) value);

	protected override void OnRowUpdated(RowUpdatedEventArgs value) => RowUpdated?.Invoke(this, (SingleStoreRowUpdatedEventArgs) value);

	protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping) => new SingleStoreRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);

	protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping) => new SingleStoreRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);

	public override int UpdateBatchSize { get; set; }

	protected override void InitializeBatching() => m_batch = new();

	protected override void TerminateBatching()
	{
		m_batch?.Dispose();
		m_batch = null;
	}

	protected override int AddToBatch(IDbCommand command)
	{
		var mySqlCommand = (SingleStoreCommand) command;
		if (m_batch!.Connection is null)
		{
			m_batch.Connection = mySqlCommand.Connection;
			m_batch.Transaction = mySqlCommand.Transaction;
		}

		var count = m_batch.BatchCommands.Count;
		var batchCommand = new SingleStoreBatchCommand
		{
			CommandText = command.CommandText,
			CommandType = command.CommandType,
		};
		if (mySqlCommand.CloneRawParameters() is SingleStoreParameterCollection clonedParameters)
		{
			foreach (var clonedParameter in clonedParameters)
				batchCommand.Parameters.Add(clonedParameter!);
		}

		m_batch.BatchCommands.Add(batchCommand);
		return count;
	}

	protected override void ClearBatch() => m_batch!.BatchCommands.Clear();

	protected override int ExecuteBatch()
	{
		if (TryConvertToCommand(m_batch!) is SingleStoreCommand command)
		{
			command.Connection = m_batch!.Connection;
			command.Transaction = m_batch.Transaction;
			return command.ExecuteNonQuery();
		}
		else
		{
			return m_batch!.ExecuteNonQuery();
		}
	}

	// Detects if the commands in 'batch' are all "INSERT" commands that can be combined into one large value list;
	// returns a SingleStoreCommand with the combined SQL if so.
	internal static SingleStoreCommand? TryConvertToCommand(SingleStoreBatch batch)
	{
		// ensure there are at least two commands
		if (batch.BatchCommands.Count < 1)
			return null;

		// check for a parameterized command
		var firstCommand = batch.BatchCommands[0];
		if (firstCommand.Parameters.Count == 0)
			return null;
		firstCommand.Batch = batch;

		// check that all commands have the same SQL
		var sql = firstCommand.CommandText;
		for (var i = 1; i < batch.BatchCommands.Count; i++)
		{
			if (batch.BatchCommands[i].CommandText != sql)
				return null;
		}

		// check that it's an INSERT statement
		if (!sql.StartsWith("INSERT INTO ", StringComparison.OrdinalIgnoreCase))
			return null;

		// check for "VALUES(...)" clause
		var match = Regex.Match(sql, @"\bVALUES\s*\([^)]+\)\s*;?\s*$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (!match.Success)
			return null;

		// extract the parameters
		var parser = new InsertSqlParser(firstCommand);
		parser.Parse(sql);

		// record the parameter indexes that were found
		foreach (var parameterIndex in parser.ParameterIndexes)
		{
			if (parameterIndex < 0 || parameterIndex >= firstCommand.Parameters.Count)
				return null;
		}

		// ensure that the VALUES(...) clause contained only parameters, and that all were consumed
		var remainingValues = parser.CommandText.Substring(match.Index + 6).Trim();
		remainingValues = remainingValues.TrimEnd(';').Trim().TrimStart('(').TrimEnd(')');
		remainingValues = remainingValues.Replace(",", "");
		if (!string.IsNullOrWhiteSpace(remainingValues))
			return null;

		// build one INSERT statement with concatenated VALUES
		var combinedCommand = new SingleStoreCommand();
		var sqlBuilder = new StringBuilder(sql.Substring(0, match.Index + 6));
		var combinedParameterIndex = 0;
		for (var i = 0; i < batch.BatchCommands.Count; i++)
		{
			var command = batch.BatchCommands[i];
			if (i != 0)
				sqlBuilder.Append(',');
			sqlBuilder.Append('(');

			for (var parameterIndex = 0; parameterIndex < parser.ParameterIndexes.Count; parameterIndex++)
			{
				if (parameterIndex != 0)
					sqlBuilder.Append(',');
				var parameterName = "@p" + combinedParameterIndex.ToString(CultureInfo.InvariantCulture);
				sqlBuilder.Append(parameterName);
				combinedParameterIndex++;
				var parameter = command.Parameters[parser.ParameterIndexes[parameterIndex]].Clone();
				parameter.ParameterName = parameterName;
				combinedCommand.Parameters.Add(parameter);
			}

			sqlBuilder.Append(')');
		}
		sqlBuilder.Append(';');

		combinedCommand.CommandText = sqlBuilder.ToString();
		return combinedCommand;
	}

	internal sealed class InsertSqlParser : SqlParser
	{
		public InsertSqlParser(ISingleStoreCommand command)
			: base(new StatementPreparer(command.CommandText!, null, command.CreateStatementPreparerOptions()))
		{
			CommandText = command.CommandText!;
			m_parameters = command.RawParameters;
			ParameterIndexes = new();
		}

		public List<int> ParameterIndexes { get; }

		public string CommandText { get; private set; }

		protected override void OnNamedParameter(int index, int length)
		{
			var name = CommandText.Substring(index, length);
			var parameterIndex = m_parameters?.NormalizedIndexOf(name) ?? -1;
			ParameterIndexes.Add(parameterIndex);

			// overwrite the parameter name with spaces
#if NETCOREAPP3_0_OR_GREATER
			CommandText = string.Concat(CommandText.AsSpan(0, index), new string(' ', length), CommandText.AsSpan(index + length));
#else
			CommandText = CommandText.Substring(0, index) + new string(' ', length) + CommandText.Substring(index + length);
#endif
		}

		protected override void OnPositionalParameter(int index)
		{
			ParameterIndexes.Add(ParameterIndexes.Count);

			// overwrite the parameter placeholder with a space
#if NETCOREAPP3_0_OR_GREATER
			CommandText = string.Concat(CommandText.AsSpan(0, index), " ", CommandText.AsSpan(index + 1));
#else
			CommandText = CommandText.Substring(0, index) + " " + CommandText.Substring(index + 1);
#endif
		}

		private readonly SingleStoreParameterCollection? m_parameters;
	}

	private SingleStoreBatch? m_batch;
}

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public delegate void SingleStoreRowUpdatingEventHandler(object sender, SingleStoreRowUpdatingEventArgs e);

public delegate void SingleStoreRowUpdatedEventHandler(object sender, SingleStoreRowUpdatedEventArgs e);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

public sealed class SingleStoreRowUpdatingEventArgs : RowUpdatingEventArgs
{
	public SingleStoreRowUpdatingEventArgs(DataRow row, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
		: base(row, command, statementType, tableMapping)
	{
	}

	public new SingleStoreCommand? Command => (SingleStoreCommand?) base.Command!;
}

public sealed class SingleStoreRowUpdatedEventArgs : RowUpdatedEventArgs
{
	public SingleStoreRowUpdatedEventArgs(DataRow row, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
		: base(row, command, statementType, tableMapping)
	{
	}

	public new SingleStoreCommand? Command => (SingleStoreCommand?) base.Command;
}
