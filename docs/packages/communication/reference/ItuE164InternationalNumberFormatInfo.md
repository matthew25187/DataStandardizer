---
title: ItuE164InternationalNumberFormatInfo Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ItuE164InternationalNumberFormatInfo Class

## Definition

Namespace: `DataStandardizer.Communication.E164`

Holds the patterns used to format an international number. Once attached to a
read-only `TelephonyInfo`, the patterns cannot be changed.

```csharp
public sealed class ItuE164InternationalNumberFormatInfo : IFormatProvider
```

## Remarks

Both pattern setters throw `InvalidOperationException` when the instance is
read-only. `GetFormat` returns the instance itself when asked for an
`ItuE164InternationalNumberFormatInfo`, and `null` otherwise.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `IsReadOnly` | `bool IsReadOnly { get; }` | Whether the patterns are locked (the setter is `internal`). |
| `LongInternationalNumberPattern` | `string LongInternationalNumberPattern { get; set; }` | Setter throws `InvalidOperationException` when read-only. |
| `ShortInternationalNumberPattern` | `string ShortInternationalNumberPattern { get; set; }` | Setter throws `InvalidOperationException` when read-only. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetFormat(Type? formatType)` | `object?` | `IFormatProvider` implementation (a normal public method); returns itself for `ItuE164InternationalNumberFormatInfo`. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets the `formatType` parameter and the return value of
`GetFormat` are non-nullable; on `net8.0`/`net10.0` they are nullable
(`Type?` / `object?`).

## See also

- [TelephonyInfo](TelephonyInfo.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
