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
public sealed class MissingCultureResourceException : Exception
```

## Remarks

Resource lookup falls back towards the neutral resources, so a culture which
defines none of its own is served by them rather than failing. This exception
therefore indicates that a required value is absent from the neutral resources as
well, which should not occur in a correctly built package.

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

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [CurrencyFormatInfo](CurrencyFormatInfo.md)
- [MoneyInfo](MoneyInfo.md)
- [Money API reference](index.md)
