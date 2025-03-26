namespace SingleStoreConnector.Core;

/// <summary>
/// <see cref="CommandListPosition"/> encapsulates a list of <see cref="ISingleStoreCommand"/> and the current position within that list.
/// </summary>
internal struct CommandListPosition
{
	public CommandListPosition(object commands)
	{
		m_commands = commands;
		CommandCount = commands switch
		{
			SingleStoreCommand _ => 1,
			IReadOnlyList<SingleStoreBatchCommand> list => list.Count,
			_ => 0,
		};
		PreparedStatements = null;
		CommandIndex = 0;
		PreparedStatementIndex = 0;
	}

	public readonly ISingleStoreCommand CommandAt(int index) =>
		m_commands switch
		{
			SingleStoreCommand command when index is 0 => command,
			IReadOnlyList<SingleStoreBatchCommand> list => list[index],
			_ => throw new ArgumentOutOfRangeException(nameof(index)),
		};

	/// <summary>
	/// The commands in this list; either a singular <see cref="SingleStoreCommand"/> or a <see cref="IReadOnlyList{SingleStoreBatchCommand}"/>.
	/// </summary>
	private readonly object m_commands;

	/// <summary>
	/// The number of commands in the list.
	/// </summary>
	public readonly int CommandCount;

	/// <summary>
	/// Associated prepared statements of commands
	/// </summary>
	public PreparedStatements? PreparedStatements;

	/// <summary>
	/// The index of the current command.
	/// </summary>
	public int CommandIndex;

	/// <summary>
	/// If the current command is a prepared statement, the index of the current prepared statement for that command.
	/// </summary>
	public int PreparedStatementIndex;

	/// <summary>
	/// Retrieve the last used prepared statement
	/// </summary>
	public PreparedStatement? LastUsedPreparedStatement;
}
