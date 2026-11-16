---
title: MissingCultureResourceException Class
parent: Money
grand_parent: Packages
nav_exclude: true
---

# MissingCultureResourceException Class

## Definition

Namespace: `DataStandardizer.Money`

Raised when a resource required to format monetary values for a culture is
absent.

```csharp
[Obsolete]
public sealed class MissingCultureResourceException : Exception
```

## Remarks

The package no longer throws this exception. Culture information now comes from
CLDR data built into the package, which is complete for every culture, so no value
can be missing (see [CurrencyFormatInfo](CurrencyFormatInfo.md)). The type is
obsolete: it remains so that code which refers to it still compiles, with a
warning, and will be removed in a future major version. Remove code that catches
it.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MissingCultureResourceException(string, string)` | Names the culture and the resource which is absent. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `CultureName` | `string` | Name of the culture whose resource is absent. |
| `ResourceName` | `string` | Name of the absent resource. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [CurrencyFormatInfo](CurrencyFormatInfo.md)
- [MoneyInfo](MoneyInfo.md)
- [Money API reference](index.md)
