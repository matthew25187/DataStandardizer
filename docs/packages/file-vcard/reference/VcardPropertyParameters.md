---
title: VcardPropertyParameters Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardPropertyParameters Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The group and parameters of a property being added to a card. Each one left at its default is left out. `VALUE` isn't here, since it comes from the value added.

**Syntax**

```csharp
public sealed class VcardPropertyParameters
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Group` | `string?` | The group, without its `.`. |
| `Pref` | `int?` | `PREF`. Setting anything outside 1 to 100 throws `ArgumentOutOfRangeException`. |
| `Pid` | `IList<VcardPid>` | `PID`. |
| `AltId` | `string?` | `ALTID`. |
| `Types` | `IList<string>` | `TYPE`. |
| `Language` | `Bcp47LanguageTag?` | `LANGUAGE`. |
| `MediaType` | `MimeMediaType?` | `MEDIATYPE`. |
| `CalendarScale` | `string?` | `CALSCALE`. |
| `SortAs` | `IList<string>` | `SORT-AS`. |
| `GeoLocation` | `Uri?` | `GEO`. |
| `TimeZone` | `string?` | `TZ`. |
| `Label` | `string?` | `LABEL`. |
| `Level` | `VcardLevel?` | `LEVEL`. Setting anything other than a member throws `ArgumentOutOfRangeException`. |
| `Index` | `long?` | `INDEX`. Setting anything that isn't positive throws `ArgumentOutOfRangeException`. |
| `CountryCode` | `Iso3166Part1Alpha2Country?` | `CC`, which only `ADR` allows. |
| `Author` | `Uri?` | `AUTHOR`. |
| `AuthorName` | `string?` | `AUTHOR-NAME`. |
| `Created` | `VcardTimestamp?` | `CREATED`. |
| `Derived` | `bool?` | `DERIVED`. |
| `Phonetic` | `VcardPhoneticSystem?` | `PHONETIC`, which only `N` and `ADR` allow. Setting anything other than a member throws `ArgumentOutOfRangeException`. |
| `PropertyId` | `string?` | `PROP-ID`. |
| `Script` | `string?` | `SCRIPT`. |
| `ServiceType` | `string?` | `SERVICE-TYPE`, which only `IMPP` and `SOCIALPROFILE` allow. It is written in quotes, so that its case stays significant. |
| `Username` | `string?` | `USERNAME`, which only `IMPP` and `SOCIALPROFILE` allow, with a URI. It is written in quotes, so that its case stays significant. |
| `AdditionalParameters` | `IList<VcardContentLineParameter>` | Parameters RFC 6350 and its extensions don't define. A `VALUE` here throws `ArgumentException` when added to a property they define, since it comes from the value. |

Apart from the range of `Pref`, `Level`, `Index` and `Phonetic`, nothing here is checked until the card is validated or written.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardPropertyList](VcardPropertyList.md)
- [VcardCard](VcardCard.md)
