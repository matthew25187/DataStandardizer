---
title: Metadata and lookups
parent: Concepts
nav_order: 3
---

# Metadata and lookups

A code on its own is terse — `NZD`, `nz`, `Latn` — so each standard's extra
information (human-readable names, numeric codes, related codes, and other
attributes) is attached to the individual code members as .NET attributes and
read back through `Get*` extension methods.

## How metadata is attached

Each package defines one or more `*CodeAttribute` types that derive from
`Core`'s `CodeAttributeBase`. The attribute is applied to a code member's field
and given the standard's data as constructor arguments and properties. For
currencies, `Iso4217CurrencyCodeAttribute` (sealed, deriving from
`CodeAttributeBase`) carries the currency name plus a nullable `MinorUnits`
(decimal places) and an `IsFundsCode` flag. For languages,
`Iso639LanguageCodeAttribute` carries English and French names, optional related
part codes (`Part1Code`, `Part2BCode`, `Part2TCode`), `Scope`, `LanguageType`,
`PrintName`, `InvertedName`, and `MacrolanguageCode`.

```text
  code member (enum/struct field)
        │  decorated with
        ▼
  *CodeAttribute : CodeAttributeBase      ← carries the metadata
        │  read back by
        ▼
  Get* extension method                   ← reflects the attribute off the member
```

`CodeAttributeBase` itself supplies the name plumbing common to every standard:
it stores English and French name arrays and exposes `EnglishName` /
`EnglishNames` and `FrenchName` / `FrenchNames`, where the singular property
returns the first name in the collection.

## How metadata is retrieved

You rarely touch the attributes directly. Each package exposes `Get*` extension
methods on its code types that locate the member's field, pull off the relevant
`*CodeAttribute`, and return the value you asked for — or `null` when the member
carries no such metadata.

### Example: currency metadata (verified)

`Iso4217Extensions` provides extension methods on `Iso4217CurrencyCurrent` and
`Iso4217CurrencyHistoric`:

```csharp
using DataStandardizer.Money;

var code = Iso4217CurrencyCurrent.NZD;

string? name  = code.GetCurrencyName();   // "New Zealand Dollar"
byte?   units = code.GetMinorUnits();     // 2
bool    funds = code.IsFundCode();        // false
```

Internally each method does the same thing: it resolves the member's field via
`Enum.GetName`, calls `GetCustomAttribute<Iso4217CurrencyCodeAttribute>()`, and
returns the requested property (or `null` if the attribute is absent).

### Example: language names (verified)

`Iso639Extensions` exposes name and related-code accessors across the ISO 639
part types — for instance `GetEnglishName`, `GetEnglishNames`, `GetFrenchName`,
and (on the richer Part 3 type) `GetPrintName`, `GetInvertedName`,
`GetMacrolanguageCode`, `GetScope`, and `GetLanguageType`:

```csharp
using DataStandardizer.Language;

string? english = Iso639Part1Language.en.GetEnglishName();   // "English"
string? french  = Iso639Part1Language.en.GetFrenchName();    // "anglais"
```

For the string-backed ISO 639 structs, the helper that finds the member field
uses `Core`'s `StringEnum.GetName` rather than `Enum.GetName`, but the overall
shape — find the field, read its attribute, return a property — is identical.

## Numeric and multi-valued lookups

Not every lookup returns a single name. Some standards attach numeric codes or
several values per member, and the extension surface follows suit:

- **Numeric codes.** `UnM49Extensions.GetM49Codes` collects the numeric M49
  codes (global, region, sub-region, intermediate-region, and the area codes
  themselves) for a UN M49 enum type.
- **Localised names.** UN M49 country/area names exist in several languages, so
  methods like `GetCountryOrAreaName`, `GetRegionName`, and `GetGlobalName` take
  an ISO 639 `languageCode` argument and return the matching localised name.
- **Name collections.** Where a code has more than one name, the plural
  `GetEnglishNames` / `GetFrenchNames` return the whole array (backed by
  `CodeAttributeBase.EnglishNames` / `FrenchNames`).

## Why this design

- **The code stays small; the data stays attached.** Metadata rides on the
  member declaration, not in a separate lookup table you must keep in sync.
- **Uniform access.** Every standard reads back through `Get*` extension methods,
  so the pattern is the same package to package.
- **Safe absence.** Missing metadata yields `null` rather than throwing, so
  optional fields degrade gracefully.

## Related

- [Strongly-typed codes](strongly-typed-codes.md)
- [Access country and subdivision metadata](../packages/geography/how-to/access-country-and-subdivision-metadata.md)
- [Access currency metadata](../packages/money/how-to/access-currency-metadata.md)
