using System.Diagnostics.CodeAnalysis;
using SingleStoreConnector.Core;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

/// <summary>
/// <para><see cref="SingleStoreBatch"/> implements the new
/// <a href="https://github.com/dotnet/runtime/issues/28633">ADO.NET batching API</a>.
/// <strong>It is currently experimental</strong> and may change in the future.</para>
/// <para>When using MariaDB (10.2 or later), the commands will be sent in a single batch, reducing network
/// round-trip time. With other SingleStore Servers, this may be no more efficient than executing the commands
/// individually.</para>
/// <para>Example usage:</para>
/// <code>
/// using var connection = new SingleStoreConnection("...connection string...");
/// await connection.OpenAsync();
///
/// using var batch = new SingleStoreBatch(connection)
/// {
/// 	BatchCommands =
/// 	{
/// 		new SingleStoreBatchCommand("INSERT INTO departments(name) VALUES(@name);")
/// 		{
/// 			Parameters =
/// 			{
/// 				new SingleStoreParameter("@name", "Sales"),
/// 			},
/// 		},
/// 		new SingleStoreBatchCommand("SET @dept_id = last_insert_id()"),
/// 		new SingleStoreBatchCommand("INSERT INTO employees(name, department_id) VALUES(@name, @dept_id);")
/// 		{
/// 			Parameters =
/// 			{
/// 				new SingleStoreParameter("@name", "Jim Halpert"),
/// 			},
/// 		},
/// 	 	new SingleStoreBatchCommand("INSERT INTO employees(name, department_id) VALUES(@name, @dept_id);")
/// 		{
/// 			Parameters =
/// 			{
/// 				new SingleStoreParameter("@name", "Dwight Schrute"),
/// 			},
/// 		},
/// 	},
///  };
///  await batch.ExecuteNonQueryAsync();
/// </code>
/// </summary>
/// <remarks>The proposed ADO.NET API that <see cref="SingleStoreBatch"/> is based on is not finalized. This API is experimental and may change in the future.</remarks>
#if NET6_0_OR_GREATER
#pragma warning disable CA1063 // Implement IDisposable Correctly
#endif
public sealed class SingleStoreBatch :
#if NET6_0_OR_GREATER
	DbBatch,
#endif
	ICancellableCommand, IDisposable
{
	/// <summary>
	/// Initializes a new <see cref="SingleStoreBatch"/> object. The <see cref="Connection"/> property must be set before this object can be used.
	/// </summary>
	public SingleStoreBatch()
		: this(null, null)
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SingleStoreBatch"/> object, setting the <see cref="Connection"/> and <see cref="Transaction"/> if specified.
	/// </summary>
	/// <param name="connection">(Optional) The <see cref="SingleStoreConnection"/> to use.</param>
	/// <param name="transaction">(Optional) The <see cref="SingleStoreTransaction"/> to use.</param>
	public SingleStoreBatch(SingleStoreConnection? connection = null, SingleStoreTransaction? transaction = null)
	{
		Connection = connection;
		Transaction = transaction;
		BatchCommands = new();
		m_commandId = ICancellableCommandExtensions.GetNextId();
	}

#if NET6_0_OR_GREATER
	public new SingleStoreConnection? Connection { get; set; }
	protected override DbConnection? DbConnection { get => Connection; set => Connection = (SingleStoreConnection?) value; }
	public new SingleStoreTransaction? Transaction { get; set; }
	protected override DbTransaction? DbTransaction { get => Transaction; set => Transaction = (SingleStoreTransaction?) value; }
#else
	public SingleStoreConnection? Connection { get; set; }
	public SingleStoreTransaction? Transaction { get; set; }
#endif

	/// <summary>
	/// The collection of commands that will be executed in the batch.
	/// </summary>
#if NET6_0_OR_GREATER
	public new SingleStoreBatchCommandCollection BatchCommands { get; }
	protected override DbBatchCommandCollection DbBatchCommands => BatchCommands;
#else
	public SingleStoreBatchCommandCollection BatchCommands { get; }
#endif

	/// <summary>
	/// Executes all the commands in the batch, returning a <see cref="SingleStoreDataReader"/> that can iterate
	/// over the result sets. If multiple resultsets are returned, use <see cref="SingleStoreDataReader.NextResult"/>
	/// to access them.
	/// </summary>
#if NET6_0_OR_GREATER
	public new SingleStoreDataReader ExecuteReader(CommandBehavior commandBehavior = CommandBehavior.Default) =>
#else
	public SingleStoreDataReader ExecuteReader(CommandBehavior commandBehavior = CommandBehavior.Default) =>
#endif
		(SingleStoreDataReader) ExecuteDbDataReader(commandBehavior);

	/// <summary>
	/// Executes all the commands in the batch, returning a <see cref="SingleStoreDataReader"/> that can iterate
	/// over the result sets. If multiple resultsets are returned, use <see cref="SingleStoreDataReader.NextResultAsync(CancellationToken)"/>
	/// to access them.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task{SingleStoreDataReader}"/> containing the result of the asynchronous operation.</returns>
#if NET6_0_OR_GREATER
	public new async Task<SingleStoreDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) =>
