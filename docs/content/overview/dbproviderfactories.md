---
lastmod: 2020-06-14
date: 2019-03-06
menu:
  main:
    parent: getting started
title: DbProviderFactories
weight: 15
---

Using DbProviderFactories
==========

SingleStoreConnector can be registered with `DbProviderFactories` and obtained via `DbProviderFactories.GetFactory("SingleStoreConnector")`, or by
using the methods [described here](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/obtaining-a-dbproviderfactory).

## .NET Framework

For .NET Framework applications, add the following section to your `app.config` or `web.config`:

```xml
<system.data>
  <DbProviderFactories>
     <add name="SingleStoreConnector"
        invariant="SingleStoreConnector"
        description="Async SingleStore ADO.NET Connector"
        type="SingleStoreConnector.SingleStoreConnectorFactory, SingleStoreConnector, Culture=neutral, PublicKeyToken=d33d3e53aa5f8c92" />
  </DbProviderFactories>
</system.data>
```

## .NET Core

For .NET Core 2.1 or later, call `DbProviderFactories.RegisterFactory("SingleStoreConnector", SingleStoreConnectorFactory.Instance)` during application
startup. This will register SingleStoreConnector's `DbProviderFactory` implementation in the central `DbProviderFactories` registry.
