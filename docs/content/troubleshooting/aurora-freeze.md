---
date: 2021-11-25
title: Aurora Freeze
customtitle: "SingleStoreConnection.Open Freezes with Amazon Aurora"
weight: 5
menu:
  main:
    parent: troubleshooting
---

# SingleStoreConnection.Open Freezes with Amazon Aurora

When using Amazon Aurora RDS, you may experience a hang when calling `SingleStoreConnection.Open()`. The immediate prior log statement will be:

```
[TRACE]  ServerSession  Session0.0 ServerVersion=5.7.12 supports reset connection and pipelining; sending pipelined reset connection request
```

The cause of this problem is Amazon Aurora not correctly supporting pipelining in the SingleStore protocol. This is known to be a problem with 2.x versions of Aurora (that implement SingleStore 5.7), but not with 3.x versions (that implement SingleStore 8.0).

To work around it, add `Pipelining = False;` to your connection string to disable the pipelining feature.
