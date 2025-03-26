using SingleStoreConnector.Protocol.Payloads;

namespace SingleStoreConnector.Core;

/// <summary>
/// <see cref="PreparedStatement"/> is a statement that has been prepared on the SingleStore Server.
/// </summary>
internal sealed class PreparedStatement(int statementId, ParsedStatement statement, ColumnDefinitionPayload[]? columns, ColumnDefinitionPayload[]? parameters)
{
	public int StatementId { get; } = statementId;
	public ParsedStatement Statement { get; } = statement;
	public ColumnDefinitionPayload[]? Columns { get; set; } = columns;
	public ColumnDefinitionPayload[]? Parameters { get; } = parameters;
}
