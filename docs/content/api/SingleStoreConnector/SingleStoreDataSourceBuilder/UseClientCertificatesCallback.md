# SingleStoreDataSourceBuilder.UseClientCertificatesCallback method

Sets the callback used to provide client certificates for connecting to a server.

```csharp
public SingleStoreDataSourceBuilder UseClientCertificatesCallback(
    Func<X509CertificateCollection, ValueTask>? callback)
```

| parameter | description |
| --- | --- |
| callback | The callback that will provide client certificates. The X509CertificateCollection provided to the callback should be filled with the client certificate(s) needed to connect to the server. |

## Return Value

This builder, so that method calls can be chained.

## See Also

* class [SingleStoreDataSourceBuilder](../SingleStoreDataSourceBuilder.md)
* namespace [SingleStoreConnector](../../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
