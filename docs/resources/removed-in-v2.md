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
   `DataStandardizer.BCP47`, that includes `DataStandardizer.BCP47.SubtagRegistry`:
   see [where the registry types live](#where-the-registry-types-live). For
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
[builder region overloads](#languagetag-builder-region-overloads),
[what creating a language tag checks](#what-creating-a-language-tag-checks) and
[where the registry types live](#where-the-registry-types-live), the
[File.CSV line model](#filecsv-line-model) and
[File.CSV mapper registration](#filecsv-mapper-registration) and
[File.CSV conversion of mapped properties](#filecsv-conversion-of-mapped-properties)
below.

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
| Either | `IsValid(string, LanguageTagConformance)` or `IsValid(ReadOnlySpan<char>, LanguageTagConformance)` with `IsoCodeLists` | The ISO code lists |

If you relied on the ISO code lists and can't load a registry, opt in to them with
`LanguageTagConformance.IsoCodeLists`. That checks what version 1.x checked, with
one correction: it rejects a three-letter code of a language that has an ISO 639-1
code, such as `eng`, `deu` or `fra`, as RFC 5646 §2.2.1 requires. Use `en`, `de` or
`fr` instead. It remains an approximation of validity: it doesn't check variants,
languages of four to eight letters, or extended language prefixes.

```csharp
// Before
var languageTag = Bcp47LanguageTag.Create("en-NZ");
var isListed = Bcp47LanguageTag.IsWellFormedLanguageTagString("en-NZ");

// After: the ISO code lists
var languageTag = Bcp47LanguageTag.Create("en-NZ", LanguageTagConformance.IsoCodeLists);
var isListed = Bcp47LanguageTag.IsValid("en-NZ", LanguageTagConformance.IsoCodeLists);
```

The new `Conformance` property of a `Bcp47LanguageTag` says which of these was
checked when it was created. The message of `LanguageTagFormatException` now names
the class that failed, so code that compares the message text needs updating.

### Subtag checks and the builder

The `Check*Subtag` methods of `Bcp47LanguageTag` now check only the shape of a
subtag, and look nothing up. `CheckPrimaryLanguageSubtag("zz")` and
`CheckRegionSubtag("830")` return `true`. If you used them to look a code up, use
`ToIso639Part1()` and the other `ToIso*()` methods of a tag, or the enums in
`DataStandardizer.Language` and `DataStandardizer.Geography`.

The string steps of `Bcp47LanguageTagBuilder` use those checks, so they no longer
throw for a subtag of the right shape that isn't in an ISO list. `Build()` checks
validity against the registry if you gave one, and otherwise that the tag is
well-formed. To keep the ISO code lists, add the new first step:

```csharp
// Before
var languageTag = new Bcp47LanguageTagBuilder()
    .UsingPrimaryLanguageSubtag("en")
    .UsingRegionSubtag("NZ")
    .Build();

// After: the same list checks, now made by Build()
var languageTag = new Bcp47LanguageTagBuilder()
    .WithConformance(LanguageTagConformance.IsoCodeLists)
    .UsingPrimaryLanguageSubtag("en")
    .UsingRegionSubtag("NZ")
    .Build();
```

A subtag that isn't in a list now fails at `Build()` with
`LanguageTagFormatException`, instead of at its own step with `ArgumentException`.
The builder still refuses a repeated variant subtag or extension singleton.

## Where the registry types live

The class `SubtagRegistry` used to be in a namespace with the same name, so code
that used it had to write `SubtagRegistry.SubtagRegistry` or add an alias. In
version 2.0, the class is in `DataStandardizer.LanguageTag`, beside
`Bcp47LanguageTag`, and the types that describe its records are in the new namespace
`DataStandardizer.LanguageTag.Registry`:

| Type | Version 1.x namespace | Version 2.0 namespace |
| --- | --- | --- |
| `SubtagRegistry` | `DataStandardizer.LanguageTag.SubtagRegistry` | `DataStandardizer.LanguageTag` |
| `ISubtagRegistryRecord`, `SubtagRegistryFileDateRecord`, `SubtagRegistryRecordBase`, `SubtagRegistryRecordType`, `SubtagRegistrySubtagRecord`, `SubtagRegistryTagRecord`, `SubtagRegistryTagRecordBase` | `DataStandardizer.LanguageTag.SubtagRegistry` | `DataStandardizer.LanguageTag.Registry` |

Change the `using` directive, and remove any alias or doubled name. If you only load
a registry to create language tags, you need no second directive.

```csharp
// Before
using DataStandardizer.LanguageTag;
using Registry = DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistry;

var registry = Registry.CreateFromFile(path);
var languageTag = Bcp47LanguageTag.Create("en-NZ", registry);

// After
using DataStandardizer.LanguageTag;

var registry = SubtagRegistry.CreateFromFile(path, SubtagRegistryLoadOptions.AcceptBareLineFeed);
var languageTag = Bcp47LanguageTag.Create("en-NZ", registry);
```

Version 2.0 also reads the registry by the record-jar format of RFC 5646 §3.1.1,
which ends every line, the last one included, with CRLF. IANA publishes the
registry with a line feed alone at the end of each line, so a copy of it that
version 1.x loaded now throws `FormatException` unless you pass
`SubtagRegistryLoadOptions.AcceptBareLineFeed`, as above. Pass
`SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak` as well for a copy whose last
line has no line break.

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

## The JetBrains.Annotations dependency

In version 1.x, the `netstandard1.x` and `netstandard2.0` builds of each package
list `JetBrains.Annotations` as a NuGet dependency. In version 2.0 they don't, and
their assemblies have no reference to it. Those builds carry the nullable attributes
from `System.Diagnostics.CodeAnalysis` instead, as internal types in each assembly.

This matters only if your own code uses the JetBrains attributes and you got the
package through DataStandardizer. Add a reference to `JetBrains.Annotations` to
your project.

## Chronology time zone metadata

In version 1.x, the location metadata of a `TzDataTimezone` was read through
extension methods on `TzDataExtensions`, which read a `TzDataTimezoneAttribute`
applied to each time zone field. Version 1.x added properties of `TzDataTimezone`
that return the same metadata, and marked the extension methods and the attribute
`[Obsolete]`. Version 2.0 removes them:

| Removed | Use instead |
| --- | --- |
| `TzDataExtensions.GetComment(TzDataTimezone)` | `TzDataTimezone.Comment` |
| `TzDataExtensions.GetIsoCountryCodes(TzDataTimezone)` | `TzDataTimezone.IsoCountryCodes` |
| `TzDataExtensions.GetLatitude(TzDataTimezone)` | `TzDataTimezone.Latitude` |
| `TzDataExtensions.GetLongitude(TzDataTimezone)` | `TzDataTimezone.Longitude` |
| `TzDataTimezoneAttribute`, read from a field by reflection | The four properties above, read from the field's value |
| `TzDataTimezoneAttribute.Comment`, `IsoCountryCodes`, `Latitude` and `Longitude` | The property of `TzDataTimezone` with the same name |

Code that names the attribute type, such as
`field.GetCustomAttribute<TzDataTimezoneAttribute>()`, no longer compiles. The
time zone fields no longer carry any attribute, so code that finds it without
naming the type, such as through `field.CustomAttributes`, finds nothing.

The properties differ from the removed methods in two ways:

- `IsoCountryCodes` returns a read-only `IReadOnlyList<string>` rather than a
  `string[]`.
- For `default(TzDataTimezone)`, the properties return the metadata of `Etc/UTC`,
  the time zone that the default instance stands for: no countries, no comment,
  and a latitude and longitude of 0. See
  [Chronology default time zone](#chronology-default-time-zone).

```csharp
// Before
string[] countryCodes = TzDataTimezone.Europe.Zurich.GetIsoCountryCodes();
double latitude = TzDataTimezone.Europe.Zurich.GetLatitude();
string? comment = TzDataTimezone.Europe.Zurich.GetComment();

// After
IReadOnlyList<string> countryCodes = TzDataTimezone.Europe.Zurich.IsoCountryCodes;
double latitude = TzDataTimezone.Europe.Zurich.Latitude;
string? comment = TzDataTimezone.Europe.Zurich.Comment;
```

## Chronology conversion from string

In version 1.x, the explicit conversion from `string` to `TzDataTimezone`
accepted any string. `(TzDataTimezone)"Not/AZone"` returned an instance that
matched no time zone field and had no zone lines or metadata.

In version 2.0, the conversion finds the time zone through the same lookup as
`TzDataTimezone.Parse`:

| String | Version 1.x | Version 2.0 |
| --- | --- | --- |
| The identifier of a time zone field, such as `Europe/Oslo` | A new instance with that identifier | The field, such as `TzDataTimezone.Europe.Oslo` |
| A deprecated name in the TZ Database's `backward` file, such as `Asia/Calcutta` | A new instance with that name, which matched no time zone field | The canonical field, such as `TzDataTimezone.Asia.Kolkata` |
| Any other string | A new instance with that string, which matched no time zone field | Throws `InvalidCastException` |
| `null` | Throws `ArgumentNullException` | Throws `InvalidCastException` |

Code that casts identifiers it knows exist keeps working. Code that casts a string
that may not be an identifier, such as user input, should call `TryParse` instead:

```csharp
// Before
var timezone = (TzDataTimezone)input;

// After
if (!TzDataTimezone.TryParse(input, out var timezone))
{
    // input is not the identifier of a known time zone
}
```

A cast from a deprecated name now returns the canonical time zone, so its
identifier is the canonical one: `(string)(TzDataTimezone)"Asia/Calcutta"` is
`"Asia/Kolkata"`, where in version 1.x it was `"Asia/Calcutta"`.

## Chronology default time zone

In version 1.x, `default(TzDataTimezone)` was an instance with no identifier. Its
conversion to `string` returned `null`, `ToString` returned the name of the type,
and the members added in version 1.x for zone lines, location metadata and offset
calculations threw `InvalidOperationException`.

In version 2.0, `default(TzDataTimezone)` is Coordinated Universal Time,
`Etc/UTC`, in the same way that `default(TimeSpan)` is `TimeSpan.Zero`. It is
equal to the new field `TzDataTimezone.Etc.UTC`, and no member throws because a
time zone is the default value. A member still throws for an invalid argument,
as `CompareTo(null)` throws `ArgumentNullException`:

| Member | Version 1.x | Version 2.0 |
| --- | --- | --- |
| Implicit conversion to `string` | `null` | `"Etc/UTC"` |
| `ToString()` | `"DataStandardizer.Chronology.TzDataTimezone"` | `"UTC"`, as for `TzDataTimezone.Etc.UTC` |
| `==` and `Equals` with `TzDataTimezone.Etc.UTC` | No such field | `true`, with equal hash codes |
| `CompareTo` | Throws `NullReferenceException` | Compares `"Etc/UTC"` with the other identifier in ordinal order |
| `GetTypeCode()` | Throws `InvalidOperationException` | `TypeCode.String` |
| `ZoneLines`, `IsLink` and `Canonical` | Throw `InvalidOperationException` | The single zone line of `Etc/UTC`, `false`, and `Etc/UTC` |
| `Latitude`, `Longitude`, `IsoCountryCodes` and `Comment` | Throw `InvalidOperationException` | `0`, `0`, empty, and `null` |
| `TzDataExtensions` methods, `ToDateTimeOffset` and `ToOffset` | Throw `InvalidOperationException` | An offset of 0, no daylight saving time, the abbreviation `UTC` and no transitions |

The implicit conversion to `string` is now declared as returning `string`, not
`string?`, as it never returns `null`. Code that tested its result for `null` to
find a time zone that was never set should keep the time zone in a
`TzDataTimezone?` instead:

```csharp
// Before
TzDataTimezone timezone = default;
string? identifier = timezone;
if (identifier is null)
{
    // no time zone
}

// After
TzDataTimezone? timezone = null;
if (timezone is null)
{
    // no time zone
}
```

`TryParse` still sets its `result` to `default` when it returns `false`, which is
now `Etc/UTC`, so test its return value rather than `result`.

`CompareTo` also works for any two time zones, ordering them by identifier. In
version 1.x it threw `ArgumentException` for every pair, so `List<T>.Sort()` and
`OrderBy` over time zones failed.

`Parse`, `TryParse` and the explicit conversion return `TzDataTimezone.Etc.UTC`
for `Etc/UTC` and for the names that the TZ Database's `backward` file links to
it, such as `UTC`, `Zulu` and `Etc/Universal`.

## File.CSV line model

In version 1.x, `ICsvFileLine` is the non-generic `IOrderedDictionary`: its keys
are `object`, enumerating it gives `DictionaryEntry` items, and `Keys` and `Values`
are non-generic `ICollection`s. In version 2.0, `ICsvFileLine` is an ordered
collection of `KeyValuePair<string, object?>` items with string field names, and
none of the `IOrderedDictionary`, `IDictionary` or `ICollection` members remain.

The fields keep their positions and order, so code that reads a field by position
gets the same field as before, including after `Insert` or `RemoveAt`. Values are
still `object?`, because the fields of one line can hold values of different types.

| Version 1.x | Version 2.0 |
| --- | --- |
| `this[object key]` | `this[string fieldName]` |
| `this[int index]` | `this[int index]`, unchanged |
| `Add(object key, object? value)` | `Add(string fieldName, object? value)` |
| `Insert(int index, object key, object? value)` | `Insert(int index, string fieldName, object? value)` |
| `Contains(object key)` | `Contains(string fieldName)` |
| `Remove(object key)` | `Remove(string fieldName)`, which returns `true` if it removed a field |
| `Keys` (`ICollection`) | `Keys` (`IReadOnlyList<string>`) |
| `Values` (`ICollection`) | `Values` (`IReadOnlyList<object?>`) |
| Enumerating `DictionaryEntry` items | Enumerating `KeyValuePair<string, object?>` items |
| `GetEnumerator()` returning `IDictionaryEnumerator` | `GetEnumerator()` returning `IEnumerator<KeyValuePair<string, object?>>` |
| `CopyTo(Array, int)` | No replacement; use LINQ's `ToArray()` |
| `IsFixedSize`, `IsReadOnly`, `IsSynchronized`, `SyncRoot` | No replacement |
| No equivalent | `IndexOf(string)`, `TryGetValue(string, out object?)` and `GetAt(int)` |

A call that passed a `string` key, which is every key the package itself uses,
compiles unchanged. A key of any other type no longer compiles. The most common
changes are these:

```csharp
// Before
ICsvFileLine line = reader.ReadLine();
foreach (DictionaryEntry entry in line)
{
    Console.WriteLine($"{entry.Key} = {entry.Value}");
}

var fieldNames = line.Keys.Cast<string>().ToList();
var name = line["name"] as string;

// After
ICsvFileLine line = reader.ReadLine();
foreach (var field in line)
{
    Console.WriteLine($"{field.Key} = {field.Value}");
}

var fieldNames = line.Keys;   // already an IReadOnlyList<string>
var name = line["name"] as string;   // unchanged: a value is still an object?
```

A line is no longer an `IDictionary`, so it can't be passed to an API that takes
one. Copy its fields into the collection that API needs, for example with
`line.ToDictionary(field => field.Key, field => field.Value)`.

`CsvFileLineBase` still implements the members explicitly, apart from `Add`, so
call them through an `ICsvFileLine` reference as before. On every target, a line
now behaves as the generic `OrderedDictionary<string, object?>` does: replacing the
value of a field that is present no longer invalidates an enumerator of the line on
the `netstandard1.1`, `netstandard2.0` and `netstandard2.1` builds, as it already
didn't on the `net10.0` build.

## File.CSV mapper registration

In version 1.x, a mapper registered with `RegisterMapper` on one reader or writer
was registered for the whole process: every other reader and writer of the same
record line type used it too, until any of them unregistered it. In version 2.0, a
mapper registered with `RegisterMapper` applies only to the reader or writer you
register it on. Register it on each reader and writer that should use it.
A reader or writer has at most one registered mapper: registering a mapper of
another type now replaces it, where version 1.x kept the first one, and
`UnregisterMapper<TMapper>` removes the registered mapper only if it is a
`TMapper`.

```csharp
// Before: registered once, used by every reader and writer of MyRecordLine
using (var csvReader = new CsvFileReader<MyRecordLine>(inputReader))
using (var csvWriter = new CsvFileWriter<MyRecordLine>(outputWriter))
{
    csvReader.RegisterMapper<MyRecordLineMapper>();
    // ...
}

// After: registered on each reader and writer
using (var csvReader = new CsvFileReader<MyRecordLine>(inputReader))
using (var csvWriter = new CsvFileWriter<MyRecordLine>(outputWriter))
{
    csvReader.RegisterMapper<MyRecordLineMapper>();
    csvWriter.RegisterMapper<MyRecordLineMapper>();
    // ...
}
```

`CsvFileReader<TRecordLine>.Context.Mappers` holds only the mapper registered on
that reader, keyed by its record line type, instead of the mappers registered for
every type in the process.

Mappers built from `[CsvFieldMapping]` attributes are still shared by every reader
and writer, in the new `CsvMapperCache` class, which also holds the other details
the package works out once per record line type. Call `CsvMapperCache.Clear()` to
empty that cache, for example before unloading a collectible
`AssemblyLoadContext` that defines your record line types.

`CsvFileIoBase<TRecordLine>`, the base of `CsvFileReader<TRecordLine>` and
`CsvFileWriter<TRecordLine>`, no longer derives from `CsvFileCacheRepositoryBase`,
and can no longer be derived from outside the package. The members below were
`protected`, so only a class derived from one of these types could use them. They
are removed, with no replacement:

| Removed |
| --- |
| `CsvFileCacheRepositoryBase`, with its `DeclarativeMapperCache`, `ImperativeMapperCache` and `StandardLineBreaks` fields and its `GetTypeConverter` methods |
| `CsvFileIoBase<TRecordLine>.BuildException` |
| `CsvFileIoBase<TRecordLine>.DeserializeCsvLineFieldValue<T>` |
| `CsvFileIoBase<TRecordLine>.DoesFieldValueRequireQuoting` |
| `CsvFileIoBase<TRecordLine>.GetCsvLineMappedFieldValue` |
| `CsvFileIoBase<TRecordLine>.GetMapper` |
| `CsvFileIoBase<TRecordLine>.NormalizeFieldValueLineBreaks` |
| `CsvFileIoBase<TRecordLine>.SerializeCsvLineFieldValue` |

To clear the mapper caches, which version 1.x let a derived class do through the
`DeclarativeMapperCache` and `ImperativeMapperCache` fields, call
`CsvMapperCache.Clear()`, or unregister the mapper from the reader or writer it is
registered on.

## File.CSV conversion of mapped properties

In version 1.x, a reader converted a field to a mapped property only with a
converter you configured: a `CsvFieldConvertFromString` delegate, or a
`TypeConverter` from `ConvertUsing` or a `[TypeConverter]` attribute. A property
that isn't a `string`, with no converter, silently got the default value of its
type, such as `0` for an `int`, whatever the field held.

In version 2.0, a reader with no converter configured parses the field itself, for
the numeric, date, time, `bool`, `Guid` and enum types, their `Nullable<T>` forms,
any other type with a `TypeConverter` that converts from a string, and, on the
`net10.0` build, any type that implements `ISpanParsable<T>` or `IParsable<T>`. See
[converting field values](../packages/file-csv/concepts/field-mapping.md#converting-field-values)
for the details. A converter you configure still takes precedence.

Two things change for code that reads mapped lines:

- **A property with no converter now gets the field's value.** A property that
  read as its default value now reads as the value in the file. An empty field
  still gives the default value.
- **A value that fails to convert is now a bad value.** A field the built-in parse
  rejects, or that a `TypeConverter` throws for, goes to
  `CsvFileOptions.BadValueHandler` if one is set. Otherwise the reader throws a
  `CsvFileException`, with the property, the field value and, when the mapping
  names one, the field name in its `Data`. In version 1.x, the value was silently
  replaced by the default value when there was no converter, and the
  `TypeConverter`'s own exception propagated when there was one. That exception is
  now the `InnerException` of the `CsvFileException`.

A `TypeConverter` that returns `null` for a value type, or a value of another type,
also makes the value a bad value, where version 1.x threw a
`NullReferenceException` or an `InvalidCastException`.

You can remove `Int32Converter`, `DecimalConverter`, `DateTimeConverter`,
`EnumConverter` and the like from your mappings. The built-in parse uses
`CsvFileOptions.Culture`, or the invariant culture when that isn't set, as the
converters did. It doesn't accept everything those converters did, though: for
example, `Int32Converter` accepts a hexadecimal value such as `0x1F`, and
`EnumConverter` ignores case. Keep the converter where your files rely on that.
A `double` or `float` field that is too large for the type is now a bad value, and
infinity and NaN are accepted only as the culture's symbols, written in the same
case, on every target.

```csharp
// Before
this.Map()
    .Property(x => x.Id)
    .HasFieldName("person_id")
    .ConvertUsing(typeof(Int32Converter));

// After
this.Map()
    .Property(x => x.Id)
    .HasFieldName("person_id");
```

The `FieldIndex` of the `CsvFieldContext` that a validator and the bad-value
handler get for a mapped field is now the field's position in the line. In
version 1.x it was the field's position in a list of the mapper's field names
followed by the line's other field names, which differs from the line when the
header puts the fields in another order, or puts an unmapped field first.

## File.CSV invalid last field

When `CsvFileOptions.BadValueHandler` is set and a field breaks the quoting rules
of RFC 4180 section 2, such as `x"b"` or `"b"c`, a reader calls the handler with
the field's raw text and then uses an empty value for the field.

In version 1.x, it did this for every field except the last one in the record,
which kept its raw text with the quotes removed: the record `a,x"b"` was read as
`a` and `xb`, where `x"b",a` was read as an empty value and `a`. A mapped property
bound to the last field got that text, and its converter could store it or throw
its own exception.

In version 2.0, the last field is read as an empty value too, as every other
field already was. The handler is still called with the same `RawFieldValue` and
`FieldIndex`, the same number of times and in the same order, and a reader with
no handler still throws a `CsvFileException`.

The text that version 1.x returned for the last field can't be recovered. The
handler's `RawFieldValue` holds only the text read up to each violation, such as
`x` for `a,x"b"`, not the whole field, and a reader with no handler throws without
returning the record. If your files have such fields and you need their contents,
correct the files before reading them.

The same applies to a header line read with `CsvFileHeaderHandling.Use`. An
invalid last field name is now an empty name, where version 1.x used its text, so
`Id,x"Name"` gives the field names `Id` and an empty name. If the header already
has an empty name, such as `,x"Name"`, the two names are duplicates and the reader
throws a `CsvFileException` for the duplicate field names.
