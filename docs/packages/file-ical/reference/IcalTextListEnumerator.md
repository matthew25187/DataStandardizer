---
title: IcalTextListEnumerator Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTextListEnumerator Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

Enumerates the items of a list of TEXT values, split on the commas that aren't
escaped. Get one from `IcalValue.EnumerateTextList`.

**Syntax**

```csharp
public ref struct IcalTextListEnumerator
```

## Remarks

The enumerator works over a `ReadOnlySpan<char>` and doesn't allocate. Use it with
`foreach`. Each item is a slice of the value, with its escaping in place, so parse it
with `IcalValue.TryParseText`.

```csharp
Span<char> buffer = stackalloc char[64];
foreach (var item in IcalValue.EnumerateTextList(@"APPOINTMENT,EDUCATION\, TRAINING".AsSpan(), IcalValueFormat.Text))
{
    if (IcalValue.TryParseText(item, IcalValueFormat.Text, buffer, out var length))
    {
        Console.WriteLine(buffer.Slice(0, length).ToString());   // "APPOINTMENT", then "EDUCATION, TRAINING"
    }
}
```

A backslash escapes the character after it, so a comma after a backslash is part of
the item. An empty value is one empty item, and a comma at the end adds an empty
item. In `IcalValueFormat.Json` and `IcalValueFormat.Xml` form, the value is a single
item, since each item of a jCal or xCal list is a JSON string or an element of its
own.

As a `ref struct`, the enumerator can't be boxed, stored in a field of a class, or
used across an `await`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Current` | `ReadOnlySpan<char> Current { get; }` | The current item, with its escaping in place. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetEnumerator()` | `IcalTextListEnumerator` | Returns the enumerator itself, so that it can be used with `foreach`. |
| `MoveNext()` | `bool` | Moves to the next item. Returns `false` once there are no more items. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Read without allocating](../how-to/read-zero-copy.md)
- [IcalValue](IcalValue.md)
- [IcalValueFormat](IcalValueFormat.md)
- [File.iCal API reference](index.md)
