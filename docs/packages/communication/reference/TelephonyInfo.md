---
title: TelephonyInfo Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# TelephonyInfo Class

## Definition

Namespace: `DataStandardizer.Communication`

Supplies culture/region-aware formatting for `ItuE164InternationalNumber`. Pass a
`TelephonyInfo` as the `IFormatProvider` to a `ToString` overload or to
`string.Format`.

```csharp
public class TelephonyInfo : IFormatProvider
```

## Remarks

A `TelephonyInfo` carries an `ItuE164InternationalNumberFormatInfo` and, optionally,
an associated country. The instance returned by `InvariantTelephony` (and by
`CurrentTelephony`) is read-only; attempting to replace its format info throws
`InvalidOperationException`. Country-specific telephony rules are not yet fully
implemented.

## Constructors

| Constructor | Notes |
| --- | --- |
| `TelephonyInfo()` | Invariant configuration. |
| `TelephonyInfo(Iso3166Part1Alpha2Country countryCode)` | Country-specific. |
| `TelephonyInfo(Iso3166Part1Alpha3Country countryCode)` | Country-specific. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CurrentTelephony` | `static TelephonyInfo CurrentTelephony { get; }` | Based on the current thread's region. *(netstandard2.0+/.NET)* |
| `InvariantTelephony` | `static TelephonyInfo InvariantTelephony { get; }` | A read-only, culture-neutral instance. |
| `Iso3166Part1Alpha2Code` | `Iso3166Part1Alpha2Country? Iso3166Part1Alpha2Code { get; }` | The associated country, or `null`. |
| `Iso3166Part1Alpha3Code` | `Iso3166Part1Alpha3Country? Iso3166Part1Alpha3Code { get; }` | The associated country, or `null`. |
| `IsReadOnly` | `bool IsReadOnly { get; }` | Whether the instance is read-only (the setter is `internal`). |
| `ItuE164InternationalNumberFormat` | `ItuE164InternationalNumberFormatInfo ItuE164InternationalNumberFormat { get; set; }` | The format info used for output; the setter throws `InvalidOperationException` when read-only. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetFormat(Type? formatType)` | `object?` | `IFormatProvider` implementation (a normal public method); serves `ICustomFormatter` and `ItuE164InternationalNumberFormatInfo`. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.
`CurrentTelephony` requires .NET Standard 2.0 or later (`net8.0`, `net10.0`,
`netstandard2.0`); it is not available on the `netstandard1.0` target. On the
.NET Standard targets the `formatType` parameter and the return value of
`GetFormat` are non-nullable; on `net8.0`/`net10.0` they are nullable
(`Type?` / `object?`).

## See also

- [Use E.164 international numbers](../how-to/use-international-numbers.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [ItuE164InternationalNumberFormatInfo](ItuE164InternationalNumberFormatInfo.md)
- [Communication API reference](index.md)
</content>
