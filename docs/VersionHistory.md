# SingleStoreConnector Version History

## Release Notes

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
