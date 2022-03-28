## How to Use

To integrate SingleStoreConnector with [NLog](http://nlog-project.org/), add the following line of code to your application startup routine (before any `SingleStoreConnector` objects have been used):

```csharp
SingleStoreConnectorLogManager.Provider = new NLogLoggerProvider();
```
