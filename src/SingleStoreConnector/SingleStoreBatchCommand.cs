using SingleStoreConnector.Core;

namespace SingleStoreConnector;

#if !NET6_0_OR_GREATER
#pragma warning disable CA1822 // Mark members as static
#endif

public sealed class SingleStoreBatchCommand :
#if NET6_0_OR_GREATER
	DbBatchCommand,
#endif
	ISingleStoreCommand
{
	public SingleStoreBatchCommand()
		: this(null)
	{
	}

	public SingleStoreBatchCommand(string? commandText)
	{
		CommandText = commandText ?? "";
		CommandType = CommandType.Text;
	}

#if NET6_0_OR_GREATER
	public override string CommandText { get; set; }
#else
	public string CommandText { get; set; }
#endif
#if NET6_0_OR_GREATER
	public override CommandType CommandType { get; set; }
#else
	public CommandType CommandType { get; set; }
#endif
#if NET6_0_OR_GREATER
	public override int RecordsAffected =>
#else
	public int RecordsAffected =>
#endif
		0;

#if NET6_0_OR_GREATER
	public new SingleStoreParameterCollection Parameters =>
#else
	public SingleStoreParameterCollection Parameters =>
#endif
		m_parameterCollection ??= new();

#if NET6_0_OR_GREATER
	protected override DbParameterCollection DbParameterCollection => Parameters;
#endif

	bool ISingleStoreCommand.AllowUserVariables => false;

	CommandBehavior ISingleStoreCommand.CommandBehavior => Batch!.CurrentCommandBehavior;

	SingleStoreParameterCollection? ISingleStoreCommand.RawParameters => m_parameterCollection;

	SingleStoreAttributeCollection? ISingleStoreCommand.RawAttributes => null;

	SingleStoreConnection? ISingleStoreCommand.Connection => Batch?.Connection;

	long ISingleStoreCommand.LastInsertedId => m_lastInsertedId;

	PreparedStatements? ISingleStoreCommand.TryGetPreparedStatements() => null;

	void ISingleStoreCommand.SetLastInsertedId(long lastInsertedId) => m_lastInsertedId = lastInsertedId;

	SingleStoreParameterCollection? ISingleStoreCommand.OutParameters { get; set; }

	SingleStoreParameter? ISingleStoreCommand.ReturnParameter { get; set; }

	ICancellableCommand ISingleStoreCommand.CancellableCommand => Batch!;

	internal SingleStoreBatch? Batch { get; set; }

	private SingleStoreParameterCollection? m_parameterCollection;
	private long m_lastInsertedId;
}
