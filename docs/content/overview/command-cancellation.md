---
date: 2021-02-14
menu:
  main:
    parent: getting started
title: Cancellation
customtitle: "SingleStoreCommand Cancellation"
weight: 45
---

# SingleStoreCommand Cancellation

There are a number of APIs to cancel an executing `SingleStoreCommand`. The cancellation itself can take place in two ways:

* **Soft**. A "soft", or "logical", cancellation instructs the SingleStore Server to stop executing the command. This is accomplished by sending a `KILL QUERY` command to the server. Because the SingleStore protocol doesn't permit multiplexing of commands, the `KILL QUERY` command has to be sent on a different network connection to the same SingleStore server. Using a proxy or a Layer 4 load balancer may interfere with the ability to send the `KILL QUERY` command to the same server and prevent cancellation from occurring. If the cancellation is successful, the underlying `SingleStoreConnection` remains usable.
* **Hard**. A "hard", or "physical", cancellation is implemented by closing the connection to the SingleStore Server (usually by closing the TCP/IP socket). The intent is to return control to the SingleStore client if a network partition has occurred or the server isn't responding. Because the underlying network connection has been closed, the `SingleStoreConnection` is no longer usable after this cancellation occurs.

The mechanisms for cancelling a command are:

**SingleStoreCommand.Cancel**. This attempts a soft cancellation for the specified command. If successful, the `ExecuteX(Async)` method will throw a `SingleStoreException` with `SingleStoreErrorCode.QueryInterrupted`.

**ExecuteXAsync(CancellationToken)**. If the `CancellationToken` passed to any `ExecuteXAsync` method is cancelled, a soft cancellation will be attempted for that command. If successful, the `ExecuteXAsync` method will throw an `OperationCanceledException`; its inner exception will be a `SingleStoreException` with `SingleStoreErrorCode.QueryInterrupted`.

**CommandTimeout**. Each `SingleStoreCommand` has a `CommandTimeout` property that specifies a timeout in seconds. (This is initialized from the `DefaultCommandTimeout` setting in the connection string.) If this timeout is not `0`, then a soft cancellation will be attempted when the timeout expires. If successful, the `ExecuteX(Async)` method will throw a `SingleStoreException` with `SingleStoreErrorCode.CommandTimeoutExpired`; its inner exception will be a `SingleStoreException` with `SingleStoreErrorCode.QueryInterrupted`.

**CancellationTimeout**. The connection string can specify a `CancellationTimeout` timeout value. After `CommandTimeout` elapses (and a soft cancellation is started), the `CancellationTimeout` timer starts. If this times out, a hard cancellation is performed and the underlying network connection is closed. When this occurs, the `ExecuteX(Async)` method will throw a `SingleStoreException` with `SingleStoreErrorCode.CommandTimeoutExpired`.

### Notes

To distinguish a soft cancellation from `CommandTimeout` vs a hard cancellation from `CancellationTimeout`, check the inner exception of the `CommandTimeoutExpired` `SingleStoreException`: a soft cancellation will have a `QueryInterrupted` `SingleStoreException` as its `InnerException`.

To always perform a hard cancellation immediately when `CommandTimeout` expires, set `CancellationTimeout=-1` in the connection string. This was the default behavior of SingleStoreConnector prior to version 1.1.0.
