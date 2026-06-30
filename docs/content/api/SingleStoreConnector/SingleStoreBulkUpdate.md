# SingleStoreBulkUpdate class

[`SingleStoreBulkUpdate`](./SingleStoreBulkUpdate.md) lets you efficiently update many existing rows in a SingleStore table from an in-memory source. It complements [`SingleStoreBulkCopy`](./SingleStoreBulkCopy.md): where bulk copy inserts rows, bulk update modifies rows that already exist, matching them on the columns in [`KeyColumns`](./SingleStoreBulkUpdate/KeyColumns.md).

The source rows are first staged into a temporary table using [`SingleStoreBulkCopy`](./SingleStoreBulkCopy.md), then a single `UPDATE ... JOIN` copies the non-key column values into the matching rows of the destination table.

Because staging uses [`SingleStoreBulkCopy`](./SingleStoreBulkCopy.md), which loads data via `LOAD DATA LOCAL INFILE`, the connection string must have `AllowLoadLocalInfile=true` in order to use this class.

Example code:

```csharp
// open a connection that is allowed to load local data
await using var connection = new SingleStoreConnection("...;AllowLoadLocalInfile=True");
await connection.OpenAsync();

// the source data; the column ordinals are referenced by the column mappings below
var dataTable = new DataTable
{
    Columns = { new DataColumn("id", typeof(int)), new DataColumn("status", typeof(string)) },
    Rows = { { 1, "active" }, { 2, "disabled" } },
};

// update the "status" column of the rows whose "id" matches
var bulkUpdate = new SingleStoreBulkUpdate(connection)
{
    DestinationTableName = "users",
    KeyColumns = { "id" },
    ColumnMappings =
    {
        new SingleStoreBulkCopyColumnMapping(0, "id"),     // source column 0 -> key column "id"
        new SingleStoreBulkCopyColumnMapping(1, "status"), // source column 1 -> updated column "status"
    },
};
var result = await bulkUpdate.WriteToServerAsync(dataTable);

// check for problems
if (result.Warnings.Count != 0) { /* handle potential data loss warnings */ }
```

```csharp
public sealed class SingleStoreBulkUpdate
```

## Public Members

| name | description |
| --- | --- |
| [SingleStoreBulkUpdate](SingleStoreBulkUpdate/SingleStoreBulkUpdate.md)(…) | Initializes a [`SingleStoreBulkUpdate`](./SingleStoreBulkUpdate.md) object with the specified connection, and optionally the active transaction. |
| [BulkUpdateTimeout](SingleStoreBulkUpdate/BulkUpdateTimeout.md) { get; set; } | The number of seconds for each phase of the operation to complete before it times out (default `30`). |
| [ColumnMappings](SingleStoreBulkUpdate/ColumnMappings.md) { get; } | A collection of [`SingleStoreBulkCopyColumnMapping`](./SingleStoreBulkCopyColumnMapping.md) objects that map source column ordinals onto destination column names. Every key column and at least one non-key (updated) column must be mapped. |
| [ComputeRowsMatched](SingleStoreBulkUpdate/ComputeRowsMatched.md) { get; set; } | Whether to compute [`RowsMatched`](./SingleStoreBulkUpdateResult/RowsMatched.md) via a `COUNT` query (default `true`). Set this to `false` to skip that query for better performance, in which case [`RowsMatched`](./SingleStoreBulkUpdateResult/RowsMatched.md) is `null`. |
| [DestinationTableName](SingleStoreBulkUpdate/DestinationTableName.md) { get; set; } | The name of the table whose rows are updated. |
| [KeyColumns](SingleStoreBulkUpdate/KeyColumns.md) { get; } | The columns that identify which rows to update. They form the `JOIN` condition between the destination table and the staging table, so every key column must also appear in [`ColumnMappings`](./SingleStoreBulkUpdate/ColumnMappings.md). |
| [NotifyAfter](SingleStoreBulkUpdate/NotifyAfter.md) { get; set; } | If non-zero, this specifies the number of rows to be staged before raising the [`SingleStoreRowsStaged`](./SingleStoreBulkUpdate/SingleStoreRowsStaged.md) event. This applies only to the staging phase, not to the `UPDATE` execution. |
| event [SingleStoreRowsStaged](SingleStoreBulkUpdate/SingleStoreRowsStaged.md) | This event is raised every time that the number of rows specified by the [`NotifyAfter`](./SingleStoreBulkUpdate/NotifyAfter.md) property have been processed. |
| [WriteToServer](SingleStoreBulkUpdate/WriteToServer.md)(…) | Updates rows in the destination table using the data in the supplied DataTable. (3 methods) |
| [WriteToServerAsync](SingleStoreBulkUpdate/WriteToServerAsync.md)(…) | Asynchronously updates rows in the destination table using the data in the supplied DataTable. (3 methods) |

## Remarks

The following restrictions apply, and `WriteToServer` throws if they are not met: [`KeyColumns`](./SingleStoreBulkUpdate/KeyColumns.md) is required and every key column must be mapped; at least one non-key column must be mapped; the source must not contain duplicate key values; shard key columns and generated (computed) columns cannot be updated; reference tables are not supported; and expression column mappings are not supported.

An instance of this class is not thread-safe; do not share an instance across concurrent operations.

This API is experimental and may change in the future.

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
