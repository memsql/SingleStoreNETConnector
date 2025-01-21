using SingleStoreConnector.Logging;
using SingleStoreConnector.Protocol;
using SingleStoreConnector.Protocol.Serialization;

namespace SingleStoreConnector.Core;

internal sealed class ConcatenatedCommandPayloadCreator : ICommandPayloadCreator
{
	public static ICommandPayloadCreator Instance { get; } = new ConcatenatedCommandPayloadCreator();

	public bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer, bool appendSemicolon)
	{
		if (commandListPosition.CommandIndex == commandListPosition.CommandCount)
			return false;

		writer.Write((byte) CommandKind.Query);

		// ConcatenatedCommandPayloadCreator is only used by SingleStoreBatch, and SingleStoreBatchCommand doesn't expose attributes,
		// so just write an empty attribute set if the server needs it.
		if (commandListPosition.CommandAt(commandListPosition.CommandIndex).Connection!.Session.SupportsQueryAttributes)
		{
			// attribute count
			writer.WriteLengthEncodedInteger(0);

			// attribute set count (always 1)
			writer.Write((byte) 1);
		}

		bool isComplete;
		do
		{
			var command = commandListPosition.CommandAt(commandListPosition.CommandIndex);
			Log.PreparingCommandPayload(command.Logger, command.Connection!.Session.Id, command.CommandText!);

			isComplete = SingleCommandPayloadCreator.WriteQueryPayload(command, cachedProcedures, writer,
				commandListPosition.CommandIndex < commandListPosition.CommandCount - 1 || appendSemicolon,
				commandListPosition.CommandIndex == 0,
				commandListPosition.CommandIndex == commandListPosition.CommandCount - 1);
			commandListPosition.CommandIndex++;
		} while (commandListPosition.CommandIndex < commandListPosition.CommandCount && isComplete);

		return true;
	}
}
