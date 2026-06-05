---
title: Access area metadata
parent: Geography
grand_parent: Packages
nav_order: 5
---

# Access area metadata

Each member of the UN M49 enums carries associated metadata that you retrieve
through extension methods on the enum members.

UN M49 defines names for each element in its geographical hierarchy in six
languages: English, Chinese, Russian, French, Spanish, and Arabic. Each name
accessor takes an ISO 639 language code (either the Part 1 alpha-2 or the
Part 2 alpha-3 form, e.g. `"en"` or `"eng"`).

## Country or area

The name of the country or area for an area code can be retrieved.

```csharp
// Germany name in Arabic
var countryName = UnM49AreaByAlpha2CountryCode.DE.GetCountryOrAreaName("ar");
```

To do the same thing using alpha-3 codes:

```csharp
// Germany name in Arabic
var countryName = UnM49AreaByAlpha3CountryCode.DEU.GetCountryOrAreaName("ara");
```

## Global area

The identifier of the global area can be retrieved for any area code.

```csharp
var globalCode = UnM49AreaByAlpha2CountryCode.PT.GetGlobalCode();
```

And likewise the global name.

```csharp
var globalName = UnM49AreaByAlpha2CountryCode.PT.GetGlobalName("en");
```

In practice the global code is the same no matter which area code it is
retrieved for, because all areas defined by the standard are on planet Earth.

## Intermediate region

Intermediate regions are identified by a code retrieved like so:

```csharp
// Italy region code
var intermediateRegionCode = UnM49AreaByAlpha2CountryCode.IT.GetIntermediateRegionCode();
```

The name of an intermediate region can also be retrieved from an area code.

```csharp
// Italy region name
var intermediateRegionName = UnM49AreaByAlpha2CountryCode.IT.GetIntermediateRegionName("en");
```

## Region

The identifier of an area's region can be retrieved:

```csharp
var regionCode = UnM49AreaByAlpha2CountryCode.CH.GetRegionCode();
```

And the region name:

```csharp
var regionName = UnM49AreaByAlpha2CountryCode.CH.GetRegionName("en");
```

## Sub-region

A sub-region's code can be retrieved from an area code.

```csharp
var subRegionCode = UnM49AreaByAlpha2CountryCode.RO.GetSubRegionCode();
```

And the sub-region name.

```csharp
var subRegionName = UnM49AreaByAlpha2CountryCode.RO.GetSubRegionName("en");
```
