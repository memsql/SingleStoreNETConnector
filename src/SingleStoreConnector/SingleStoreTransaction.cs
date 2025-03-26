using System.Net.Sockets;
using SingleStoreConnector.Protocol.Serialization;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

#if !NET6_0_OR_GREATER
#pragma warning disable CA1822 // Mark members as static
#endif

/// <summary>
/// <see cref="SingleStoreTransaction"/> represents an in-progress transaction on a SingleStore Server.
/// </summary>
public sealed class SingleStoreTransaction : DbTransaction
{
	/// <summary>
	/// Commits the database transaction.
	/// </summary>
	public override void Commit() => CommitAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();

	/// <summary>
	/// Asynchronously commits the database transaction.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override Task CommitAsync(CancellationToken cancellationToken = default) => CommitAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
	public Task CommitAsync(CancellationToken cancellationToken = default) => CommitAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

	private async Task CommitAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyValid();

		using var activity = Connection!.Session.StartActivity("Commit");
		try
		{
			using (var cmd = new SingleStoreCommand("commit", Connection, this) { NoActivity = true })
				await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			Connection!.CurrentTransaction = null;
			Connection = null;
		}
		catch (Exception ex) when (activity is { IsAllDataRequested: true })
		{
			activity.SetException(ex);
			throw;
		}
	}

	/// <summary>
	/// Rolls back the database transaction.
	/// </summary>
	public override void Rollback() => RollbackAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();

	/// <summary>
	/// Asynchronously rolls back the database transaction.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override Task RollbackAsync(CancellationToken cancellationToken = default) => RollbackAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
	public Task RollbackAsync(CancellationToken cancellationToken = default) => RollbackAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

	private async Task RollbackAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyValid();

		await DoRollback(ioBehavior, cancellationToken).ConfigureAwait(false);
		Connection!.CurrentTransaction = null;
		Connection = null;
	}

	/// <summary>
	/// Removes the named transaction savepoint with the specified <paramref name="savepointName"/>. No commit or rollback occurs.
	/// </summary>
	/// <param name="savepointName">The savepoint name.</param>
	/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
