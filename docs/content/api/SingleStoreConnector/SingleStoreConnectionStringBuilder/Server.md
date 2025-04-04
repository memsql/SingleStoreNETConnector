# SingleStoreConnectionStringBuilder.Server property

The host name or network address of the SingleStore Server to which to connect. Multiple hosts can be specified in a comma-delimited list.

On Unix-like systems, this can be a fully qualified path to a SingleStore socket file, which will cause a Unix socket to be used instead of a TCP/IP socket. Only a single socket name can be specified.

```csharp
public string Server { get; set; }
```

## See Also

* class [SingleStoreConnectionStringBuilder](../SingleStoreConnectionStringBuilder.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
