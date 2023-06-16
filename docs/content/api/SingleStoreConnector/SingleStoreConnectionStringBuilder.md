# SingleStoreConnectionStringBuilder class

[`SingleStoreConnectionStringBuilder`](./SingleStoreConnectionStringBuilder.md) allows you to construct a SingleStore connection string by setting properties on the builder then reading the ConnectionString property.

```csharp
public sealed class SingleStoreConnectionStringBuilder : DbConnectionStringBuilder
```

## Public Members

| name | description |
| --- | --- |
| [SingleStoreConnectionStringBuilder](SingleStoreConnectionStringBuilder/SingleStoreConnectionStringBuilder.md)() | Initializes a new [`SingleStoreConnectionStringBuilder`](./SingleStoreConnectionStringBuilder.md). |
| [SingleStoreConnectionStringBuilder](SingleStoreConnectionStringBuilder/SingleStoreConnectionStringBuilder.md)(…) | Initializes a new [`SingleStoreConnectionStringBuilder`](./SingleStoreConnectionStringBuilder.md) with properties set from the specified connection string. |
| [AllowLoadLocalInfile](SingleStoreConnectionStringBuilder/AllowLoadLocalInfile.md) { get; set; } | Allows the `LOAD DATA LOCAL` command to request files from the client. |
| [AllowPublicKeyRetrieval](SingleStoreConnectionStringBuilder/AllowPublicKeyRetrieval.md) { get; set; } | Allows the client to automatically request the RSA public key from the server. |
| [AllowUserVariables](SingleStoreConnectionStringBuilder/AllowUserVariables.md) { get; set; } | Allows user-defined variables (prefixed with `@`) to be used in SQL statements. |
| [AllowZeroDateTime](SingleStoreConnectionStringBuilder/AllowZeroDateTime.md) { get; set; } | Returns `DATETIME` fields as [`SingleStoreDateTime`](./SingleStoreDateTime.md) objects instead of DateTime objects. |
| [ApplicationName](SingleStoreConnectionStringBuilder/ApplicationName.md) { get; set; } | Sets the `program_name` connection attribute passed to SingleStore Server. |
| [AutoEnlist](SingleStoreConnectionStringBuilder/AutoEnlist.md) { get; set; } | Automatically enlists this connection in any active TransactionScope. |
| [CancellationTimeout](SingleStoreConnectionStringBuilder/CancellationTimeout.md) { get; set; } | The length of time (in seconds) to wait for a query to be canceled when [`CommandTimeout`](./SingleStoreCommand/CommandTimeout.md) expires, or zero for no timeout. |
| [CertificateFile](SingleStoreConnectionStringBuilder/CertificateFile.md) { get; set; } | The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication. |
| [CertificatePassword](SingleStoreConnectionStringBuilder/CertificatePassword.md) { get; set; } | The password for the certificate specified using the [`CertificateFile`](./SingleStoreConnectionStringBuilder/CertificateFile.md) option. Not required if the certificate file is not password protected. |
| [CertificateStoreLocation](SingleStoreConnectionStringBuilder/CertificateStoreLocation.md) { get; set; } | Uses a certificate from the specified Certificate Store on the machine. The default value of None means the certificate store is not used; a value of CurrentUser or LocalMachine uses the specified store. |
| [CertificateThumbprint](SingleStoreConnectionStringBuilder/CertificateThumbprint.md) { get; set; } | Specifies which certificate should be used from the Certificate Store specified in [`CertificateStoreLocation`](./SingleStoreConnectionStringBuilder/CertificateStoreLocation.md). This option must be used to indicate which certificate in the store should be used for authentication. |
| [CharacterSet](SingleStoreConnectionStringBuilder/CharacterSet.md) { get; set; } | Supported for backwards compatibility; SingleStoreConnector always uses `utf8mb4`. |
| [ConnectionAttributes](SingleStoreConnectionStringBuilder/ConnectionAttributes.md) { get; set; } | Sets connection attributes passed to SingleStore Server. |
| [ConnectionIdleTimeout](SingleStoreConnectionStringBuilder/ConnectionIdleTimeout.md) { get; set; } | The amount of time (in seconds) that a connection can remain idle in the pool. |
| [ConnectionLifeTime](SingleStoreConnectionStringBuilder/ConnectionLifeTime.md) { get; set; } | The maximum lifetime (in seconds) for any connection, or `0` for no lifetime limit. |
| [ConnectionProtocol](SingleStoreConnectionStringBuilder/ConnectionProtocol.md) { get; set; } | The protocol to use to connect to the SingleStore Server. |
| [ConnectionReset](SingleStoreConnectionStringBuilder/ConnectionReset.md) { get; set; } | Whether connections are reset when being retrieved from the pool. |
| [ConnectionTimeout](SingleStoreConnectionStringBuilder/ConnectionTimeout.md) { get; set; } | The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error. The default value is 15. |
| [ConvertZeroDateTime](SingleStoreConnectionStringBuilder/ConvertZeroDateTime.md) { get; set; } | Whether invalid `DATETIME` fields should be converted to MinValue. |
| [Database](SingleStoreConnectionStringBuilder/Database.md) { get; set; } | (Optional) The case-sensitive name of the initial database to use. This may be required if the SingleStore user account only has access rights to particular databases on the server. |
| [DateTimeKind](SingleStoreConnectionStringBuilder/DateTimeKind.md) { get; set; } | The [`DateTimeKind`](./SingleStoreConnectionStringBuilder/DateTimeKind.md) to use when deserializing `DATETIME` values. |
| [DefaultCommandTimeout](SingleStoreConnectionStringBuilder/DefaultCommandTimeout.md) { get; set; } | The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts. |
| [DnsCheckInterval](SingleStoreConnectionStringBuilder/DnsCheckInterval.md) { get; set; } | The number of seconds between checks for DNS changes, or 0 to disable periodic checks. |
| [ForceSynchronous](SingleStoreConnectionStringBuilder/ForceSynchronous.md) { get; set; } | Forces all async methods to execute synchronously. This can be useful for debugging. |
| [GuidFormat](SingleStoreConnectionStringBuilder/GuidFormat.md) { get; set; } | Determines which column type (if any) should be read as a Guid. |
| [IgnoreCommandTransaction](SingleStoreConnectionStringBuilder/IgnoreCommandTransaction.md) { get; set; } | Does not check the [`Transaction`](./SingleStoreCommand/Transaction.md) property for validity when executing a command. |
| [IgnorePrepare](SingleStoreConnectionStringBuilder/IgnorePrepare.md) { get; set; } | Ignores calls to [`Prepare`](./SingleStoreCommand/Prepare.md) and `PrepareAsync`. |
| [InteractiveSession](SingleStoreConnectionStringBuilder/InteractiveSession.md) { get; set; } | Instructs the SingleStore server that this is an interactive session. |
| override [Item](SingleStoreConnectionStringBuilder/Item.md) { get; set; } | Retrieves an option value by name. |
| [Keepalive](SingleStoreConnectionStringBuilder/Keepalive.md) { get; set; } | TCP Keepalive idle time (in seconds), or 0 to use OS defaults. |
| override [Keys](SingleStoreConnectionStringBuilder/Keys.md) { get; } | Returns an ICollection that contains the keys in the [`SingleStoreConnectionStringBuilder`](./SingleStoreConnectionStringBuilder.md). |
| [LoadBalance](SingleStoreConnectionStringBuilder/LoadBalance.md) { get; set; } | Specifies how load is distributed across backend servers. |
| [MaximumPoolSize](SingleStoreConnectionStringBuilder/MaximumPoolSize.md) { get; set; } | The maximum number of connections allowed in the pool. |
| [MinimumPoolSize](SingleStoreConnectionStringBuilder/MinimumPoolSize.md) { get; set; } | The minimum number of connections to leave in the pool if [`ConnectionIdleTimeout`](./SingleStoreConnectionStringBuilder/ConnectionIdleTimeout.md) is reached. |
| [NoBackslashEscapes](SingleStoreConnectionStringBuilder/NoBackslashEscapes.md) { get; set; } | Doesn't escape backslashes in string parameters. For use with the `NO_BACKSLASH_ESCAPES` SingleStore server mode. |
| [OldGuids](SingleStoreConnectionStringBuilder/OldGuids.md) { get; set; } | Use the [`GuidFormat`](./SingleStoreConnectionStringBuilder/GuidFormat.md) property instead. |
| [Password](SingleStoreConnectionStringBuilder/Password.md) { get; set; } | The password for the SingleStore user. |
| [PersistSecurityInfo](SingleStoreConnectionStringBuilder/PersistSecurityInfo.md) { get; set; } | If true, preserves security-sensitive information in the connection string retrieved from any open [`SingleStoreConnection`](./SingleStoreConnection.md). |
| [Pipelining](SingleStoreConnectionStringBuilder/Pipelining.md) { get; set; } | Enables query pipelining. |
| [PipeName](SingleStoreConnectionStringBuilder/PipeName.md) { get; set; } | The name of the Windows named pipe to use to connect to the server. You must also set [`ConnectionProtocol`](./SingleStoreConnectionStringBuilder/ConnectionProtocol.md) to NamedPipe to used named pipes. |
| [Pooling](SingleStoreConnectionStringBuilder/Pooling.md) { get; set; } | Enables connection pooling. |
| [Port](SingleStoreConnectionStringBuilder/Port.md) { get; set; } | The TCP port on which SingleStore Server is listening for connections. |
| [Server](SingleStoreConnectionStringBuilder/Server.md) { get; set; } | The host name or network address of the SingleStore Server to which to connect. Multiple hosts can be specified in a comma-delimited list. |
| [ServerRedirectionMode](SingleStoreConnectionStringBuilder/ServerRedirectionMode.md) { get; set; } | Whether to use server redirection. |
| [ServerRsaPublicKeyFile](SingleStoreConnectionStringBuilder/ServerRsaPublicKeyFile.md) { get; set; } | The path to a file containing the server's RSA public key. |
| [ServerSPN](SingleStoreConnectionStringBuilder/ServerSPN.md) { get; set; } | The server’s Service Principal Name (for `auth_gssapi_client` authentication). |
| [SslCa](SingleStoreConnectionStringBuilder/SslCa.md) { get; set; } | The path to a CA certificate file in a PEM Encoded (.pem) format. This should be used with a value for the [`SslMode`](./SingleStoreConnectionStringBuilder/SslMode.md) property of VerifyCA or VerifyFull to enable verification of a CA certificate that is not trusted by the operating system’s certificate store. |
| [SslCert](SingleStoreConnectionStringBuilder/SslCert.md) { get; set; } | The path to the client’s SSL certificate file in PEM format. [`SslKey`](./SingleStoreConnectionStringBuilder/SslKey.md) must also be specified, and [`CertificateFile`](./SingleStoreConnectionStringBuilder/CertificateFile.md) should not be. |
| [SslKey](SingleStoreConnectionStringBuilder/SslKey.md) { get; set; } | The path to the client’s SSL private key in PEM format. [`SslCert`](./SingleStoreConnectionStringBuilder/SslCert.md) must also be specified, and [`CertificateFile`](./SingleStoreConnectionStringBuilder/CertificateFile.md) should not be. |
| [SslMode](SingleStoreConnectionStringBuilder/SslMode.md) { get; set; } | Whether to use SSL/TLS when connecting to the SingleStore server. |
| [TlsCipherSuites](SingleStoreConnectionStringBuilder/TlsCipherSuites.md) { get; set; } | The TLS cipher suites which may be used during TLS negotiation. The default value (the empty string) allows the OS to determine the TLS cipher suites to use; this is the recommended setting. |
| [TlsVersion](SingleStoreConnectionStringBuilder/TlsVersion.md) { get; set; } | The TLS versions which may be used during TLS negotiation, or empty to use OS defaults. |
| [TreatChar48AsGeographyPoint](SingleStoreConnectionStringBuilder/TreatChar48AsGeographyPoint.md) { get; set; } | Determines whether CHAR(48) should be read as a GeographyPoint. |
| [TreatTinyAsBoolean](SingleStoreConnectionStringBuilder/TreatTinyAsBoolean.md) { get; set; } | Returns `TINYINT(1)` fields as Boolean values. |
| [UseAffectedRows](SingleStoreConnectionStringBuilder/UseAffectedRows.md) { get; set; } | Report changed rows instead of found rows. |
| [UseCompression](SingleStoreConnectionStringBuilder/UseCompression.md) { get; set; } | Compress packets sent to and from the server. |
| [UserID](SingleStoreConnectionStringBuilder/UserID.md) { get; set; } | The SingleStore user ID. |
| [UseXaTransactions](SingleStoreConnectionStringBuilder/UseXaTransactions.md) { get; set; } | Use XA transactions to implement TransactionScope distributed transactions. |
| override [ContainsKey](SingleStoreConnectionStringBuilder/ContainsKey.md)(…) | Whether this [`SingleStoreConnectionStringBuilder`](./SingleStoreConnectionStringBuilder.md) contains a set option with the specified name. |
| override [Remove](SingleStoreConnectionStringBuilder/Remove.md)(…) | Removes the option with the specified name. |

## Protected Members

| name | description |
| --- | --- |
| override [GetProperties](SingleStoreConnectionStringBuilder/GetProperties.md)(…) | Fills in *propertyDescriptors* with information about the available properties on this object. |

## Remarks

See [Connection String Options](https://mysqlconnector.net/connection-options/) for more documentation on the options.

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
