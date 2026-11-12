---
title: CsvFieldContext Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFieldContext Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The context for a CSV field being processed, passed to the field handler and
converter delegates. It exposes the field's identity, the surrounding line, the
model, the raw value, and the active options.

**.NET:**

```csharp
public sealed record CsvFieldContext<TModel> where TModel : class
```

**.NET Standard:**

```csharp
public sealed class CsvFieldContext<TModel> where TModel : class
```

## Remarks

On `net10.0` the type is a `record`; on .NET Standard it is a plain `class`.
You do not construct it — its constructor and property setters are `internal`. The
library supplies a populated instance to your [CsvFieldBadValue](CsvFieldBadValue.md),
[CsvFieldCount](CsvFieldCount.md), [CsvFieldValidate](CsvFieldValidate.md),
[CsvFieldConvertFromString](CsvFieldConvertFromString.md), and
[CsvFieldConvertToString](CsvFieldConvertToString.md) delegates. All properties are
publicly read-only.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FieldIndex` | `int? FieldIndex { get; }` | The index of the field. When reading, its position in the record as read, and `null` when the file has no such field. |
| `FieldName` | `string? FieldName { get; }` | The name of the field. When reading, the name of the field in the line, which a mapping by index resolves through the header. |
| `HeaderLine` | `CsvFileHeaderLine? HeaderLine { get; }` | The header line, if any. |
| `Model` | `TModel? Model { get; }` | The line model being read or written. |
| `Options` | `ICsvFileOptions Options { get; }` | The options configuring the reader or writer. |
| `RawFieldValue` | `string? RawFieldValue { get; }` | The raw value of the field. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
`CsvFieldContext<TModel>` is a `record` on the `net10.0` build and a
plain `class` on the `netstandard1.1`, `netstandard2.0`, and `netstandard2.1`
builds. The nullable reference annotations apply to the
`netstandard2.1` and `net10.0` builds.

## See also

- [Field mapping](../concepts/field-mapping.md)
- [CsvFieldBadValue](CsvFieldBadValue.md)
- [CsvFieldValidate](CsvFieldValidate.md)
- [CsvFieldConvertFromString](CsvFieldConvertFromString.md)
- [CsvFieldConvertToString](CsvFieldConvertToString.md)
- [ICsvFileOptions](ICsvFileOptions.md)
- [File.CSV API reference](index.md)