#else
	public async Task<SingleStoreDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) =>
#endif
		(SingleStoreDataReader) await ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

	//// TODO: new ExecuteReaderAsync(CommandBehavior)

#if NET6_0_OR_GREATER
	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
#else
	private DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
#endif
	{
		this.ResetCommandTimeout();
		return ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
	}

#if NET6_0_OR_GREATER
	protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
#else
	private async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
#endif
	{
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		return await ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken).ConfigureAwait(false);
	}

	private Task<SingleStoreDataReader> ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!IsValid(out var exception))
		 	return Task.FromException<SingleStoreDataReader>(exception);

		CurrentCommandBehavior = behavior;
		foreach (SingleStoreBatchCommand batchCommand in BatchCommands)
			batchCommand.Batch = this;

		var payloadCreator = Connection!.Session.SupportsComMulti ? BatchedCommandPayloadCreator.Instance :
			IsPrepared ? SingleCommandPayloadCreator.Instance :
			ConcatenatedCommandPayloadCreator.Instance;
		return CommandExecutor.ExecuteReaderAsync(BatchCommands!.Commands, payloadCreator, behavior, default, ioBehavior, cancellationToken);
	}

#if NET6_0_OR_GREATER
	public override int ExecuteNonQuery() =>
#else
	public int ExecuteNonQuery() =>
#endif
		ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

#if NET6_0_OR_GREATER
	public override object? ExecuteScalar() =>
#else
	public object? ExecuteScalar() =>
#endif
		ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

#if NET6_0_OR_GREATER
	public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) =>
#else
	public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) =>
#endif
		ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken);

#if NET6_0_OR_GREATER
	public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default) =>
#else
	public Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default) =>
#endif
		ExecuteScalarAsync(AsyncIOBehavior, cancellationToken);

	public
#if NET6_0_OR_GREATER
		override
#endif
		int Timeout
	{
		get => m_timeout;
		set
		{
			m_timeout = value;
			((ICancellableCommand) this).EffectiveCommandTimeout = null;
		}
	}

#if NET6_0_OR_GREATER
	public override void Prepare()
#else
	public void Prepare()
#endif
	{
		if (!NeedsPrepare(out var exception))
		{
			if (exception is not null)
				throw exception;
			return;
		}

		DoPrepareAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
	}

#if NET6_0_OR_GREATER
	public override Task PrepareAsync(CancellationToken cancellationToken = default) =>
#else
	public Task PrepareAsync(CancellationToken cancellationToken = default) =>
#endif
		PrepareAsync(AsyncIOBehavior, cancellationToken);

#if NET6_0_OR_GREATER
	public override void Cancel() =>
#else
	public void Cancel() =>
#endif
		Connection?.Cancel(this, m_commandId, true);

#if NET6_0_OR_GREATER
	protected override DbBatchCommand CreateDbBatchCommand() => new SingleStoreBatchCommand();
#endif

#if NET6_0_OR_GREATER
	public override void Dispose()
#else
	public void Dispose()
