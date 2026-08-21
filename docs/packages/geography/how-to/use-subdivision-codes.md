---
title: Use subdivision codes
parent: Geography
grand_parent: Packages
nav_order: 2
---

# Use subdivision codes

ISO 3166-2 subdivisions are represented by instances of the
`Iso3166Part2Subdivision` type, which you access through predefined instances
rather than constructing yourself.

Because ISO 3166-2 defines a distinct set of subdivision codes for most
countries, the predefined subdivision codes are organised hierarchically by the
country they belong to. This takes the form of nested classes on
`Iso3166Part2Subdivision` named after the ISO 3166-1 alpha-2 country code, each
containing a member for every subdivision code ISO 3166-2 defines for that
country. For example:

```csharp
// Australian Capital Territory
var act = Iso3166Part2Subdivision.AU._ACT;
```

You may notice that all subdivision codes start with an underscore. This is
because of a technical limitation on identifier naming in .NET: an identifier
cannot start with a digit, and some ISO 3166-2 subdivision codes do start with
digits. To keep things consistent, every subdivision code member starts with an
underscore.

To read the names and other metadata associated with a subdivision code, see
[Access country and subdivision metadata](access-country-and-subdivision-metadata.md).
