---
title: Removed in v2.0
parent: Resources
nav_order: 6
---

# Removed in v2.0

Version 2.0 removes the legacy packages that were deprecated in
[Bulletin 202508-01](https://github.com/matthew25187/DataStandardizer/discussions/17).
No new versions of them will be published. Versions already on NuGet can still be
restored, but they get no fixes or data updates.

Each removed package has a maintained replacement, and in most cases migrating is
a matter of swapping the package reference, changing a `using` directive, and
renaming a few types.

## Which package replaces each one

| Removed | Use instead | Namespace |
| --- | --- | --- |
| `DataStandardizer.BCP47` | [`DataStandardizer.LanguageTag`](../packages/language-tag/index.md) | `DataStandardizer.LanguageTag` |
| `DataStandardizer.File` | [`DataStandardizer.File.CSV`](../packages/file-csv/index.md) | `DataStandardizer.File.CSV` |
| `DataStandardizer.ISO15924` | [`DataStandardizer.Language`](../packages/language/index.md) | `DataStandardizer.Language` |
| `DataStandardizer.ISO3166` | [`DataStandardizer.Geography`](../packages/geography/index.md) | `DataStandardizer.Geography` |
| `DataStandardizer.ISO4217` | [`DataStandardizer.Money`](../packages/money/index.md) | `DataStandardizer.Money` |
| `DataStandardizer.ISO639` | [`DataStandardizer.Language`](../packages/language/index.md) | `DataStandardizer.Language` |
| `DataStandardizer.UNM49` | [`DataStandardizer.Geography`](../packages/geography/index.md) | `DataStandardizer.Geography` |

## Type renames

The types below were marked `[Obsolete]` in the removed packages and have no
counterpart under the old name. Use the replacement type that the obsolete warning
pointed to:

| Removed package | Old type | New type |
| --- | --- | --- |
| `DataStandardizer.ISO15924` | `Iso15924` | `Iso15924Script` |
| `DataStandardizer.ISO3166` | `Iso3166Part1Alpha2` | `Iso3166Part1Alpha2Country` |
| `DataStandardizer.ISO3166` | `Iso3166Part1Alpha3` | `Iso3166Part1Alpha3Country` |
| `DataStandardizer.ISO3166` | `Iso3166Part2` | `Iso3166Part2Subdivision` |
| `DataStandardizer.ISO4217` | `Iso4217Current` | `Iso4217CurrencyCurrent` |
| `DataStandardizer.ISO4217` | `Iso4217Historic` | `Iso4217CurrencyHistoric` |
| `DataStandardizer.ISO639` | `Iso639Part1` | `Iso639Part1Language` |
| `DataStandardizer.ISO639` | `Iso639Part2B` | `Iso639Part2BLanguage` |
| `DataStandardizer.ISO639` | `Iso639Part2T` | `Iso639Part2TLanguage` |
| `DataStandardizer.ISO639` | `Iso639Part3` | `Iso639Part3Language` |
| `DataStandardizer.ISO639` | `Iso639Part5` | `Iso639Part5LanguageFamily` |
| `DataStandardizer.UNM49` | `UnM49ByAlpha2Code` | `UnM49AreaByAlpha2CountryCode` |
| `DataStandardizer.UNM49` | `UnM49ByAlpha3Code` | `UnM49AreaByAlpha3CountryCode` |

The extension methods that took an old type, such as `GetEnglishName()` on
`Iso639Part1`, came from the `*CompatibilityExtensions` classes, which are also
gone. Every one of them has an extension method with the same name and parameters
on the new type, so a call compiles unchanged once the value it is called on has
the new type.

Every other public type keeps its name and moves to the replacement package's
namespace. That includes the types the removed packages already shared with their
replacements, such as `Iso3166Part1Alpha2Country`, `Bcp47LanguageTag`,
`SubtagRegistry` and `CsvFileReader`.

## Migrating a project

1. Replace each removed package reference with its replacement. For example, in
   your project file:

   ```xml
   <!-- Before -->
   <PackageReference Include="DataStandardizer.ISO3166" Version="1.*" />
   <!-- After -->
   <PackageReference Include="DataStandardizer.Geography" Version="2.*" />
   ```

   If you referenced two packages that share a replacement, such as
   `DataStandardizer.ISO639` and `DataStandardizer.ISO15924`, you need only one
   reference to `DataStandardizer.Language`.
2. Change the `using` directives to the namespaces in the first table. For
   `DataStandardizer.BCP47`, that includes `DataStandardizer.BCP47.SubtagRegistry`,
   which becomes `DataStandardizer.LanguageTag.SubtagRegistry`. For
   `DataStandardizer.File`, the namespace `DataStandardizer.File.Csv` becomes
   `DataStandardizer.File.CSV`.
3. Rename the obsolete types using the second table. If you build the project
   against the last version of the removed package first, each `CS0618` warning
   names the replacement type.

```csharp
// Before
using DataStandardizer.ISO3166;

Iso3166Part1Alpha2 country = Iso3166Part1Alpha2.NZ;
string? name = country.GetEnglishName(Iso3166CountryName.Short);

// After
using DataStandardizer.Geography;

Iso3166Part1Alpha2Country country = Iso3166Part1Alpha2Country.NZ;
string? name = country.GetEnglishName(Iso3166CountryName.Short);
```

`DataStandardizer.LanguageTag` and `DataStandardizer.File.CSV` have changed since
they took over from the removed packages, so check their pages for anything your
code relies on, such as the
[deprecated LanguageTag timeouts](../packages/language-tag/concepts/builder-pipeline.md).

## The netstandard1.0 target

Version 2.0 also drops the `netstandard1.0` target from three packages that stay:

| Package | Lowest target in v1.x | Lowest target in v2.0 |
| --- | --- | --- |
| `DataStandardizer.Communication` | `netstandard1.0` | `netstandard1.1` |
| `DataStandardizer.LanguageTag` | `netstandard1.0` | `netstandard1.1` |
| `DataStandardizer.Money` | `netstandard1.0` | `netstandard1.1` |

**DataStandardizer.LanguageTag** now uses `System.Memory` spans on every target,
and `System.Memory` starts at `netstandard1.1`. That puts the `ReadOnlySpan<char>`
overloads of `Bcp47LanguageTag` on its lowest target as well.
**DataStandardizer.Communication** and **DataStandardizer.Money** depend on
**DataStandardizer.LanguageTag**, so they move with it.

.NET Standard 1.0 and 1.1 run on the same platforms, so no application loses
support. The change matters only to a library that itself targets
`netstandard1.0` and references one of these packages. Retarget that library to
`netstandard1.1` or later, or keep it on version 1.x of the package.
