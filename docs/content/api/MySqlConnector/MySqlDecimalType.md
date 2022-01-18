---
title: MySqlDecimal
---

# MySqlDecimal structure

[`MySqlDecimal`](../MySqlDecimalType/) represents a MySQL `DECIMAL` value that is too large to fit in a .NET Decimal.

```csharp
public struct MySqlDecimal
```

## Public Members

| name | description |
| --- | --- |
| [Value](../MySqlDecimal/Value/) { get; } | Gets the value of this [`MySqlDecimal`](../MySqlDecimalType/) as a Decimal. |
| [ToDouble](../MySqlDecimal/ToDouble/)() | Gets the value of this [`MySqlDecimal`](../MySqlDecimalType/) as a Double. |
| override [ToString](../MySqlDecimal/ToString/)() | Gets the original value of this [`MySqlDecimal`](../MySqlDecimalType/) as a String. |

## See Also

* namespace [MySqlConnector](../../MySqlConnectorNamespace/)
* assembly [MySqlConnector](../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->