#if NET5_0_OR_GREATER
	public override void Release(string savepointName) => ExecuteSavepointAsync("release ", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#else
	public void Release(string savepointName) => ExecuteSavepointAsync("release ", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#endif

	/// <summary>
	/// Asynchronously removes the named transaction savepoint with the specified <paramref name="savepointName"/>. No commit or rollback occurs.
	/// </summary>
	/// <param name="savepointName">The savepoint name.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
#if NET5_0_OR_GREATER
	public override Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("release ", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
	public Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("release ", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

	/// <summary>
	/// Rolls back the current transaction to the savepoint with the specified <paramref name="savepointName"/> without aborting the transaction.
	/// </summary>
	/// <param name="savepointName">The savepoint name.</param>
	/// <remarks><para>The name must have been created with <see cref="Save"/>, but not released by calling <see cref="Release"/>.</para>
	/// <para>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</para></remarks>
#if NET5_0_OR_GREATER
	public override void Rollback(string savepointName) => ExecuteSavepointAsync("rollback to ", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#else
	public void Rollback(string savepointName) => ExecuteSavepointAsync("rollback to ", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#endif

	/// <summary>
	/// Asynchronously rolls back the current transaction to the savepoint with the specified <paramref name="savepointName"/> without aborting the transaction.
	/// </summary>
	/// <param name="savepointName">The savepoint name.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	/// <remarks><para>The name must have been created with <see cref="SaveAsync"/>, but not released by calling <see cref="ReleaseAsync"/>.</para>
	/// <para>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</para></remarks>
#if NET5_0_OR_GREATER
	public override Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("rollback to ", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
	public Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("rollback to ", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

	/// <summary>
	/// Sets a named transaction savepoint with the specified <paramref name="savepointName"/>. If the current transaction
	/// already has a savepoint with the same name, the old savepoint is deleted and a new one is set.
	/// </summary>
	/// <param name="savepointName">The savepoint name.</param>
	/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
#if NET5_0_OR_GREATER
	public override void Save(string savepointName) => ExecuteSavepointAsync("", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#else
	public void Save(string savepointName) => ExecuteSavepointAsync("", savepointName, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#endif

	/// <summary>
	/// Asynchronously sets a named transaction savepoint with the specified <paramref name="savepointName"/>. If the current transaction
	/// already has a savepoint with the same name, the old savepoint is deleted and a new one is set.
	/// </summary>
	/// <param name="savepointName">The savepoint name.</param>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
	/// <remarks>The proposed ADO.NET API that this is based on is not finalized; this API may change in the future.</remarks>
#if NET5_0_OR_GREATER
	public override Task SaveAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#else
	public Task SaveAsync(string savepointName, CancellationToken cancellationToken = default) => ExecuteSavepointAsync("", savepointName, Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
#endif

	private static Task ExecuteSavepointAsync(string command, string savepointName, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		throw new SingleStoreConnector.SingleStoreException("Feature 'SAVEPOINT' is not supported by SingleStore");

		// VerifyValid();

		// if (savepointName is null)
		// 	throw new ArgumentNullException(nameof(savepointName));
		// if (savepointName.Length == 0)
		// 	throw new ArgumentException("savepointName must not be empty", nameof(savepointName));

		// using var cmd = new SingleStoreCommand(command + "savepoint " + QuoteIdentifier(savepointName), Connection, this) { NoActivity = true };
		// await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Gets the <see cref="SingleStoreConnection"/> that this transaction is associated with.
	/// </summary>
	public new SingleStoreConnection? Connection { get; private set; }

	/// <summary>
	/// Gets the <see cref="SingleStoreConnection"/> that this transaction is associated with.
	/// </summary>
	protected override DbConnection? DbConnection => Connection;

	/// <summary>
	/// Gets the <see cref="IsolationLevel"/> of this transaction. This value is set from <see cref="SingleStoreConnection.BeginTransaction(IsolationLevel)"/>
	/// or any other overload that specifies an <see cref="IsolationLevel"/>.
	/// </summary>
	public override IsolationLevel IsolationLevel { get; }

	/// <summary>
	/// Releases any resources associated with this transaction. If it was not committed, it will be rolled back.
	/// </summary>
	/// <param name="disposing"><c>true</c> if this method is being called from <c>Dispose</c>; <c>false</c> if being called from a finalizer.</param>
	protected override void Dispose(bool disposing)
	{
		try
		{
#pragma warning disable CA2012 // Safe because method completes synchronously
			if (disposing)
				DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	/// <summary>
	/// Asynchronously releases any resources associated with this transaction. If it was not committed, it will be rolled back.
	/// </summary>
	/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override ValueTask DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#else
	public Task DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#endif

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	internal ValueTask DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#else
	internal Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#endif
	{
		m_isDisposed = true;
		if (Connection?.CurrentTransaction == this)
			return DoDisposeAsync(ioBehavior, cancellationToken);
		Connection = null;
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return default;
#else
		return Task.CompletedTask;
#endif
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	private async ValueTask DoDisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#else
	private async Task DoDisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#endif
	{
		if (Connection?.CurrentTransaction == this)
		{
			if (Connection.State == ConnectionState.Open && Connection.Session.IsConnected)
			{
				try
				{
					await DoRollback(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				catch (IOException)
				{
				}
				catch (SocketException)
				{
				}
			}
			Connection.CurrentTransaction = null;
		}
		Connection = null;
	}

	internal SingleStoreTransaction(SingleStoreConnection connection, IsolationLevel isolationLevel)
	{
		Connection = connection;
		IsolationLevel = isolationLevel;
	}

	private async Task DoRollback(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		using var activity = Connection!.Session.StartActivity("Rollback");
		try
		{
			using var cmd = new SingleStoreCommand("rollback", Connection, this) { NoActivity = true };
			await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex) when (activity is { IsAllDataRequested: true })
		{
			activity.SetException(ex);
			throw;
		}
	}

	private void VerifyValid()
	{
#if NET7_0_OR_GREATER
		ObjectDisposedException.ThrowIf(m_isDisposed, this);
#else
		if (m_isDisposed)
			throw new ObjectDisposedException(nameof(SingleStoreTransaction));
#endif
		if (Connection is null)
			throw new InvalidOperationException("Already committed or rolled back.");
		if (Connection.CurrentTransaction is null)
			throw new InvalidOperationException("There is no active transaction.");
		if (!object.ReferenceEquals(Connection.CurrentTransaction, this))
			throw new InvalidOperationException("This is not the active transaction.");
	}

	private static string QuoteIdentifier(string identifier) => "`" + identifier.Replace("`", "``") + "`";

	private bool m_isDisposed;
}
