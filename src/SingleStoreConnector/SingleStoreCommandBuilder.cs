using System.Globalization;
using SingleStoreConnector.Core;
using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector;

public sealed class SingleStoreCommandBuilder : DbCommandBuilder
{
	public static void DeriveParameters(SingleStoreCommand command) => DeriveParametersAsync(IOBehavior.Synchronous, command, CancellationToken.None).GetAwaiter().GetResult();
	public static Task DeriveParametersAsync(SingleStoreCommand command) => DeriveParametersAsync(command?.Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, command!, CancellationToken.None);
	public static Task DeriveParametersAsync(SingleStoreCommand command, CancellationToken cancellationToken) => DeriveParametersAsync(command?.Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, command!, cancellationToken);

	private static async Task DeriveParametersAsync(IOBehavior ioBehavior, SingleStoreCommand command, CancellationToken cancellationToken)
	{
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(command);
#else
		if (command is null)
			throw new ArgumentNullException(nameof(command));
#endif
		if (command.CommandType != CommandType.StoredProcedure)
			throw new ArgumentException($"SingleStoreCommand.CommandType must be StoredProcedure not {command.CommandType}", nameof(command));
		if (string.IsNullOrWhiteSpace(command.CommandText))
			throw new ArgumentException("SingleStoreCommand.CommandText must be set to a stored procedure name", nameof(command));
		if (command.Connection?.State != ConnectionState.Open)
			throw new ArgumentException("SingleStoreCommand.Connection must be an open connection.", nameof(command));

		// no-op check as MySQL reports at least 5.5.58
		if (command.Connection.Session.MySqlCompatVersion.Version < ServerVersions.SupportsProcedureCache)
			throw new NotSupportedException($"MySQL Server {command.Connection.Session.MySqlCompatVersion.OriginalString} doesn't support INFORMATION_SCHEMA");

		var cachedProcedure = await command.Connection.GetCachedProcedure(command.CommandText!, revalidateMissing: true, ioBehavior, cancellationToken).ConfigureAwait(false);
		if (cachedProcedure is null)
		{
			var name = NormalizedSchema.MustNormalize(command.CommandText!, command.Connection.Database);
			throw new SingleStoreException($"Procedure or function '{name.Component}' cannot be found in database '{name.Schema}'.");
		}

		command.Parameters.Clear();
		foreach (var cachedParameter in cachedProcedure.Parameters)
		{
			var parameter = command.Parameters.Add("@" + cachedParameter.Name, cachedParameter.SingleStoreDbType);
			parameter.Direction = cachedParameter.Direction;
			parameter.Size = cachedParameter.Length;
		}
	}

	public SingleStoreCommandBuilder()
	{
		GC.SuppressFinalize(this);
		QuotePrefix = "`";
		QuoteSuffix = "`";
	}

	public SingleStoreCommandBuilder(SingleStoreDataAdapter dataAdapter)
		: this()
	{
		DataAdapter = dataAdapter;
	}

	public new SingleStoreDataAdapter? DataAdapter
	{
		get => (SingleStoreDataAdapter?) base.DataAdapter;
		set => base.DataAdapter = value;
	}

	public new SingleStoreCommand GetDeleteCommand() => (SingleStoreCommand) base.GetDeleteCommand();
	public new SingleStoreCommand GetInsertCommand() => (SingleStoreCommand) base.GetInsertCommand();
	public new SingleStoreCommand GetUpdateCommand() => (SingleStoreCommand) base.GetUpdateCommand();

	protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
	{
		((SingleStoreParameter) parameter).SingleStoreDbType = (SingleStoreDbType) row[SchemaTableColumn.ProviderType];
	}

#if NET6_0_OR_GREATER
	protected override string GetParameterName(int parameterOrdinal) => string.Create(CultureInfo.InvariantCulture, $"@p{parameterOrdinal}");
#else
	protected override string GetParameterName(int parameterOrdinal) => FormattableString.Invariant($"@p{parameterOrdinal}");
#endif
	protected override string GetParameterName(string parameterName) => "@" + parameterName;
	protected override string GetParameterPlaceholder(int parameterOrdinal) => GetParameterName(parameterOrdinal);

	protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
	{
		if (!(adapter is SingleStoreDataAdapter mySqlDataAdapter))
			throw new ArgumentException("adapter needs to be a SingleStoreDataAdapter", nameof(adapter));

		if (adapter == DataAdapter)
			mySqlDataAdapter.RowUpdating -= RowUpdatingHandler;
		else
			mySqlDataAdapter.RowUpdating += RowUpdatingHandler;
	}

	public override string QuoteIdentifier(string unquotedIdentifier) => QuotePrefix + unquotedIdentifier.Replace("`", "``") + QuoteSuffix;

	public override string UnquoteIdentifier(string quotedIdentifier)
	{
		if (quotedIdentifier is ['`', .., '`'])
			quotedIdentifier = quotedIdentifier[1..^1];
		return quotedIdentifier.Replace("``", "`");
	}

	private void RowUpdatingHandler(object sender, SingleStoreRowUpdatingEventArgs e) => RowUpdatingHandler(e);
}
