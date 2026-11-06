---
title: CsvFileLineBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileLineBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The abstract base of the CSV line types. It holds the fields of a line and
implements [ICsvFileLine](ICsvFileLine.md).

**Syntax**

```csharp
public abstract class CsvFileLineBase : ICsvFileLine
```

## Remarks

`CsvFileLineBase` exposes a single public `Add` method for populating a line. The
rest of the `ICsvFileLine` surface is implemented **explicitly**, so those members
don't clash with the properties of a record model that derives from
[CsvFileRecordLine](CsvFileRecordLine.md), such as one with a `Count` or `Values`
column; reach them through an `ICsvFileLine` reference. The field-access helpers
(`GetFieldValue`, `SetFieldValue`, `GetFieldCount`, `GetPropertyKey`) are `protected`
and used by the derived line types. Concrete line types are
[CsvFileRecordLine](CsvFileRecordLine.md) and [CsvFileHeaderLine](CsvFileHeaderLine.md).

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Add(string fieldName, object? value)` | `void` | Adds a field to the end of the line. Throws `ArgumentNullException` for a `null` name, and `ArgumentException` if the line already has a field with that name. |

### Explicit implementation

Each member is callable only through an `ICsvFileLine` reference. See
[ICsvFileLine](ICsvFileLine.md) for what each one does. *(All targets.)*

| Member | Returns |
| --- | --- |
| `ICsvFileLine.this[int index]` | `object?` |
| `ICsvFileLine.this[string fieldName]` | `object?` |
| `ICsvFileLine.Keys` | `IReadOnlyList<string>` |
| `ICsvFileLine.Values` | `IReadOnlyList<object?>` |
| `ICsvFileLine.GetAt(int index)` | `KeyValuePair<string, object?>` |
| `ICsvFileLine.Contains(string fieldName)` | `bool` |
| `ICsvFileLine.IndexOf(string fieldName)` | `int` |
| `ICsvFileLine.TryGetValue(string fieldName, out object? value)` | `bool` |
| `ICsvFileLine.Insert(int index, string fieldName, object? value)` | `void` |
| `ICsvFileLine.Remove(string fieldName)` | `bool` |
| `ICsvFileLine.RemoveAt(int index)` | `void` |
| `ICsvFileLine.Clear()` | `void` |
| `IReadOnlyCollection<KeyValuePair<string, object?>>.Count` | `int` |
| `IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()` | `IEnumerator<KeyValuePair<string, object?>>` |
| `IEnumerable.GetEnumerator()` | `IEnumerator` |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable annotations on the `object?` members apply to the `netstandard2.1` and `net10.0` builds.

The `net10.0` build stores the fields in a generic
`OrderedDictionary<string, object?>`. The other builds store them in an ordered
store of the package's own, which keeps the fields in an array in position order
with a lookup from name to position beside it. The two behave the same: the same
positions, the same order, the same exceptions, and the same rules for when an
enumerator is invalidated.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [ICsvFileLine](ICsvFileLine.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
