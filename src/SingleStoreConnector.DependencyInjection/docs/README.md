## About

SingleStoreConnector.DependencyInjection helps set up SingleStoreConnector in applications that use dependency injection, most notably in ASP.NET.
It allows easy configuration of your SingleStore connections and registers the appropriate services in your DI container.
It also configures logging by integrating SingleStoreConnector with the `ILoggingFactory` registered with the service provider.

## How to Use

For example, if using the ASP.NET minimal web API, use the following to register SingleStoreConnector:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleStoreDataSource(builder.Configuration.GetConnectionString("Default"));
```

This registers a transient `SingleStoreConnection` which can get injected into your controllers:

```csharp
app.MapGet("/", async (SingleStoreConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM users LIMIT 1";
    return "Hello World: " + await command.ExecuteScalarAsync();
});
```

You can use `SingleStoreDataSource` directly if you need more than one connection:

```csharp
app.MapGet("/", async (SingleStoreDataSource dataSource) =>
{
    await using var connection1 = await dataSource.OpenConnectionAsync();
    await using var connection2 = await dataSource.OpenConnectionAsync();
    // use the two connections...
});
```

## Advanced Usage

The `AddSingleStoreDataSource` method also accepts a lambda parameter allowing you to configure aspects of SingleStoreConnector beyond the connection string.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleStoreDataSource("Server=server;User ID=test;Password=test;Database=test",
	x => x.UseRemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { /* custom logic */ })
);
```
