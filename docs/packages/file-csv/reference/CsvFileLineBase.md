---
title: CsvFileLineBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileLineBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The abstract base of the CSV line types. It backs a line with an ordered,
name-keyed field bag and implements the `IOrderedDictionary` surface required by
[ICsvFileLine](ICsvFileLine.md).

**Syntax**

```csharp
public abstract class CsvFileLineBase : ICsvFileLine
```

## Remarks

`CsvFileLineBase` exposes a single public `Add` method for populating a line; the
rest of the `IOrderedDictionary` / `IDictionary` / `ICollection` / `IEnumerable`
surface is implemented **explicitly**, so those members are reached only through an
interface reference (typically `ICsvFileLine`). The field-access helpers
(`GetFieldValue`, `SetFieldValue`, `GetFieldCount`, `GetPropertyKey`) are `protected`
and used by the derived line types. Concrete line types are
[CsvFileRecordLine](CsvFileRecordLine.md) and [CsvFileHeaderLine](CsvFileHeaderLine.md).

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Add(object key, object? value)` | `void` | Appends a field. `value` is `object` (not nullable) on `netstandard1.3` / `netstandard2.0`. |

### Explicit implementation

`CsvFileLineBase` implements the ordered-dictionary surface explicitly; each member
is callable only through the corresponding interface reference. *(All targets.)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ICollection.CopyTo(Array array, int index)` | `void` | |
| `ICollection.Count` | `int` | |
| `ICollection.IsSynchronized` | `bool` | |
| `ICollection.SyncRoot` | `object` | |
| `IDictionary.Clear()` | `void` | |
| `IDictionary.Contains(object key)` | `bool` | |
| `IDictionary.GetEnumerator()` | `IDictionaryEnumerator` | |
| `IDictionary.IsFixedSize` | `bool` | |
| `IDictionary.IsReadOnly` | `bool` | |
| `IDictionary.Keys` | `ICollection` | |
| `IDictionary.Remove(object key)` | `void` | |
| `IDictionary.this[object key]` | `object?` | |
| `IDictionary.Values` | `ICollection` | |
| `IEnumerable.GetEnumerator()` | `IEnumerator` | |
| `IOrderedDictionary.GetEnumerator()` | `IDictionaryEnumerator` | |
| `IOrderedDictionary.Insert(int index, object key, object? value)` | `void` | |
| `IOrderedDictionary.RemoveAt(int index)` | `void` | |
| `IOrderedDictionary.this[int index]` | `object?` | |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `net8.0`, and `net10.0`. The nullable
annotations on the `object?` members apply to the `net8.0` and `net10.0` builds.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [ICsvFileLine](ICsvFileLine.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
</content>
