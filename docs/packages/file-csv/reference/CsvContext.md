---
title: CsvContext Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvContext Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The state of a CSV reader: its registered mapper and its options.
Available from [CsvFileReader&lt;TRecordLine&gt;.Context](CsvFileReader.md).

**Syntax**

```csharp
public sealed class CsvContext
```

## Remarks

You do not construct this type directly — its constructor is `internal`. Obtain it
from the reader's `Context` property.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Mappers` | `IReadOnlyDictionary<Type, ICsvFileMapper> Mappers { get; }` | The mapper registered on the reader with `RegisterMapper`, keyed by the reader's record-line type, or no entries if none is registered. A live view: it reflects later calls to `RegisterMapper` and `UnregisterMapper`. Mappers built from attributes aren't included. |
| `Options` | `ICsvFileOptions Options { get; }` | The options configuring the reader or writer. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [CsvFileReader](CsvFileReader.md)
- [ICsvFileMapper](ICsvFileMapper.md)
- [ICsvFileOptions](ICsvFileOptions.md)
- [File.CSV API reference](index.md)
