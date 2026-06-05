---
title: Use country codes
parent: Geography
grand_parent: Packages
nav_order: 1
---

# Use country codes

ISO 3166-1 country codes are available in both alpha-2 and alpha-3 variants,
exposed as the `Iso3166Part1Alpha2Country` and `Iso3166Part1Alpha3Country`
enums respectively.

## Alpha-2 country codes

The `Iso3166Part1Alpha2Country` enum contains a member for each ISO 3166-1
alpha-2 country code. The name of the member is the country code, and its value
is the numeric code associated with the alpha-2 code.

```csharp
// United Kingdom of Great Britain and Northern Ireland
var ukCountryCode = Iso3166Part1Alpha2Country.GB;
```

## Alpha-3 country codes

The `Iso3166Part1Alpha3Country` enum contains a member for each ISO 3166-1
alpha-3 country code. The name of the member is the country code, and its value
is the numeric code associated with the alpha-3 code.

```csharp
// Greece
var greeceCountryCode = Iso3166Part1Alpha3Country.GRC;
```

To read the names and other metadata associated with a country code, see
[Access country and subdivision metadata](access-country-and-subdivision-metadata.md).
