# SingleStoreDataReader class

```csharp
public sealed class SingleStoreDataReader : DbDataReader, IDbColumnSchemaGenerator
```

## Public Members

| name | description |
| --- | --- |
| override [Depth](SingleStoreDataReader/Depth.md) { get; } |  |
| override [FieldCount](SingleStoreDataReader/FieldCount.md) { get; } |  |
| override [HasRows](SingleStoreDataReader/HasRows.md) { get; } |  |
| override [IsClosed](SingleStoreDataReader/IsClosed.md) { get; } |  |
| override [Item](SingleStoreDataReader/Item.md) { get; } |  (2 indexers) |
| override [RecordsAffected](SingleStoreDataReader/RecordsAffected.md) { get; } | Gets the number of rows changed, inserted, or deleted by execution of the SQL statement. |
| override [VisibleFieldCount](SingleStoreDataReader/VisibleFieldCount.md) { get; } |  |
| override [Close](SingleStoreDataReader/Close.md)() |  |
| override [DisposeAsync](SingleStoreDataReader/DisposeAsync.md)() |  |
| override [GetBoolean](SingleStoreDataReader/GetBoolean.md)(…) |  |
| [GetBoolean](SingleStoreDataReader/GetBoolean.md)(…) |  |
| override [GetByte](SingleStoreDataReader/GetByte.md)(…) |  |
| [GetByte](SingleStoreDataReader/GetByte.md)(…) |  |
| override [GetBytes](SingleStoreDataReader/GetBytes.md)(…) |  |
| [GetBytes](SingleStoreDataReader/GetBytes.md)(…) |  |
| override [GetChar](SingleStoreDataReader/GetChar.md)(…) |  |
| [GetChar](SingleStoreDataReader/GetChar.md)(…) |  |
| override [GetChars](SingleStoreDataReader/GetChars.md)(…) |  |
| [GetColumnSchema](SingleStoreDataReader/GetColumnSchema.md)() | Returns metadata about the columns in the result set. |
| override [GetColumnSchemaAsync](SingleStoreDataReader/GetColumnSchemaAsync.md)(…) | Returns metadata about the columns in the result set. |
| override [GetDataTypeName](SingleStoreDataReader/GetDataTypeName.md)(…) |  |
| [GetDateOnly](SingleStoreDataReader/GetDateOnly.md)(…) |  (2 methods) |
| override [GetDateTime](SingleStoreDataReader/GetDateTime.md)(…) |  |
| [GetDateTime](SingleStoreDataReader/GetDateTime.md)(…) |  |
| [GetDateTimeOffset](SingleStoreDataReader/GetDateTimeOffset.md)(…) |  (2 methods) |
| override [GetDecimal](SingleStoreDataReader/GetDecimal.md)(…) |  |
| [GetDecimal](SingleStoreDataReader/GetDecimal.md)(…) |  |
| override [GetDouble](SingleStoreDataReader/GetDouble.md)(…) |  |
| [GetDouble](SingleStoreDataReader/GetDouble.md)(…) |  |
| override [GetEnumerator](SingleStoreDataReader/GetEnumerator.md)() |  |
| override [GetFieldType](SingleStoreDataReader/GetFieldType.md)(…) |  |
| [GetFieldType](SingleStoreDataReader/GetFieldType.md)(…) |  |
| override [GetFieldValue&lt;T&gt;](SingleStoreDataReader/GetFieldValue.md)(…) |  |
| override [GetFloat](SingleStoreDataReader/GetFloat.md)(…) |  |
| [GetFloat](SingleStoreDataReader/GetFloat.md)(…) |  |
| override [GetGuid](SingleStoreDataReader/GetGuid.md)(…) |  |
| [GetGuid](SingleStoreDataReader/GetGuid.md)(…) |  |
| override [GetInt16](SingleStoreDataReader/GetInt16.md)(…) |  |
| [GetInt16](SingleStoreDataReader/GetInt16.md)(…) |  |
| override [GetInt32](SingleStoreDataReader/GetInt32.md)(…) |  |
| [GetInt32](SingleStoreDataReader/GetInt32.md)(…) |  |
| override [GetInt64](SingleStoreDataReader/GetInt64.md)(…) |  |
| [GetInt64](SingleStoreDataReader/GetInt64.md)(…) |  |
| override [GetName](SingleStoreDataReader/GetName.md)(…) |  |
| override [GetOrdinal](SingleStoreDataReader/GetOrdinal.md)(…) |  |
| [GetSByte](SingleStoreDataReader/GetSByte.md)(…) |  (2 methods) |
| override [GetSchemaTable](SingleStoreDataReader/GetSchemaTable.md)() | Returns a DataTable that contains metadata about the columns in the result set. |
| override [GetSchemaTableAsync](SingleStoreDataReader/GetSchemaTableAsync.md)(…) | Returns a DataTable that contains metadata about the columns in the result set. |
| [GetSingleStoreDateTime](SingleStoreDataReader/GetSingleStoreDateTime.md)(…) |  (2 methods) |
| [GetSingleStoreDecimal](SingleStoreDataReader/GetSingleStoreDecimal.md)(…) |  (2 methods) |
| [GetSingleStoreGeography](SingleStoreDataReader/GetSingleStoreGeography.md)(…) |  (2 methods) |
| [GetSingleStoreGeographyPoint](SingleStoreDataReader/GetSingleStoreGeographyPoint.md)(…) |  (2 methods) |
| override [GetStream](SingleStoreDataReader/GetStream.md)(…) |  |
| [GetStream](SingleStoreDataReader/GetStream.md)(…) |  |
| override [GetString](SingleStoreDataReader/GetString.md)(…) |  |
| [GetString](SingleStoreDataReader/GetString.md)(…) |  |
| override [GetTextReader](SingleStoreDataReader/GetTextReader.md)(…) |  |
| [GetTextReader](SingleStoreDataReader/GetTextReader.md)(…) |  |
| [GetTimeOnly](SingleStoreDataReader/GetTimeOnly.md)(…) |  (2 methods) |
| [GetTimeSpan](SingleStoreDataReader/GetTimeSpan.md)(…) |  (2 methods) |
| [GetUInt16](SingleStoreDataReader/GetUInt16.md)(…) |  (2 methods) |
| [GetUInt32](SingleStoreDataReader/GetUInt32.md)(…) |  (2 methods) |
| [GetUInt64](SingleStoreDataReader/GetUInt64.md)(…) |  (2 methods) |
| override [GetValue](SingleStoreDataReader/GetValue.md)(…) |  |
| override [GetValues](SingleStoreDataReader/GetValues.md)(…) |  |
| override [IsDBNull](SingleStoreDataReader/IsDBNull.md)(…) |  |
| override [NextResult](SingleStoreDataReader/NextResult.md)() |  |
| override [NextResultAsync](SingleStoreDataReader/NextResultAsync.md)(…) |  |
| override [Read](SingleStoreDataReader/Read.md)() |  |
| override [ReadAsync](SingleStoreDataReader/ReadAsync.md)(…) |  |

## Protected Members

| name | description |
| --- | --- |
| override [Dispose](SingleStoreDataReader/Dispose.md)(…) |  |
| override [GetDbDataReader](SingleStoreDataReader/GetDbDataReader.md)(…) |  |

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
