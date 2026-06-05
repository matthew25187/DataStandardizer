---
title: Access country and subdivision metadata
parent: Geography
grand_parent: Packages
nav_order: 3
---

# Access country and subdivision metadata

Each ISO 3166 code carries associated metadata that you retrieve through
extension methods on a member of one of the ISO 3166 enums.

## ISO 3166-1 country codes

### English name

You can get the English name for a country by its alpha-2 or alpha-3 code. The
`Iso3166CountryName` enum lets you choose the form of the name: `Short`,
`ShortUpper`, or `Full`.

```csharp
// Thailand full name by alpha-2 code
var thailandName = Iso3166Part1Alpha2Country.TH.GetEnglishName(Iso3166CountryName.Full);
```

or,

```csharp
// USA short name by alpha-3 code
var usaName = Iso3166Part1Alpha3Country.USA.GetEnglishName(Iso3166CountryName.Short);
```

### Native name

You can get the native name for a country by its alpha-2 or alpha-3 code. You
supply an ISO 639 language code for the language of the name and choose the form
of the name (as with the English name).

```csharp
// Greece short name in Greek by alpha-2 code
var greeceName = Iso3166Part1Alpha2Country.GR.GetNativeName("el", Iso3166CountryName.Short);
```

N.B. For countries that are natively English-speaking, the native name may
already be in English but may also have been defined in other languages.

### Independence

You can get a flag indicating whether the territory is an independent nation or
administered by another country.

```csharp
// Greenland independence
var isGreenlandIndependent = Iso3166Part1Alpha2Country.GL.IsIndependent();
```

## ISO 3166-2 subdivision codes

### Category identifier

You can get the identifier of the category to which a subdivision belongs.

```csharp
// Australian Northern Territory category
var ntCategoryId = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCategoryIdentifier();
```

### Category name

You can get the name of the category to which a subdivision belongs, in a
supported language.

```csharp
// Australian Northern Territory category
var ntCategoryName = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCategoryName("en");
```

The name of a category may also be available in a plural form, which is
retrieved separately.

```csharp
// Australian Northern Territory category, plural
var ntCategoryNamePlural = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCategoryNamePlural("en");
```

### Code

You can get the subdivision code as defined by ISO 3166-2 from an
`Iso3166Part2Subdivision`.

```csharp
var subdivisionCode = Iso3166Part2Subdivision.AU._NT.GetSubdivisionCode();  // returns AU-NT
```

### Native name

You can get the native name of a subdivision in a supported language.

```csharp
var nativeName = Iso3166Part2Subdivision.ZA._EC.GetSubdivisionNativeName("af");
```

There may also be a local variant of the native name, retrieved separately.

```csharp
var nativeNameLocal = Iso3166Part2Subdivision.ZA._EC.GetSubdivisionNativeNameLocalVariant("zu");
```

### Parent code

Some countries have a hierarchical structure to their subdivisions. You can
retrieve the parent code for a subdivision from an `Iso3166Part2Subdivision`.

```csharp
var subdivisionParentCode = Iso3166Part2Subdivision.PH._SCO.GetSubdivisionParentCode();
```
