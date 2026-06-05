---
title: Prerequisites
parent: File.CSV
grand_parent: Packages
nav_order: 1
---

# Prerequisites

Before reading or writing CSV data you need a source or target for the file and
a reader or writer to access it — and all of these are disposable resources that
should be cleaned up.

The source or target of a CSV file can be represented by either a `Stream` or a
`TextReader`. As these resources implement the `IDisposable` interface, use of
them should guarantee that they are properly disposed of when no longer needed.

The same concerns also apply to the reader or writer used to access the CSV
data. Like the `Stream` or `TextReader`, the reader and writer also implement
the `IDisposable` interface and should be guaranteed cleanup when no longer
required.

To create the resources needed to interact with a CSV file, you would typically
instantiate them in a `using` statement. This establishes the scope, or
lifetime, of the resource after which they will be automatically disposed. For
example:

```csharp
using (var csvStream = new StreamReader(@"C:\temp\some_file.csv"))
{
    ...
}
```

You may in some circumstances want to create these resources outside of a
`using` statement so the use and disposal of the resources happens separately.
It would then be incumbent on your code to ensure that the resources are
disposed when no longer needed by calling the `Dispose()` method directly.
