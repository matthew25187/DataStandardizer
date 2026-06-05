---
title: ItuE164AssignedCountryCodesForGlobalServices Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ItuE164AssignedCountryCodesForGlobalServices Enum

## Definition

Namespace: `DataStandardizer.Communication.E164`

The ITU E.164 country codes assigned for global services. Used as the country-code
argument of `ItuE164InternationalNumber.CreateNumberForGlobalService`.

```csharp
public enum ItuE164AssignedCountryCodesForGlobalServices : ushort
```

## Fields

| Field | Value | Description |
| --- | --- | --- |
| `IFS` | `800` | International Freephone Service. |
| `ISCS` | `808` | International Shared Cost Service. |
| `SNAC` | `870` | Inmarsat SNAC (Shared Network Access Code). |
| `IPRS` | `979` | International Premium Rate Service. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [IItuE164InternationalNumberForGlobalServices](IItuE164InternationalNumberForGlobalServices.md)
- [Communication API reference](index.md)
</content>
