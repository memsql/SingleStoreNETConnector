using System.Diagnostics;
using System.Net.Sockets;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Core;

internal static class CommandExecutor
{
	public static async ValueTask<SingleStoreDataReader> ExecuteReaderAsync(CommandListPosition commandListPosition, ICommandPayloadCreator payloadCreator, CommandBehavior behavior, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var command = commandListPosition.CommandAt(0);

			// pre-requisite: Connection is non-null must be checked before calling this method
			var connection = command.Connection!;

			Log.CommandExecutorExecuteReader(command.Logger, connection.Session.Id, ioBehavior, commandListPosition.CommandCount);

			Dictionary<string, CachedProcedure?>? cachedProcedures = null;
			for (var commandIndex = 0; commandIndex < commandListPosition.CommandCount; commandIndex++)
			{
				var command2 = commandListPosition.CommandAt(commandIndex);
				if (command2.CommandType == CommandType.StoredProcedure)
				{
					cachedProcedures ??= [];
					var commandText = command2.CommandText!;
					if (!cachedProcedures.ContainsKey(commandText))
					{
						cachedProcedures.Add(commandText, await connection.GetCachedProcedure(commandText, revalidateMissing: false, ioBehavior, cancellationToken).ConfigureAwait(false));

						// because the connection was used to execute a SingleStoreDataReader with the connection's DefaultCommandTimeout,
						// we need to reapply the command's CommandTimeout (even if some of the time has elapsed)
						command.CancellableCommand.ResetCommandTimeout();
					}
				}
			}

			var writer = new ByteBufferWriter();
			//// cachedProcedures will be non-null if there is a stored procedure, which is also the only time it will be read
			if (!payloadCreator.WriteQueryCommand(ref commandListPosition, cachedProcedures!, writer, false))
				throw new InvalidOperationException("ICommandPayloadCreator failed to write query payload");

			cancellationToken.ThrowIfCancellationRequested();

			using var payload = writer.ToPayloadData();
			var session = connection.Session;
			session.StartQuerying(command.CancellableCommand);
			command.SetLastInsertedId(0);
			try
			{
				await session.SendAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
				await session.DataReader.InitAsync(commandListPosition, payloadCreator, cachedProcedures, command, behavior, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
				return session.DataReader;
			}
			catch (SingleStoreException ex) when (ex.ErrorCode == SingleStoreErrorCode.QueryInterrupted && cancellationToken.IsCancellationRequested)
			{
				Log.QueryWasInterrupted(command.Logger, session.Id);
				throw new OperationCanceledException(ex.Message, ex, cancellationToken);
			}
			catch (Exception ex) when (payload.Span.Length > 4_194_304 && (ex is SocketException or IOException or SingleStoreProtocolException))
			{
				// the default MySQL Server value for max_allowed_packet (in MySQL 5.7) is 4MiB: https://dev.mysql.com/doc/refman/5.7/en/server-system-variables.html#sysvar_max_allowed_packet
				// use "decimal megabytes" (to round up) when creating the exception message
				int megabytes = payload.Span.Length / 1_000_000;
				throw new SingleStoreException($"Error submitting {megabytes}MB packet; ensure 'max_allowed_packet' is greater than {megabytes}MB.", ex);
			}
			catch (SingleStoreException ex) when (ex.ErrorCode == SingleStoreErrorCode.QueryInterrupted)
			{
				Log.Trace("Session{0} got QueryInterrupted exception, but not because of the CommandTimeout or CancellationToken (CommandExecutor.cs)", connection.Session.Id);
				throw;
			}
		}
		catch (Exception ex) when (activity is { IsAllDataRequested: true })
		{
			activity.SetException(ex);
			activity.Stop();
			throw;
		}
	}
}
