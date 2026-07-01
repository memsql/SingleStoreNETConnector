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
* `RowsMatched` — the number of destination rows matched by the join on the key columns. When the destination key
  columns are unique this equals the number of staged rows that matched; if they are not unique, a single staged row
  can match several destination rows, so this can exceed `RowsStaged`. This is `null` when `ComputeRowsMatched` is set
  to `false` (see below).
* `RowsAffected` — the number of rows affected by the `UPDATE`, as reported by the server. Its exact meaning depends on
  the connection's `UseAffectedRows` setting: with the default (`UseAffectedRows=false`) it counts the rows *matched* by
  the update — including rows that already held the new values — so it typically equals `RowsMatched`; with
  `UseAffectedRows=true` it counts only the rows whose values actually *changed*.
* `Warnings` — any warnings raised while staging or updating; check that this is empty to avoid silent data loss from
  failed type conversions.

Performance
-----------

* Set `ComputeRowsMatched = false` to skip the extra `COUNT(*)` query that populates `RowsMatched`. When disabled,
  `RowsMatched` is `null`.
* Set `BulkUpdateTimeout` (in seconds) to bound how long each phase may run. It defaults to `0` (no timeout), so a
  finite value should be set deliberately if a phase must not run unbounded.
* Set `NotifyAfter` to a non-zero value to receive `SingleStoreRowsStaged` events while rows are being staged; the
  event handler can set `Abort = true` to cancel the operation. Aborting stops staging and skips the `UPDATE`
  entirely, so no rows in the destination table are modified.

Limitations
-----------

`SingleStoreBulkUpdate` enforces the following restrictions, and will throw if they are not met:

* `KeyColumns` is required and must contain at least one column. Every key column must also appear in `ColumnMappings`.
* At least one non-key column must be mapped, so there is a column to update.
* Duplicate key values in the source data are rejected; they would collide in the staging table's primary key.
* Shard key columns cannot be updated, because SingleStore does not allow updating a shard key.
* Generated (computed) columns cannot be mapped (neither as key nor update columns), because the staging table cannot
  reproduce a generated column's definition.
* Reference tables are not supported as the destination.
* Expression column mappings (a `SingleStoreBulkCopyColumnMapping` with an `Expression`) are not supported.

Other things to be aware of:

* **Key columns need not be unique in the destination.** The key columns identify rows to update via a join; they are
  not required to be a unique or primary key on the destination table. If they are not unique, a single source row can
  update multiple destination rows, and `RowsMatched`/`RowsAffected` can exceed the number of source rows. Duplicate
  keys are only rejected in the *source* data (they would collide in the staging table's primary key).
* **Key column types.** The key columns become the primary key of the staging table, so they must be types that
  SingleStore allows in a primary key. Large `TEXT`/`BLOB`/`JSON`/spatial columns are not usable as key columns.
* **Required privileges.** In addition to `UPDATE` on the destination table, the connection needs permission to run
  `SHOW CREATE TABLE`, `SHOW INDEXES`, and a schema-only `SELECT` against it, because the operation inspects the
  table's schema before updating.
* **`IDataReader` source.** When the source is an `IDataReader`, it must be opened on a *different* connection than
  the one used for the bulk update. The update connection runs schema queries, creates the staging table, and loads
  data, so it cannot have an open reader on it at the same time.
* **Thread safety.** A `SingleStoreBulkUpdate` instance is not thread-safe. Do not share an instance across concurrent
  operations.

Transactions
------------

A `SingleStoreTransaction` may be passed to the constructor. When supplied, all phases participate in that transaction,
so the update can be committed or rolled back atomically with other work on the connection.

Future directions
-----------------

This API is experimental. Several of the limitations above are deliberate scope decisions for the first version rather
than restrictions imposed by SingleStore, and may be relaxed in a future version:

* **`KeyColumns` is required.** A future version could auto-detect the destination table's primary key when
  `KeyColumns` is not set.
* **Expression column mappings are rejected.** Because the source is staged into a real temporary table, a
  `SingleStoreBulkCopyColumnMapping.Expression` (which relies on user variables during `LOAD DATA`) is not supported;
  a future version could apply expressions when populating the staging table.
* **Generated (computed) columns cannot be mapped.** This could be supported for key columns if the staging table
  learned to reproduce a generated column's underlying type.
* **Row counts are `int`.** `RowsStaged`, `RowsMatched`, and `RowsAffected` are `int` for consistency with
  `SingleStoreBulkCopyResult`; they could widen to `long` if very large updates need it.
* **Update only.** There is no upsert (insert-or-update) mode; a future version could add one.

By contrast, the following are SingleStore behaviors rather than choices made here, and are not expected to change:
updating shard key columns is not allowed, and reference tables are not supported as the destination.
