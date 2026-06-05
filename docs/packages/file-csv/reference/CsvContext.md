---
title: CsvContext Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvContext Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The state of a CSV reader or writer: its registered mappers and its options.
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
| `Mappers` | `IReadOnlyDictionary<Type, ICsvFileMapper> Mappers { get; }` | The mappers in use, keyed by record-line type. |
| `Options` | `ICsvFileOptions Options { get; }` | The options configuring the reader or writer. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [CsvFileReader](CsvFileReader.md)
- [ICsvFileMapper](ICsvFileMapper.md)
- [ICsvFileOptions](ICsvFileOptions.md)
- [File.CSV API reference](index.md)
</content>
