---
date: 2026-06-26
menu:
  main:
    parent: tutorials
title: Bulk Update
customtitle: "Tutorial: Bulk Updating Rows in SingleStore from C#"
weight: 14
---

Bulk Update
===========

`SingleStoreBulkUpdate` efficiently updates many existing rows in a SingleStore table from an in-memory source.
It complements [`SingleStoreBulkCopy`](../../api/SingleStoreConnector/SingleStoreBulkCopy/): where bulk copy *inserts*
rows, bulk update *modifies* rows that already exist, matching them on one or more key columns.

It is much faster than issuing an individual `UPDATE` statement per row, because all of the work is performed in a
single round trip pattern instead of one command per row.

> **Note:** This API is experimental and may change in the future.

How it works
------------

`SingleStoreBulkUpdate` performs the update in three phases, all on the same connection:

1. It creates a temporary staging table whose columns mirror the mapped columns of the destination table (their exact
   types, lengths, and collations are copied from the destination so values round-trip without conversion).
2. It loads the source rows into that staging table using `SingleStoreBulkCopy`.
3. It runs a single `UPDATE ... JOIN` that copies the non-key column values from the staging table into the matching
   rows of the destination table, joining on the key columns. The staging table is then dropped.

Because staging uses `SingleStoreBulkCopy` (which loads data with `LOAD DATA LOCAL INFILE`), the connection string
*must* have `AllowLoadLocalInfile=true` in order to use this class.

Basic example
-------------

```csharp
// open a connection that is allowed to load local data
await using var connection = new SingleStoreConnection("...;AllowLoadLocalInfile=True");
await connection.OpenAsync();

// the source data; the DataTable column names need not match the destination,
// but the source ordinals must match the column mappings below
var dataTable = new DataTable
{
    Columns =
    {
        new DataColumn("id", typeof(int)),
        new DataColumn("status", typeof(string)),
    },
    Rows =
    {
        { 1, "active" },
        { 2, "disabled" },
    },
};

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

Column mappings
---------------

Each `SingleStoreBulkCopyColumnMapping` maps a **source ordinal** (the zero-based column index in the source data) to a
**destination column name**. The names of the columns in the source `DataTable`/`DataReader` are ignored; only the
ordinal matters. Every key column must be included in the mappings, and at least one non-key column must be mapped so
that there is something to update.

Source data may be supplied as a `DataTable`, a sequence of `DataRow` objects, or an `IDataReader`. Both synchronous
(`WriteToServer`) and asynchronous (`WriteToServerAsync`) methods are available.

Interpreting the result
------------------------

`WriteToServerAsync` returns a `SingleStoreBulkUpdateResult`:

* `RowsStaged` — the number of source rows loaded into the staging table.
* `RowsMatched` — the number of staged rows that matched a row in the destination table. This is `-1` when
  `ComputeRowsMatched` is set to `false` (see below).
* `RowsUpdated` — the number of rows affected by the `UPDATE`.
* `Warnings` — any warnings raised while staging or updating; check that this is empty to avoid silent data loss from
  failed type conversions.

Performance
-----------

* Set `ComputeRowsMatched = false` to skip the extra `COUNT(*)` query that populates `RowsMatched`. When disabled,
  `RowsMatched` is reported as `-1`.
* Set `BulkCopyTimeout` (in seconds) to control how long each phase may run.
* Set `NotifyAfter` to a non-zero value to receive `SingleStoreRowsStaged` events while rows are being staged; the
  event handler can set `Abort = true` to stop staging early.

Limitations
-----------

`SingleStoreBulkUpdate` enforces the following restrictions, and will throw if they are not met:

* `KeyColumns` is required and must contain at least one column. Every key column must also appear in `ColumnMappings`.
* At least one non-key column must be mapped, so there is a column to update.
* Duplicate key values in the source data are rejected; they would collide in the staging table's primary key.
* Shard key columns cannot be updated, because SingleStore does not allow updating a shard key.
* Reference tables are not supported as the destination.
* Expression column mappings (a `SingleStoreBulkCopyColumnMapping` with an `Expression`) are not supported.

Transactions
------------

A `SingleStoreTransaction` may be passed to the constructor. When supplied, all phases participate in that transaction,
so the update can be committed or rolled back atomically with other work on the connection.
