---
title: ImfHeaderFieldCollection Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderFieldCollection Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The fields of a header, in order. Lookups by name are case-insensitive.

**Syntax**

```csharp
public sealed class ImfHeaderFieldCollection : IReadOnlyList<ImfHeaderField>
```

## Remarks

A writer writes the fields that were read, and have not been replaced or removed, byte for byte, so changing one field rewrites that field only.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfHeaderFieldCollection()` | Creates an empty collection. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | The number of items. |
| `this[]` | `ImfHeaderField this[int index] { get; }` | The item at an index. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Add(ImfHeaderField field)` | `void` | Adds a field after the others. **Throws** `ArgumentNullException`: `field` is `null`. |
| `Contains(string name)` | `bool` | Determines if there is a field with a name. **Returns:** `true` if there is such a field; otherwise `false`. **Throws** `ArgumentNullException`: `name` is `null`. |
| `Find(string name)` | `ImfHeaderField?` | Finds the first field with a name. **Returns:** The field, or `null` if there is none. **Throws** `ArgumentNullException`: `name` is `null`. |
| `FindAll(string name)` | `IReadOnlyList<ImfHeaderField>` | Finds every field with a name. **Returns:** The fields, in order, in a read-only list that doesn't follow later changes. **Throws** `ArgumentNullException`: `name` is `null`. |
| `GetEnumerator()` | `IEnumerator<ImfHeaderField>` | Gets an enumerator over the items. |
| `Insert(int index, ImfHeaderField field)` | `void` | Adds a field at an index. RFC 5322 §3.6.6 and §3.6.7 have resent and trace fields put before the fields already there. **Throws** `ArgumentNullException`: `field` is `null`. **Throws** `ArgumentOutOfRangeException`: `index` is less than 0, or more than `Count`. |
| `RemoveAll(string name)` | `int` | Removes every field with a name. **Returns:** The number of fields removed. **Throws** `ArgumentNullException`: `name` is `null`. |
| `RemoveAt(int index)` | `void` | Removes the field at an index. **Throws** `ArgumentOutOfRangeException`: `index` is less than 0, or not less than `Count`. |
| `Replace(int index, ImfHeaderField field)` | `void` | Replaces the field at an index. **Throws** `ArgumentNullException`: `field` is `null`. **Throws** `ArgumentOutOfRangeException`: `index` is less than 0, or not less than `Count`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Edit one header without breaking a signature](../how-to/edit-a-header.md)
- [File.InternetMessage API reference](index.md)
