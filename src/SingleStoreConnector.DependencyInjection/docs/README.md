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

## Keyed Services

Use the `AddKeyedSingleStoreDataSource` method to register a `SingleStoreDataSource` as a [keyed service](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/runtime#keyed-di-services).
This is useful if you have multiple connection strings or need to connect to multiple databases.
If the service key is a string, it will automatically be used as the `SingleStoreDataSource` name;
to customize this, call the `AddKeyedSingleStoreDataSource(object?, string, Action<SingleStoreDataSourceBuilder>)` overload and call `SingleStoreDataSourceBuilder.UseName`.

```csharp
builder.Services.AddKeyedSingleStoreDataSource("users", builder.Configuration.GetConnectionString("Users"));
builder.Services.AddKeyedSingleStoreDataSource("products", builder.Configuration.GetConnectionString("Products"));

app.MapGet("/users/{userId}", async (int userId, [FromKeyedServices("users")] SingleStoreConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM users WHERE user_id = @userId LIMIT 1";
    command.Parameters.AddWithValue("@userId", userId);
    return $"Hello, {await command.ExecuteScalarAsync()}";
});

app.MapGet("/products/{productId}", async (int productId, [FromKeyedServices("products")] SingleStoreConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM products WHERE product_id = @productId LIMIT 1";
    command.Parameters.AddWithValue("@productId", productId);
    return await command.ExecuteScalarAsync();
});
```
