---
title: ICsvFileLine Interface
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# ICsvFileLine Interface

## Definition

Namespace: `DataStandardizer.File.CSV`

A line of a CSV file, as an ordered collection of fields, each with a name and a
value. Read or set a field by name or by position.

**Syntax**

```csharp
public interface ICsvFileLine : IReadOnlyCollection<KeyValuePair<string, object?>>
```

## Remarks

The fields keep the order they were added in. The field at position *N* is always
the *N*th field of the line, and `Insert`, `RemoveAt` and `Remove` move the fields
after it up or down by one. Enumerating a line gives its fields in that order, as
`KeyValuePair<string, object?>` items.

Field names are compared by ordinal, so they are case-sensitive. A line can't hold
two fields with the same name. Values are `object?`, because the fields of one line
can hold values of different types.

Adding, inserting or removing a field, or clearing a line that has fields,
invalidates any enumerator of the line, its `Keys` or its `Values`, so its next
`MoveNext` throws `InvalidOperationException`. Replacing the value of a field that
is present doesn't, and neither does removing a field that isn't present.

The reader and writer accept and return values typed as `ICsvFileLine`; the
concrete implementations are [CsvFileHeaderLine](CsvFileHeaderLine.md) and
[CsvFileRecordLine](CsvFileRecordLine.md) (via [CsvFileLineBase](CsvFileLineBase.md)).

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | The number of fields. Inherited from `IReadOnlyCollection<T>`. |
| Indexer (position) | `object? this[int index] { get; set; }` | The value of the field at a position. Throws `ArgumentOutOfRangeException` for a position outside the line. |
| Indexer (name) | `object? this[string fieldName] { get; set; }` | The value of a field, or `null` if the line has no field with that name. Setting a field that isn't present adds it to the end of the line. |
| `Keys` | `IReadOnlyList<string> Keys { get; }` | The field names, in position order. A live view of the line. |
| `Values` | `IReadOnlyList<object?> Values { get; }` | The field values, in position order. A live view of the line. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Add(string fieldName, object? value)` | `void` | Adds a field to the end of the line. Throws `ArgumentException` if the line already has a field with that name. |
| `Insert(int index, string fieldName, object? value)` | `void` | Inserts a field at a position, moving the fields from that position on up by one. `index` can be `Count`, which adds the field to the end. Throws `ArgumentOutOfRangeException` for a position outside `0` to `Count`, and `ArgumentException` if the line already has a field with that name. |
| `GetAt(int index)` | `KeyValuePair<string, object?>` | The name and value of the field at a position. |
| `Contains(string fieldName)` | `bool` | Whether the line has a field with that name. |
| `IndexOf(string fieldName)` | `int` | The position of a field, or `-1` if the line has no field with that name. |
| `TryGetValue(string fieldName, out object? value)` | `bool` | Gets the value of a field, if the line has it. |
| `Remove(string fieldName)` | `bool` | Removes a field, moving the fields after it down by one. Returns `false` if the line has no field with that name. |
| `RemoveAt(int index)` | `void` | Removes the field at a position, moving the fields after it down by one. |
| `Clear()` | `void` | Removes every field. |

Every member that takes a `fieldName` throws `ArgumentNullException` for a `null`
name.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable annotations on the `object?` members apply to the `netstandard2.1` and
`net10.0` builds.

Version 2.0 replaces the `IOrderedDictionary` base of version 1.x with this
interface. See [the File.CSV line model](../../../resources/removed-in-v2.md#filecsv-line-model)
in the v2.0 migration notes.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [CsvFileLineBase](CsvFileLineBase.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
