namespace SingleStoreConnector.Core;

/// <summary>
/// <see cref="ISingleStoreCommand"/> provides an internal abstraction over <see cref="SingleStoreCommand"/> and <see cref="SingleStoreBatchCommand"/>.
/// </summary>
internal interface ISingleStoreCommand
{
	string? CommandText { get; }
	CommandType CommandType { get; }
	bool AllowUserVariables { get; }
	CommandBehavior CommandBehavior { get; }
	SingleStoreParameterCollection? RawParameters { get; }
	SingleStoreAttributeCollection? RawAttributes { get; }
	PreparedStatements? TryGetPreparedStatements();
	SingleStoreConnection? Connection { get; }
	long LastInsertedId { get; }
	void SetLastInsertedId(long lastInsertedId);
	SingleStoreParameterCollection? OutParameters { get; set; }
	SingleStoreParameter? ReturnParameter { get; set; }
	ICancellableCommand CancellableCommand { get; }
}

internal static class ISingleStoreCommandExtensions
{
	public static StatementPreparerOptions CreateStatementPreparerOptions(this ISingleStoreCommand command)
	{
		var connection = command.Connection!;
		var statementPreparerOptions = StatementPreparerOptions.None;
		if (connection.AllowUserVariables || command.CommandType == CommandType.StoredProcedure ||
		    command.AllowUserVariables)
			statementPreparerOptions |= StatementPreparerOptions.AllowUserVariables;
		if (connection.DateTimeKind == DateTimeKind.Utc)
			statementPreparerOptions |= StatementPreparerOptions.DateTimeUtc;
		else if (connection.DateTimeKind == DateTimeKind.Local)
			statementPreparerOptions |= StatementPreparerOptions.DateTimeLocal;
		if (command.CommandType == CommandType.StoredProcedure)
			statementPreparerOptions |= StatementPreparerOptions.AllowOutputParameters;
		if (connection.NoBackslashEscapes)
			statementPreparerOptions |= StatementPreparerOptions.NoBackslashEscapes;
		if (connection.TreatChar48AsGeographyPoint)
			statementPreparerOptions |= StatementPreparerOptions.TreatChar48AsGeographyPoint;

		statementPreparerOptions |= connection.GuidFormat switch
		{
			SingleStoreGuidFormat.Char36 => StatementPreparerOptions.GuidFormatChar36,
			SingleStoreGuidFormat.Char32 => StatementPreparerOptions.GuidFormatChar32,
			SingleStoreGuidFormat.Binary16 => StatementPreparerOptions.GuidFormatBinary16,
			SingleStoreGuidFormat.TimeSwapBinary16 => StatementPreparerOptions.GuidFormatTimeSwapBinary16,
			SingleStoreGuidFormat.LittleEndianBinary16 => StatementPreparerOptions.GuidFormatLittleEndianBinary16,
			_ => StatementPreparerOptions.None,
		};

		return statementPreparerOptions;
	}
}
