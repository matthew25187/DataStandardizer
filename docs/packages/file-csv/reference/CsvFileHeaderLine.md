---
title: CsvFileHeaderLine Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileHeaderLine Class

## Definition

Namespace: `DataStandardizer.File.CSV`

A CSV header line. Each field value is a field name; `FieldNames` exposes those
names in order.

**Syntax**

```csharp
public sealed class CsvFileHeaderLine : CsvFileLineBase
```

## Remarks

The reader returns a `CsvFileHeaderLine` for the first line when
`CsvFileOptions.HeaderHandling` is `Use`; pass one to the writer to emit a header.
Populate it with the public `Add` method inherited from
[CsvFileLineBase](CsvFileLineBase.md).

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FieldNames` | `IReadOnlyList<string> FieldNames { get; }` | The field names, in order. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileLineBase](CsvFileLineBase.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [CsvFileHeaderHandling](CsvFileHeaderHandling.md)
- [File.CSV API reference](index.md)
</content>