#endif
	{
		m_isDisposed = true;
#if NET6_0_OR_GREATER
		base.Dispose();
#endif
	}

	internal CommandBehavior CurrentCommandBehavior { get; set; }

	int ICancellableCommand.CommandId => m_commandId;
	int ICancellableCommand.CommandTimeout => Timeout;
	int? ICancellableCommand.EffectiveCommandTimeout { get; set; }
	int ICancellableCommand.CancelAttemptCount { get; set; }

	CancellationTokenRegistration ICancellableCommand.RegisterCancel(CancellationToken cancellationToken)
	{
		if (!cancellationToken.CanBeCanceled)
			return default;

		m_cancelAction ??= Cancel;
		return cancellationToken.Register(m_cancelAction);
	}

	void ICancellableCommand.SetTimeout(int milliseconds)
	{
		Volatile.Write(ref m_commandTimedOut, false);

		if (m_cancelTimerId != 0)
			TimerQueue.Instance.Remove(m_cancelTimerId);

		if (milliseconds != Constants.InfiniteTimeout)
		{
			m_cancelForCommandTimeoutAction ??= CancelCommandForTimeout;
			m_cancelTimerId = TimerQueue.Instance.Add(milliseconds, m_cancelForCommandTimeoutAction);
		}
	}

	bool ICancellableCommand.IsTimedOut => Volatile.Read(ref m_commandTimedOut);

	private void CancelCommandForTimeout()
	{
		Volatile.Write(ref m_commandTimedOut, true);
		Cancel();
	}

	private async Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		using var reader = await ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
		do
		{
			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
			}
		} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
		return reader.RecordsAffected;
	}

	private async Task<object?> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		var hasSetResult = false;
		object? result = null;
		using var reader = await ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
		do
		{
			var hasResult = await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			if (!hasSetResult)
			{
				if (hasResult)
					result = reader.GetValue(0);
				hasSetResult = true;
			}
		} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
		return result;
	}

	private bool IsValid([NotNullWhen(false)] out Exception? exception)
	{
		exception = null;
		if (m_isDisposed)
			exception = new ObjectDisposedException(GetType().Name);
		else if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
			exception = new InvalidOperationException($"Connection must be Open; current state is {Connection.State}");
		else if (!Connection.IgnoreCommandTransaction && Transaction != Connection.CurrentTransaction)
			exception = new InvalidOperationException("The transaction associated with this batch is not the connection's active transaction; see https://fl.vu/mysql-trans");
		else if (BatchCommands.Count == 0)
			exception = new InvalidOperationException("BatchCommands must contain a command");
		else
			exception = GetExceptionForInvalidCommands();

		return exception is null;
	}

	private bool NeedsPrepare(out Exception? exception)
	{
		if (m_isDisposed)
			exception = new ObjectDisposedException(GetType().Name);
		else if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State != ConnectionState.Open)
			exception = new InvalidOperationException($"Connection must be Open; current state is {Connection.State}");
		else if (BatchCommands.Count == 0)
			exception = new InvalidOperationException("BatchCommands must contain a command");
		else if (Connection.HasActiveReader)
			exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command; it must be closed first.");
		else
			exception = GetExceptionForInvalidCommands();

		return exception is null && !Connection!.IgnorePrepare;
	}

	private Exception? GetExceptionForInvalidCommands()
	{
		foreach (var command in BatchCommands)
		{
			if (command is null)
				return new InvalidOperationException("BatchCommands must not contain null");
			if (string.IsNullOrWhiteSpace(command.CommandText))
				return new InvalidOperationException("CommandText must be specified on each batch command");
		}
		return null;
	}

	private Task PrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!NeedsPrepare(out var exception))
			return exception is null ? Task.CompletedTask : Task.FromException(exception);

		return DoPrepareAsync(ioBehavior, cancellationToken);
	}

	private async Task DoPrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		foreach (ISingleStoreCommand batchCommand in BatchCommands)
		{
			if (batchCommand.CommandType != CommandType.Text)
				throw new NotSupportedException("Only CommandType.Text is currently supported by SingleStoreBatch.Prepare");
			((SingleStoreBatchCommand) batchCommand).Batch = this;

			// don't prepare the same SQL twice
			if (Connection!.Session.TryGetPreparedStatement(batchCommand.CommandText!) is null)
				await Connection.Session.PrepareAsync(batchCommand, ioBehavior, cancellationToken).ConfigureAwait(false);
		}
	}

	private bool IsPrepared
	{
		get
		{
			foreach (var command in BatchCommands)
			{
				if (Connection!.Session.TryGetPreparedStatement(command!.CommandText!) is null)
					return false;
			}
			return true;
		}
	}

	private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

	private readonly int m_commandId;
	private bool m_isDisposed;
	private int m_timeout;
	private Action? m_cancelAction;
	private Action? m_cancelForCommandTimeoutAction;
	private uint m_cancelTimerId;
	private bool m_commandTimedOut;
}
