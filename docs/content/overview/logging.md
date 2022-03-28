---
lastmod: 2018-12-14
date: 2018-01-20
menu:
  main:
    parent: getting started
title: Logging
weight: 50
---

Logging
=======

SingleStoreConnector implements a custom logging framework with the `ISingleStoreConnectorLogger` and `ISingleStoreConnectorLoggerProvider` interfaces.
To connect SingleStoreConnector to an existing logging framework, write an implementation of `ISingleStoreConnectorLoggerProvider` that adapts
the existing logging framework, and install it by setting `SingleStoreConnector.Logging.SingleStoreConnectorLogManager.Provider = provider;`.

The `SingleStoreConnectorLogManager.Provider` property may only be set once, and must be set before any other SingleStoreConnector library methods are called.

Debug-level logging is useful for diagnosing problems with SingleStoreConnector itself; it is recommend that applications limit SingleStoreConnector
logging to Info or higher.

### Existing Logging Providers

There are NuGet packages that adapt SingleStoreConnector logging for popular logging frameworks.

#### log4net

Install [SingleStoreConnector.Logging.log4net](https://www.nuget.org/packages/MySqlConnector.Logging.log4net/).

Add the following line of code to your application startup routine:

```csharp
SingleStoreConnectorLogManager.Provider = new Log4netLoggerProvider();
```

To reduce the verbosity of SingleStoreConnector logging, add the following element to your log4net config:

```xml
<log4net>
  ...
  <logger name="SingleStoreConnector">
    <level value="WARN" /> <!-- or "INFO" -->
  </logger>
```

#### Microsoft.Extensions.Logging

Install [SingleStoreConnector.Logging.Microsoft.Extensions.Logging](https://www.nuget.org/packages/MySqlConnector.Logging.Microsoft.Extensions.Logging/).

Add the following line of code to your `Startup.Configure` method:

```csharp
SingleStoreConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```

#### NLog

Install [SingleStoreConnector.Logging.NLog](https://www.nuget.org/packages/MySqlConnector.Logging.NLog/).

Add the following line of code to your application startup routine:

```csharp
SingleStoreConnectorLogManager.Provider = new NLogLoggerProvider();
```

#### Serilog

Install [SingleStoreConnector.Logging.Serilog](https://www.nuget.org/packages/MySqlConnector.Logging.Serilog/).

Add the following line of code to your application startup routine:

```csharp
SingleStoreConnectorLogManager.Provider = new SerilogLoggerProvider(loggerFactory);
```
