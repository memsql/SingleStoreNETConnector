# SingleStoreBulkCopyColumnMapping class

Use [`SingleStoreBulkCopyColumnMapping`](./SingleStoreBulkCopyColumnMapping.md) to specify how to map columns in the source data to columns in the destination table when using [`SingleStoreBulkCopy`](./SingleStoreBulkCopy.md).

Set [`SourceOrdinal`](./SingleStoreBulkCopyColumnMapping/SourceOrdinal.md) to the zero-based index of the source column to map. Set [`DestinationColumn`](./SingleStoreBulkCopyColumnMapping/DestinationColumn.md) to either the name of a column in the destination table, or the name of a user-defined variable. If a user-defined variable, you can use [`Expression`](./SingleStoreBulkCopyColumnMapping/Expression.md) to specify a SingleStore expression that assigns its value to destination column.

Source columns that don't have an entry in [`ColumnMappings`](./SingleStoreBulkCopy/ColumnMappings.md) will be ignored (unless the [`ColumnMappings`](./SingleStoreBulkCopy/ColumnMappings.md) collection is empty, in which case all columns will be mapped one-to-one).

SingleStoreConnector will transmit all binary data as hex, so any expression that operates on it must decode it with the `UNHEX` function first. (This will be performed automatically if no [`Expression`](./SingleStoreBulkCopyColumnMapping/Expression.md) is specified, but will be necessary to specify manually for more complex expressions.)

Example code:

```csharp
new SingleStoreBulkCopyColumnMapping
{
    SourceOrdinal = 2,
    DestinationColumn = "user_name",
},
new SingleStoreBulkCopyColumnMapping
{
    SourceOrdinal = 0,
    DestinationColumn = "@tmp",
    Expression = "column_value = @tmp * 2",
},
```

```csharp
public sealed class SingleStoreBulkCopyColumnMapping
```

| parameter | description |
| --- | --- |
| sourceOrdinal | The zero-based ordinal position of the source column. |
| destinationColumn | The name of the destination column. |
| expression | The optional expression to be used to set the destination column. |

## Public Members

| name | description |
| --- | --- |
| [SingleStoreBulkCopyColumnMapping](SingleStoreBulkCopyColumnMapping/SingleStoreBulkCopyColumnMapping.md)() | Initializes [`SingleStoreBulkCopyColumnMapping`](./SingleStoreBulkCopyColumnMapping.md) with the default values. |
| [SingleStoreBulkCopyColumnMapping](SingleStoreBulkCopyColumnMapping/SingleStoreBulkCopyColumnMapping.md)(…) | Use [`SingleStoreBulkCopyColumnMapping`](./SingleStoreBulkCopyColumnMapping.md) to specify how to map columns in the source data to columns in the destination table when using [`SingleStoreBulkCopy`](./SingleStoreBulkCopy.md). |
| [DestinationColumn](SingleStoreBulkCopyColumnMapping/DestinationColumn.md) { get; set; } | The name of the destination column to copy to. To use an expression, this should be the name of a unique user-defined variable. |
| [Expression](SingleStoreBulkCopyColumnMapping/Expression.md) { get; set; } | An optional expression for setting a destination column. To use an expression, the [`DestinationColumn`](./SingleStoreBulkCopyColumnMapping/DestinationColumn.md) should be set to the name of a user-defined variable and this expression should set a column using that variable. |
| [SourceOrdinal](SingleStoreBulkCopyColumnMapping/SourceOrdinal.md) { get; set; } | The zero-based ordinal position of the source column to map from. |

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
