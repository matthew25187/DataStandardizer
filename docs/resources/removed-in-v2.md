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
[LanguageTag match timeouts](#languagetag-match-timeouts),
[builder region overloads](#languagetag-builder-region-overloads) and
[what creating a language tag checks](#what-creating-a-language-tag-checks) below.

## LanguageTag match timeouts

`DataStandardizer.LanguageTag` checks language tags with a parser, which can't
time out, so a match timeout has had no effect since version 1.x. The members
that took one were marked `[Obsolete]` in version 1.x and are removed in version
2.0:

| Removed | Use instead |
| --- | --- |
| `Bcp47LanguageTag.Create(string, TimeSpan)` | `Bcp47LanguageTag.Create(string)` |
| `Bcp47LanguageTag.Create(string, SubtagRegistry, TimeSpan)` | `Bcp47LanguageTag.Create(string, SubtagRegistry)` |
| `Bcp47LanguageTag.TryCreate(string?, TimeSpan, out Bcp47LanguageTag)` | `Bcp47LanguageTag.TryCreate(string?, out Bcp47LanguageTag)` |
| `Bcp47LanguageTag.TryCreate(string?, SubtagRegistry, TimeSpan, out Bcp47LanguageTag)` | `Bcp47LanguageTag.TryCreate(string?, SubtagRegistry, out Bcp47LanguageTag)` |
| `Bcp47LanguageTagBuilder.WithTimeout(TimeSpan)` | Leave the step out |
| `IBcp47LanguageTagBuilderStepWithTimeout` | No replacement |
| `IBcp47LanguageTagBuilderStepWithTimeoutNext` | No replacement |

Drop the timeout argument, or the `WithTimeout(...)` call, and the code builds
against version 2.0 with the same behaviour. The removed overloads threw
`ArgumentOutOfRangeException` for a timeout out of range; the ones that remain
have no timeout to check.

```csharp
// Before
var languageTag = Bcp47LanguageTag.Create("en-NZ", TimeSpan.FromSeconds(1));
var built = new Bcp47LanguageTagBuilder()
    .WithTimeout(TimeSpan.FromSeconds(1))
    .UsingLanguageTag("en-NZ")
    .Build();

// After
var languageTag = Bcp47LanguageTag.Create("en-NZ");
var built = new Bcp47LanguageTagBuilder()
    .UsingLanguageTag("en-NZ")
    .Build();
```

## LanguageTag builder region overloads

`Bcp47LanguageTagBuilder` no longer has a `UsingRegionSubtag` overload that takes
`UnM49AreaByAlpha2CountryCode` or `UnM49AreaByAlpha3CountryCode`. Every defined
member of those enums is a country code, which BCP 47 doesn't allow as a Region
subtag, so the overloads were marked `[Obsolete]` in version 1.x. They only
succeeded for an undefined value cast from the code of an area made up of more
than one country. The enums themselves stay in `DataStandardizer.Geography`.

| Removed | Use instead |
| --- | --- |
| `UsingRegionSubtag(UnM49AreaByAlpha2CountryCode)` | `UsingRegionSubtag(UnM49Area)` for an area made up of more than one country, or `UsingRegionSubtag(Iso3166Part1Alpha2Country)` for a country |
| `UsingRegionSubtag(UnM49AreaByAlpha3CountryCode)` | `UsingRegionSubtag(UnM49Area)` for an area made up of more than one country, or `UsingRegionSubtag(Iso3166Part1Alpha2Country)` for a country |

Replace a cast with the `UnM49Area` member for the same code, and a country member
of either enum with the matching `Iso3166Part1Alpha2Country` member. A country
member threw `ArgumentException` in the removed overloads, and it now builds a tag
with the country as its Region subtag.

```csharp
// Before
var latinAmerica = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag("es")
    .UsingRegionSubtag((UnM49AreaByAlpha2CountryCode)419)
    .Build();
var newZealand = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag("en")
    .UsingRegionSubtag(UnM49AreaByAlpha3CountryCode.NZL)
    .Build();

// After
var latinAmerica = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag("es")
    .UsingRegionSubtag(UnM49Area._419)
    .Build();
var newZealand = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag("en")
    .UsingRegionSubtag(Iso3166Part1Alpha2Country.NZ)
    .Build();
```

## What creating a language tag checks

Version 1.x checked a language tag created without a subtag registry against a
hybrid of the grammar and the ISO code lists, which is neither of the two classes
that RFC 5646 §2.2.9 defines. In version 2.0, creating a tag without a registry
checks that it's **well-formed**: it matches the ABNF of RFC 5646 §2.1, and nothing
else is checked. Creating one with a registry still checks that it's **valid**.

`Bcp47LanguageTag.Create(string)`, `TryCreate`, `Parse`, `TryParse` and the explicit
conversion from `string` now accept tags that they rejected in version 1.x:

| Tag | Why version 1.x rejected it |
| --- | --- |
| `zz` | Not an ISO 639 code |
| `en-Abcd` | Not an ISO 15924 code |
| `en-830`, `en-434` | Not a UN M.49 area that RFC 5646 allows as a region |
| `de-1901-1901`, `en-a-bb-a-cc` | Repeats a variant subtag or an extension singleton, which RFC 5646 §2.2.9 lists under validity, not well-formedness |

`IsWellFormedLanguageTagString` is removed, and the name won't come back, so a
caller moving to version 2.0 gets a compile error rather than a method that means
something new. Choose its replacement by what you need:

| Removed | Use instead | Checks |
| --- | --- | --- |
| `IsWellFormedLanguageTagString(string)` | `IsWellFormed(string)` | The ABNF only |
| `IsWellFormedLanguageTagString(ReadOnlySpan<char>)` | `IsWellFormed(ReadOnlySpan<char>)` | The ABNF only |
| Either | `IsValid(string, SubtagRegistry)` or `IsValid(ReadOnlySpan<char>, SubtagRegistry)` | Validity against a registry (RFC 5646 §2.2.9) |

If you relied on the ISO code lists and can't load a registry, opt in to them with
`LanguageTagConformance.IsoCodeLists`. That checks what version 1.x checked:

```csharp
// Before
var languageTag = Bcp47LanguageTag.Create("en-NZ");
var isListed = Bcp47LanguageTag.IsWellFormedLanguageTagString("en-NZ");

// After: the same checks as version 1.x
var languageTag = Bcp47LanguageTag.Create("en-NZ", LanguageTagConformance.IsoCodeLists);
var isListed = Bcp47LanguageTag.TryCreate("en-NZ", LanguageTagConformance.IsoCodeLists, out _);
```

The new `Conformance` property of a `Bcp47LanguageTag` says which of these was
checked when it was created. The message of `LanguageTagFormatException` now names
the class that failed, so code that compares the message text needs updating.

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
