## How to Use

To integrate SingleStoreConnector with Microsoft.Extensions.Logging, add the following line of code to `Program.cs` method (before any `SingleStoreConnector` objects have been used):

```csharp
app.Services.UseSingleStoreConnectorLogging();
```

Alternatively, obtain an `ILoggerFactory` through dependency injection and add:

```csharp
SingleStoreConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```
