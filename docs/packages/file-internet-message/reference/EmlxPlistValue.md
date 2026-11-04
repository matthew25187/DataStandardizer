---
title: EmlxPlistValue Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxPlistValue Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A value in an XML property list. It can't be changed once made.

**Syntax**

```csharp
public sealed class EmlxPlistValue
```

## Remarks

A value that was read keeps its text, so that a `real` of `1.50`, or an `integer` too large for a `Int64`, is written back as it was.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Entries` | `IReadOnlyList<KeyValuePair<string, EmlxPlistValue>> Entries { get; }` | Gets the keys and values of a `dict`, in order. The list is read-only, and for any other kind, it is empty. |
| `Items` | `IReadOnlyList<EmlxPlistValue> Items { get; }` | Gets the values of an `array`. The list is read-only, and for any other kind, it is empty. |
| `Kind` | `EmlxPlistValueKind Kind { get; }` | Gets the kind of value. |
| `Text` | `string Text { get; }` | Gets the text of a `string`, `integer`, `real`, `date` or `data`, as it is held. For any other kind, it is empty. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `static FromArray(IEnumerable<EmlxPlistValue> items)` | `EmlxPlistValue` | Creates an `array`. **Returns:** The value. **Throws** `ArgumentNullException`: `items` is `null`. **Throws** `ArgumentException`: `items` holds a `null`. |
| `static FromBoolean(bool value)` | `EmlxPlistValue` | Creates a `true` or `false`. **Returns:** The value. |
| `static FromData(byte[] value)` | `EmlxPlistValue` | Creates a `data`. **Returns:** The value. **Throws** `ArgumentNullException`: `value` is `null`. |
| `static FromDate(DateTimeOffset value)` | `EmlxPlistValue` | Creates a `date`, which holds whole seconds in UTC. **Returns:** The value. |
| `static FromDictionary(IEnumerable<KeyValuePair<string, EmlxPlistValue>> entries)` | `EmlxPlistValue` | Creates a `dict`. **Returns:** The value. **Throws** `ArgumentNullException`: `entries` is `null`. **Throws** `ArgumentException`: `entries` holds a `null` key or value, or a key with a character that XML doesn't allow. |
| `static FromInteger(long value)` | `EmlxPlistValue` | Creates an `integer`. **Returns:** The value. |
| `static FromReal(double value)` | `EmlxPlistValue` | Creates a `real`. **Returns:** The value. |
| `static FromString(string value)` | `EmlxPlistValue` | Creates a `string`. **Returns:** The value. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `ArgumentException`: `value` holds a character that XML doesn't allow. |
| `TryGetData(out byte[] value)` | `bool` | Gets the bytes of a `data`. **Returns:** `true` if this is a `data`. **`value`:** Receives a copy of the bytes, or an empty array if this is not a `data`. |
| `TryGetDate(out DateTimeOffset value)` | `bool` | Gets the date and time of a `date`. **Returns:** `true` if this is a `date`. |
| `TryGetDouble(out double value)` | `bool` | Gets the number of a `real` or an `integer`. **Returns:** `true` if this is a number within the range of a `Double`, or a `real` that holds a word for a value with no digits. **`value`:** Receives the number, rounded if a `Double` can't hold it exactly, or 0. |
| `TryGetInt64(out long value)` | `bool` | Gets the number of an `integer`. **Returns:** `true` if this is an `integer` that a `Int64` can hold. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [File.InternetMessage API reference](index.md)
