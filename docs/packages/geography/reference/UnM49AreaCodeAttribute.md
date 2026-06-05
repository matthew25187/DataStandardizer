---
title: UnM49AreaCodeAttribute Class
parent: Geography
grand_parent: Packages
nav_exclude: true
---

# UnM49AreaCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Geography`

Describes a UN M49 area code with its related global, region, sub-region, and
intermediate region codes, plus the names of those codes and the area itself in
English, Chinese, Russian, French, Spanish, and Arabic.

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class UnM49AreaCodeAttribute : CodeAttributeBase
```

## Remarks

Applied to each member of the UN M49 area enums. You normally read the metadata
through the [UnM49Extensions](UnM49Extensions.md) accessors rather than reading the
attribute directly. The per-language name properties are read/write; an unset name
reads as `null`. `CodeAttributeBase` is defined in `DataStandardizer.Core`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `UnM49AreaCodeAttribute(ushort globalCode)` | |
| `UnM49AreaCodeAttribute(ushort globalCode, ushort regionCode)` | |
| `UnM49AreaCodeAttribute(ushort globalCode, ushort regionCode, ushort subRegionCode)` | |
| `UnM49AreaCodeAttribute(ushort globalCode, ushort regionCode, ushort subRegionCode, ushort intermediateRegionCode)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ArabicCountryOrAreaName` | `string? ArabicCountryOrAreaName { get; set; }` | Country or area name in Arabic. |
| `ArabicGlobalName` | `string? ArabicGlobalName { get; set; }` | Global name in Arabic. |
| `ArabicIntermediateRegionName` | `string? ArabicIntermediateRegionName { get; set; }` | Intermediate region name in Arabic. |
| `ArabicRegionName` | `string? ArabicRegionName { get; set; }` | Region name in Arabic. |
| `ArabicSubRegionName` | `string? ArabicSubRegionName { get; set; }` | Sub-region name in Arabic. |
| `ChineseCountryOrAreaName` | `string? ChineseCountryOrAreaName { get; set; }` | Country or area name in Chinese. |
| `ChineseGlobalName` | `string? ChineseGlobalName { get; set; }` | Global name in Chinese. |
| `ChineseIntermediateRegionName` | `string? ChineseIntermediateRegionName { get; set; }` | Intermediate region name in Chinese. |
| `ChineseRegionName` | `string? ChineseRegionName { get; set; }` | Region name in Chinese. |
| `ChineseSubRegionName` | `string? ChineseSubRegionName { get; set; }` | Sub-region name in Chinese. |
| `EnglishCountryOrAreaName` | `string? EnglishCountryOrAreaName { get; set; }` | Country or area name in English. |
| `EnglishGlobalName` | `string? EnglishGlobalName { get; set; }` | Global name in English. |
| `EnglishIntermediateRegionName` | `string? EnglishIntermediateRegionName { get; set; }` | Intermediate region name in English. |
| `EnglishRegionName` | `string? EnglishRegionName { get; set; }` | Region name in English. |
| `EnglishSubRegionName` | `string? EnglishSubRegionName { get; set; }` | Sub-region name in English. |
| `FrenchCountryOrAreaName` | `string? FrenchCountryOrAreaName { get; set; }` | Country or area name in French. |
| `FrenchGlobalName` | `string? FrenchGlobalName { get; set; }` | Global name in French. |
| `FrenchIntermediateRegionName` | `string? FrenchIntermediateRegionName { get; set; }` | Intermediate region name in French. |
| `FrenchRegionName` | `string? FrenchRegionName { get; set; }` | Region name in French. |
| `FrenchSubRegionName` | `string? FrenchSubRegionName { get; set; }` | Sub-region name in French. |
| `GlobalCode` | `ushort? GlobalCode { get; }` | Related global code. |
| `IntermediateRegionCode` | `ushort? IntermediateRegionCode { get; }` | Related intermediate region code. |
| `Iso3166Part1Alpha2Code` | `string? Iso3166Part1Alpha2Code { get; set; }` | Related ISO 3166-1 alpha-2 code. |
| `Iso3166Part1Alpha3Code` | `string? Iso3166Part1Alpha3Code { get; set; }` | Related ISO 3166-1 alpha-3 code. |
| `RegionCode` | `ushort? RegionCode { get; }` | Related region code. |
| `RussianCountryOrAreaName` | `string? RussianCountryOrAreaName { get; set; }` | Country or area name in Russian. |
| `RussianGlobalName` | `string? RussianGlobalName { get; set; }` | Global name in Russian. |
| `RussianIntermediateRegionName` | `string? RussianIntermediateRegionName { get; set; }` | Intermediate region name in Russian. |
| `RussianRegionName` | `string? RussianRegionName { get; set; }` | Region name in Russian. |
| `RussianSubRegionName` | `string? RussianSubRegionName { get; set; }` | Sub-region name in Russian. |
| `SpanishCountryOrAreaName` | `string? SpanishCountryOrAreaName { get; set; }` | Country or area name in Spanish. |
| `SpanishGlobalName` | `string? SpanishGlobalName { get; set; }` | Global name in Spanish. |
| `SpanishIntermediateRegionName` | `string? SpanishIntermediateRegionName { get; set; }` | Intermediate region name in Spanish. |
| `SpanishRegionName` | `string? SpanishRegionName { get; set; }` | Region name in Spanish. |
| `SpanishSubRegionName` | `string? SpanishSubRegionName { get; set; }` | Sub-region name in Spanish. |
| `SubRegionCode` | `ushort? SubRegionCode { get; }` | Related sub-region code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [UnM49Extensions](UnM49Extensions.md)
- [UnM49AreaByAlpha2CountryCode](UnM49AreaByAlpha2CountryCode.md)
- [UnM49AreaByAlpha3CountryCode](UnM49AreaByAlpha3CountryCode.md)
- [Geography API reference](index.md)
</content>
