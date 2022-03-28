# SingleStoreConnector assembly

## SingleStoreConnector namespace

| public type | description |
| --- | --- |
| class [SingleStoreAttribute](./SingleStoreConnector/SingleStoreAttribute.md) | [`SingleStoreAttribute`](./SingleStoreConnector/SingleStoreAttribute.md) represents an attribute that can be sent with a SingleStore query. |
| class [SingleStoreAttributeCollection](./SingleStoreConnector/SingleStoreAttributeCollection.md) | [`SingleStoreAttributeCollection`](./SingleStoreConnector/SingleStoreAttributeCollection.md) represents a collection of query attributes that can be added to a [`SingleStoreCommand`](./SingleStoreConnector/SingleStoreCommand.md). |
| class [SingleStoreBatch](./SingleStoreConnector/SingleStoreBatch.md) | [`SingleStoreBatch`](./SingleStoreConnector/SingleStoreBatch.md) implements the new [ADO.NET batching API](https://github.com/dotnet/runtime/issues/28633). It is currently experimental and may change in the future. |
| class [SingleStoreBatchCommand](./SingleStoreConnector/SingleStoreBatchCommand.md) |  |
| class [SingleStoreBatchCommandCollection](./SingleStoreConnector/SingleStoreBatchCommandCollection.md) |  |
| class [SingleStoreBulkCopy](./SingleStoreConnector/SingleStoreBulkCopy.md) | [`SingleStoreBulkCopy`](./SingleStoreConnector/SingleStoreBulkCopy.md) lets you efficiently load a SingleStore Server table with data from another source. It is similar to the [SqlBulkCopy](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy) class for SQL Server. |
| class [SingleStoreBulkCopyColumnMapping](./SingleStoreConnector/SingleStoreBulkCopyColumnMapping.md) | Use [`SingleStoreBulkCopyColumnMapping`](./SingleStoreConnector/SingleStoreBulkCopyColumnMapping.md) to specify how to map columns in the source data to columns in the destination table when using [`SingleStoreBulkCopy`](./SingleStoreConnector/SingleStoreBulkCopy.md). |
| class [SingleStoreBulkCopyResult](./SingleStoreConnector/SingleStoreBulkCopyResult.md) | Represents the result of a [`SingleStoreBulkCopy`](./SingleStoreConnector/SingleStoreBulkCopy.md) operation. |
| class [SingleStoreBulkLoader](./SingleStoreConnector/SingleStoreBulkLoader.md) | [`SingleStoreBulkLoader`](./SingleStoreConnector/SingleStoreBulkLoader.md) lets you efficiently load a SingleStore Server Table with data from a CSV or TSV file or Stream. |
| enum [SingleStoreBulkLoaderConflictOption](./SingleStoreConnector/SingleStoreBulkLoaderConflictOption.md) |  |
| enum [SingleStoreBulkLoaderPriority](./SingleStoreConnector/SingleStoreBulkLoaderPriority.md) |  |
| enum [SingleStoreCertificateStoreLocation](./SingleStoreConnector/SingleStoreCertificateStoreLocation.md) |  |
| class [SingleStoreCommand](./SingleStoreConnector/SingleStoreCommand.md) | [`SingleStoreCommand`](./SingleStoreConnector/SingleStoreCommand.md) represents a SQL statement or stored procedure name to execute against a SingleStore database. |
| class [SingleStoreCommandBuilder](./SingleStoreConnector/SingleStoreCommandBuilder.md) |  |
| class [SingleStoreConnection](./SingleStoreConnector/SingleStoreConnection.md) | [`SingleStoreConnection`](./SingleStoreConnector/SingleStoreConnection.md) represents a connection to a SingleStore database. |
| enum [SingleStoreConnectionProtocol](./SingleStoreConnector/SingleStoreConnectionProtocol.md) | Specifies the type of connection to make to the server. |
| class [SingleStoreConnectionStringBuilder](./SingleStoreConnector/SingleStoreConnectionStringBuilder.md) | [`SingleStoreConnectionStringBuilder`](./SingleStoreConnector/SingleStoreConnectionStringBuilder.md) allows you to construct a SingleStore connection string by setting properties on the builder then reading the ConnectionString property. |
| class [SingleStoreConnectorFactory](./SingleStoreConnector/SingleStoreConnectorFactory.md) | An implementation of DbProviderFactory that creates SingleStoreConnector objects. |
| class [SingleStoreConversionException](./SingleStoreConnector/SingleStoreConversionException.md) | [`SingleStoreConversionException`](./SingleStoreConnector/SingleStoreConversionException.md) is thrown when a SingleStore value can't be converted to another type. |
| class [SingleStoreDataAdapter](./SingleStoreConnector/SingleStoreDataAdapter.md) |  |
| class [SingleStoreDataReader](./SingleStoreConnector/SingleStoreDataReader.md) |  |
| struct [SingleStoreDateTime](./SingleStoreConnector/SingleStoreDateTime.md) | Represents a SingleStore date/time value. This type can be used to store `DATETIME` values such as `0000-00-00` that can be stored in SingleStore (when [`AllowZeroDateTime`](./SingleStoreConnector/SingleStoreConnectionStringBuilder/AllowZeroDateTime.md) is true) but can't be stored in a DateTime value. |
| enum [SingleStoreDateTimeKind](./SingleStoreConnector/SingleStoreDateTimeKind.md) | The DateTimeKind used when reading DateTime from the database. |
| class [SingleStoreDbColumn](./SingleStoreConnector/SingleStoreDbColumn.md) |  |
| enum [SingleStoreDbType](./SingleStoreConnector/SingleStoreDbType.md) |  |
| struct [SingleStoreDecimal](./SingleStoreConnector/SingleStoreDecimal.md) | [`SingleStoreDecimal`](./SingleStoreConnector/SingleStoreDecimal.md) represents a SingleStore `DECIMAL` value that is too large to fit in a .NET Decimal. |
| class [SingleStoreError](./SingleStoreConnector/SingleStoreError.md) | [`SingleStoreError`](./SingleStoreConnector/SingleStoreError.md) represents an error or warning that occurred during the execution of a SQL statement. |
| enum [SingleStoreErrorCode](./SingleStoreConnector/SingleStoreErrorCode.md) | SingleStore Server error codes. Taken from [Server Error Codes and Messages](https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html). |
| class [SingleStoreException](./SingleStoreConnector/SingleStoreException.md) | [`SingleStoreException`](./SingleStoreConnector/SingleStoreException.md) is thrown when SingleStore Server returns an error code, or there is a communication error with the server. |
| class [SingleStoreGeometry](./SingleStoreConnector/SingleStoreGeometry.md) | Represents SingleStore's internal GEOMETRY format: https://dev.mysql.com/doc/refman/8.0/en/gis-data-formats.html#gis-internal-format |
| enum [SingleStoreGuidFormat](./SingleStoreConnector/SingleStoreGuidFormat.md) | Determines which column type (if any) should be read as a `System.Guid`. |
| class [SingleStoreHelper](./SingleStoreConnector/SingleStoreHelper.md) |  |
| class [SingleStoreInfoMessageEventArgs](./SingleStoreConnector/SingleStoreInfoMessageEventArgs.md) | [`SingleStoreInfoMessageEventArgs`](./SingleStoreConnector/SingleStoreInfoMessageEventArgs.md) contains the data supplied to the [`SingleStoreInfoMessageEventHandler`](./SingleStoreConnector/SingleStoreInfoMessageEventHandler.md) event handler. |
| delegate [SingleStoreInfoMessageEventHandler](./SingleStoreConnector/SingleStoreInfoMessageEventHandler.md) | Defines the event handler for [`InfoMessage`](./SingleStoreConnector/SingleStoreConnection/InfoMessage.md). |
| enum [SingleStoreLoadBalance](./SingleStoreConnector/SingleStoreLoadBalance.md) |  |
| class [SingleStoreParameter](./SingleStoreConnector/SingleStoreParameter.md) |  |
| class [SingleStoreParameterCollection](./SingleStoreConnector/SingleStoreParameterCollection.md) |  |
| class [SingleStoreProtocolException](./SingleStoreConnector/SingleStoreProtocolException.md) | [`SingleStoreProtocolException`](./SingleStoreConnector/SingleStoreProtocolException.md) is thrown when there is an internal protocol error communicating with SingleStore Server. |
| class [SingleStoreProvidePasswordContext](./SingleStoreConnector/SingleStoreProvidePasswordContext.md) | Provides context for the [`ProvidePasswordCallback`](./SingleStoreConnector/SingleStoreConnection/ProvidePasswordCallback.md) delegate. |
| class [SingleStoreRowsCopiedEventArgs](./SingleStoreConnector/SingleStoreRowsCopiedEventArgs.md) |  |
| delegate [SingleStoreRowsCopiedEventHandler](./SingleStoreConnector/SingleStoreRowsCopiedEventHandler.md) | Represents the method that handles the [`SingleStoreRowsCopied`](./SingleStoreConnector/SingleStoreBulkCopy/SingleStoreRowsCopied.md) event of a [`SingleStoreBulkCopy`](./SingleStoreConnector/SingleStoreBulkCopy.md). |
| class [SingleStoreRowUpdatedEventArgs](./SingleStoreConnector/SingleStoreRowUpdatedEventArgs.md) |  |
| delegate [SingleStoreRowUpdatedEventHandler](./SingleStoreConnector/SingleStoreRowUpdatedEventHandler.md) |  |
| class [SingleStoreRowUpdatingEventArgs](./SingleStoreConnector/SingleStoreRowUpdatingEventArgs.md) |  |
| delegate [SingleStoreRowUpdatingEventHandler](./SingleStoreConnector/SingleStoreRowUpdatingEventHandler.md) |  |
| enum [SingleStoreServerRedirectionMode](./SingleStoreConnector/SingleStoreServerRedirectionMode.md) | Server redirection configuration. |
| enum [SingleStoreSslMode](./SingleStoreConnector/SingleStoreSslMode.md) | SSL connection options. |
| class [SingleStoreTransaction](./SingleStoreConnector/SingleStoreTransaction.md) | [`SingleStoreTransaction`](./SingleStoreConnector/SingleStoreTransaction.md) represents an in-progress transaction on a SingleStore Server. |

## SingleStoreConnector.Authentication namespace

| public type | description |
| --- | --- |
| static class [AuthenticationPlugins](./SingleStoreConnector.Authentication/AuthenticationPlugins.md) | A registry of known authentication plugins. |
| interface [IAuthenticationPlugin](./SingleStoreConnector.Authentication/IAuthenticationPlugin.md) | The primary interface implemented by an authentication plugin. |

## SingleStoreConnector.Logging namespace

| public type | description |
| --- | --- |
| class [ConsoleLoggerProvider](./SingleStoreConnector.Logging/ConsoleLoggerProvider.md) |  |
| interface [ISingleStoreConnectorLogger](./SingleStoreConnector.Logging/ISingleStoreConnectorLogger.md) | Implementations of [`ISingleStoreConnectorLogger`](./SingleStoreConnector.Logging/ISingleStoreConnectorLogger.md) write logs to a particular target. |
| interface [ISingleStoreConnectorLoggerProvider](./SingleStoreConnector.Logging/ISingleStoreConnectorLoggerProvider.md) | Implementations of [`ISingleStoreConnectorLoggerProvider`](./SingleStoreConnector.Logging/ISingleStoreConnectorLoggerProvider.md) create logger instances. |
| class [NoOpLogger](./SingleStoreConnector.Logging/NoOpLogger.md) | [`NoOpLogger`](./SingleStoreConnector.Logging/NoOpLogger.md) is an implementation of [`ISingleStoreConnectorLogger`](./SingleStoreConnector.Logging/ISingleStoreConnectorLogger.md) that does nothing. |
| class [NoOpLoggerProvider](./SingleStoreConnector.Logging/NoOpLoggerProvider.md) | Creates loggers that do nothing. |
| enum [SingleStoreConnectorLogLevel](./SingleStoreConnector.Logging/SingleStoreConnectorLogLevel.md) |  |
| static class [SingleStoreConnectorLogManager](./SingleStoreConnector.Logging/SingleStoreConnectorLogManager.md) | Controls logging for SingleStoreConnector. |

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
