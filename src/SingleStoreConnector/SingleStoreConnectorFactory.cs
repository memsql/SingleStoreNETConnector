namespace SingleStoreConnector;

/// <summary>
/// An implementation of <see cref="DbProviderFactory"/> that creates SingleStoreConnector objects.
/// </summary>
public sealed class SingleStoreConnectorFactory : DbProviderFactory
{
	/// <summary>
	/// Provides an instance of <see cref="DbProviderFactory"/> that can create SingleStoreConnector objects.
	/// </summary>
	public static readonly SingleStoreConnectorFactory Instance = new();

	/// <summary>
	/// Creates a new <see cref="SingleStoreCommand"/> object.
	/// </summary>
	public override DbCommand CreateCommand() => new SingleStoreCommand();

	/// <summary>
	/// Creates a new <see cref="SingleStoreConnection"/> object.
	/// </summary>
	public override DbConnection CreateConnection() => new SingleStoreConnection();

	/// <summary>
	/// Creates a new <see cref="SingleStoreConnectionStringBuilder"/> object.
	/// </summary>
	public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SingleStoreConnectionStringBuilder();

	/// <summary>
	/// Creates a new <see cref="SingleStoreParameter"/> object.
	/// </summary>
	public override DbParameter CreateParameter() => new SingleStoreParameter();

	/// <summary>
	/// Creates a new <see cref="SingleStoreCommandBuilder"/> object.
	/// </summary>
	public override DbCommandBuilder CreateCommandBuilder() => new SingleStoreCommandBuilder();

	/// <summary>
	/// Creates a new <see cref="SingleStoreDataAdapter"/> object.
	/// </summary>
	public override DbDataAdapter CreateDataAdapter() => new SingleStoreDataAdapter();

	/// <summary>
	/// Returns <c>false</c>.
	/// </summary>
	/// <remarks><see cref="DbDataSourceEnumerator"/> is not supported by SingleStoreConnector.</remarks>
	public override bool CanCreateDataSourceEnumerator => false;

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	/// <summary>
	/// Returns <c>true</c>.
	/// </summary>
	public override bool CanCreateCommandBuilder => true;

	/// <summary>
	/// Returns <c>true</c>.
	/// </summary>
	public override bool CanCreateDataAdapter => true;
#endif

#pragma warning disable CA1822 // Mark members as static
	/// <summary>
	/// Creates a new <see cref="SingleStoreBatch"/> object.
	/// </summary>
#if NET6_0_OR_GREATER
	public override DbBatch CreateBatch() => new SingleStoreBatch();
#else
	public SingleStoreBatch CreateBatch() => new SingleStoreBatch();
#endif

	/// <summary>
	/// Creates a new <see cref="SingleStoreBatchCommand"/> object.
	/// </summary>
#if NET6_0_OR_GREATER
	public override DbBatchCommand CreateBatchCommand() => new SingleStoreBatchCommand();
#else
	public SingleStoreBatchCommand CreateBatchCommand() => new SingleStoreBatchCommand();
#endif

	/// <summary>
	/// Returns <c>true</c>.
	/// </summary>
#if NET6_0_OR_GREATER
	public override bool CanCreateBatch => true;
#else
	public bool CanCreateBatch => true;
#endif
#pragma warning restore CA1822 // Mark members as static

#if NET7_0_OR_GREATER
	/// <summary>
	/// Creates a new <see cref="SingleStoreDataSource"/> object.
	/// </summary>
	/// <param name="connectionString">The connection string.</param>
	public override DbDataSource CreateDataSource(string connectionString) => new SingleStoreDataSource(connectionString);
#endif

	private SingleStoreConnectorFactory()
	{
	}
}
