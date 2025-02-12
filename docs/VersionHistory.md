# SingleStoreConnector Version History

## Release Notes

### 1.2.0

Basically all the changes introduced with MySqlConnector 2.3.0

* Support .NET 8

* Drop support for .NET 4.6.1 and .NET Core 3.1

* Prevent connection pool falling back to an unsupported TLS version

* SingleStoreDataSource is now available for all TFMs, not just .NET 7.0. This provides a single place to configure a SingleStore connection and makes it easier to register SingleStoreConnection with dependency injection.
- Add SingleStoreDataSourceBuilder class to configure SingleStoreDataSource instances.
- Add SingleStoreDataSource.Name and SingleStoreDataSourceBuilder.UseName

* Microsoft.Extensions.Logging is now used as the core logging abstraction

* Expose connection pool metrics

* Remove COM_MULTI protocol support

* Use ValueTask in SingleStoreBulkCopy API for all TFMs

* Breaking This changes the return type of WriteToServerAsync from Task<SingleStoreBulkCopyResult> to ValueTask<SingleStoreBulkCopyResult> on .NET Framework

* Support multiple authentication methods when connecting

* Support per-query variables for CommandBehavior.SchemaOnly and SingleRow

* Recycle SingleStoreDataReader objects

* Implement faster parsing for result sets with multiple rows

* Optimize parameter encoding for ASCII strings

* Use TcpClient.ConnectAsync overload with CancellationToken on .NET 5.0 and later

* Fix cancellation when using a redirected connection

* Fix SingleStoreConnection.CloneWith for connections created from a SingleStoreDataSource

* Work around ephemeral PEM bug on Windows

* Reduce allocations on common code paths.

* Fix bug when column name begins with @ in SingleStoreBulkCopy

* Ignore SingleStoreDbType when serializing enum values

* Fix bug that didn't copy SingleStoreDataSource in SingleStoreConnection.Clone

* Fix potential error in reallocating an internal buffer when writing ASCII text.

* Update handling of ActivityStatus to latest conventions

* Reduce overhead of CommandTimeout

* Reword end-of-stream message to be more generic

### 1.1.6

* Resolved an issue where a package was being packed in the Debug configuration instead of Release.

### 1.1.4

* Get rid of a fix for MySQL KILL QUERY bug

### 1.1.3

* Minor release that resolves CommandTimeout issue introduced in 1.1.2

### 1.1.2

* Add `node_id` to `KILL QUERY {connection_id} {node_id}` command used in `Connection.Cancel()`.

### 1.1.1

* Add ConnectionAttributes parameter

### 1.1.0

* Support .NET 7.0

* Speed up inserts with SingleStoreDataAdapter

* Loop to read all data when decompressing

* Fix deadlock when cancelling a command

* Fix BulkCopy for DateOnly and TimeOnly

* Fix unintentional TLS downgrade

* Implement SingleStoreAttribute.Clone

* Normalize the order of keys in the connection string returned by `SingleStoreConnectionStringBuilder.ConnectionString`

* Drop support for .NET 4.5

* Fix a race condition in recovering leaked sessions

* Fix failure to dispose objects if an exception is thrown when connecting

### 1.0.1

* Allow RecordsAffected to be read after Close() in SingleStoreDataReader

* Strong name the assembly

### 1.0.0

* Add support for `GEOGRAPHY` and `GEOGRAPHYPOINT`

### 0.1.2-beta

* Initial beta release of SingleStore .NET connector
