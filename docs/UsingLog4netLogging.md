## How to Use

To integrate SingleStoreConnector with log4net, add the following line of code to your application startup routine (before any `SingleStoreConnector` objects have been used):

```csharp
SingleStoreConnectorLogManager.Provider = new Log4netLoggerProvider();
```

To reduce the verbosity of SingleStoreConnector logging, add the following element to your log4net config:

```xml
<log4net>
  ...
  <logger name="SingleStoreConnector">
    <level value="WARN" />
  </logger>
```
