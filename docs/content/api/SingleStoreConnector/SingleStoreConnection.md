# SingleStoreConnection class

[`SingleStoreConnection`](./SingleStoreConnection.md) represents a connection to a SingleStore database.

```csharp
public sealed class SingleStoreConnection : DbConnection, ICloneable
```

## Public Members

| name | description |
| --- | --- |
| [SingleStoreConnection](SingleStoreConnection/SingleStoreConnection.md)() | The default constructor. |
| [SingleStoreConnection](SingleStoreConnection/SingleStoreConnection.md)(…) |  |
| override [CanCreateBatch](SingleStoreConnection/CanCreateBatch.md) { get; } |  |
| override [ConnectionString](SingleStoreConnection/ConnectionString.md) { get; set; } |  |
| override [ConnectionTimeout](SingleStoreConnection/ConnectionTimeout.md) { get; } | Gets the time (in seconds) to wait while trying to establish a connection before terminating the attempt and generating an error. This value is controlled by [`ConnectionTimeout`](./SingleStoreConnectionStringBuilder/ConnectionTimeout.md), which defaults to 15 seconds. |
| override [Database](SingleStoreConnection/Database.md) { get; } |  |
| override [DataSource](SingleStoreConnection/DataSource.md) { get; } |  |
| [ProvideClientCertificatesCallback](SingleStoreConnection/ProvideClientCertificatesCallback.md) { get; set; } | Gets or sets the delegate used to provide client certificates for connecting to a server. |
| [ProvidePasswordCallback](SingleStoreConnection/ProvidePasswordCallback.md) { get; set; } | Gets or sets the delegate used to generate a password for new database connections. |
| [RemoteCertificateValidationCallback](SingleStoreConnection/RemoteCertificateValidationCallback.md) { get; set; } | Gets or sets the delegate used to verify that the server's certificate is valid. |
| [S2ServerVersion](SingleStoreConnection/S2ServerVersion.md) { get; } |  |
| [ServerThread](SingleStoreConnection/ServerThread.md) { get; } | The connection ID from SingleStore Server. |
| override [ServerVersion](SingleStoreConnection/ServerVersion.md) { get; } |  |
| override [State](SingleStoreConnection/State.md) { get; } |  |
| event [InfoMessage](SingleStoreConnection/InfoMessage.md) |  |
| [BeginTransaction](SingleStoreConnection/BeginTransaction.md)() | Begins a database transaction. |
| [BeginTransaction](SingleStoreConnection/BeginTransaction.md)(…) | Begins a database transaction. (2 methods) |
| [BeginTransactionAsync](SingleStoreConnection/BeginTransactionAsync.md)(…) | Begins a database transaction asynchronously. (3 methods) |
| override [ChangeDatabase](SingleStoreConnection/ChangeDatabase.md)(…) |  |
| override [ChangeDatabaseAsync](SingleStoreConnection/ChangeDatabaseAsync.md)(…) |  |
| [Clone](SingleStoreConnection/Clone.md)() |  |
| [CloneWith](SingleStoreConnection/CloneWith.md)(…) | Returns an unopened copy of this connection with a new connection string. If the `Password` in *connectionString* is not set, the password from this connection will be used. This allows creating a new connection with the same security information while changing other options, such as database or pooling. |
| override [Close](SingleStoreConnection/Close.md)() |  |
| override [CloseAsync](SingleStoreConnection/CloseAsync.md)() |  |
| [CreateBatch](SingleStoreConnection/CreateBatch.md)() | Creates a [`SingleStoreBatch`](./SingleStoreBatch.md) object for executing batched commands. |
| [CreateCommand](SingleStoreConnection/CreateCommand.md)() |  |
| override [DisposeAsync](SingleStoreConnection/DisposeAsync.md)() |  |
| override [EnlistTransaction](SingleStoreConnection/EnlistTransaction.md)(…) |  |
| override [GetSchema](SingleStoreConnection/GetSchema.md)() | Returns schema information for the data source of this [`SingleStoreConnection`](./SingleStoreConnection.md). |
| override [GetSchema](SingleStoreConnection/GetSchema.md)(…) | Returns schema information for the data source of this [`SingleStoreConnection`](./SingleStoreConnection.md). (2 methods) |
| override [GetSchemaAsync](SingleStoreConnection/GetSchemaAsync.md)(…) | Asynchronously returns schema information for the data source of this [`SingleStoreConnection`](./SingleStoreConnection.md). (3 methods) |
| override [Open](SingleStoreConnection/Open.md)() |  |
| override [OpenAsync](SingleStoreConnection/OpenAsync.md)(…) |  |
| [Ping](SingleStoreConnection/Ping.md)() |  |
| [PingAsync](SingleStoreConnection/PingAsync.md)(…) |  |
| [ResetConnectionAsync](SingleStoreConnection/ResetConnectionAsync.md)(…) | Resets the session state of the current open connection; this clears temporary tables and user-defined variables. |
| static [ClearAllPools](SingleStoreConnection/ClearAllPools.md)() | Clears all connection pools. |
| static [ClearAllPoolsAsync](SingleStoreConnection/ClearAllPoolsAsync.md)(…) | Asynchronously clears all connection pools. |
| static [ClearPool](SingleStoreConnection/ClearPool.md)(…) | Clears the connection pool that *connection* belongs to. |
| static [ClearPoolAsync](SingleStoreConnection/ClearPoolAsync.md)(…) | Asynchronously clears the connection pool that *connection* belongs to. |

## Protected Members

| name | description |
| --- | --- |
| override [DbProviderFactory](SingleStoreConnection/DbProviderFactory.md) { get; } |  |
| override [BeginDbTransaction](SingleStoreConnection/BeginDbTransaction.md)(…) | Begins a database transaction. |
| override [BeginDbTransactionAsync](SingleStoreConnection/BeginDbTransactionAsync.md)(…) | Begins a database transaction asynchronously. |
| override [CreateDbBatch](SingleStoreConnection/CreateDbBatch.md)() |  |
| override [CreateDbCommand](SingleStoreConnection/CreateDbCommand.md)() |  |
| override [Dispose](SingleStoreConnection/Dispose.md)(…) |  |

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
