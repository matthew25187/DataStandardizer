---
title: VcardProperty Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardProperty Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A property of a card with its value parsed: a typed view of one `VcardContentLine`. The members are version-agnostic, and an instance doesn't change.

**Syntax**

```csharp
public sealed class VcardProperty<TValue> : IVcard40Property<TValue>
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Line` | `VcardContentLine` | The underlying content line. |
| `Name` | `string` | The name, as written. |
| `Value` | `TValue` | The value. |
| `Group` | `string?` | The group, without its `.`. |
| `Pref` | `int?` | `PREF` (RFC 6350 §5.3), or `null` if there is no valid one, which makes it least preferred. |
| `Pid` | `IReadOnlyList<VcardPid>` | The `PID` values (§5.5) that are valid. |
| `AltId` | `string?` | `ALTID` (§5.4). |
| `Types` | `IReadOnlyList<string>` | The `TYPE` values (§5.6), as written. `TYPE=a;TYPE=b`, `TYPE=a,b` and `TYPE="a,b"` all give two. |
| `Language` | `Bcp47LanguageTag?` | `LANGUAGE` (§5.1), or `null` if there is no valid one. |
| `MediaType` | `MimeMediaType?` | `MEDIATYPE` (§5.7), or `null` if there is no valid one. |
| `CalendarScale` | `string?` | `CALSCALE` (§5.8). |
| `SortAs` | `IReadOnlyList<string>` | The `SORT-AS` strings (§5.9). |
| `GeoLocation` | `Uri?` | `GEO` (§5.10), or `null` if there is no valid one. |
| `TimeZone` | `string?` | `TZ` (§5.11). |
| `Label` | `string?` | `LABEL` (§6.3.1, RFC 9554 §4.5). |
| `Level` | `VcardLevel?` | `LEVEL` (RFC 6715 §3.2), or `null` if there is none that is valid for the property. |
| `Index` | `long?` | `INDEX` (RFC 6715 §3.1), or `null` if there is no valid one or the property doesn't allow it. See `VcardPropertyList<TValue>.GetOrderedByIndex()`. |
| `CountryCode` | `Iso3166Part1Alpha2Country?` | `CC` (RFC 8605 §3.1), or `null` if there is no valid one or the property doesn't allow it. |
| `Author` | `Uri?` | `AUTHOR` (RFC 9554 §4.1), or `null` if there is no valid one. |
| `AuthorName` | `string?` | `AUTHOR-NAME` (RFC 9554 §4.2). |
| `Created` | `VcardTimestamp?` | `CREATED` (RFC 9554 §4.3), or `null` if there is no valid one. |
| `IsDerived` | `bool` | `true` if `DERIVED` (RFC 9554 §4.4) is `true`. |
| `Phonetic` | `VcardPhoneticSystem?` | `PHONETIC` (RFC 9554 §4.6), or `null` if there is no valid one or the property doesn't allow it. |
| `PropertyId` | `string?` | `PROP-ID` (RFC 9554 §4.7), or `null` if there is no valid one. |
| `Script` | `string?` | `SCRIPT` (RFC 9554 §4.8), or `null` if there is no valid one. With `Phonetic`, a script subtag in `Language` is to be ignored in favour of this. |
| `ServiceType` | `string?` | `SERVICE-TYPE` (RFC 9554 §4.9), with its case as written, or `null` if the property doesn't allow it. Two names are equal if they match case-insensitively (§3.5). |
| `Username` | `string?` | `USERNAME` (RFC 9554 §4.10), with its case as written, or `null` if the property doesn't allow it or its value isn't a URI. |
| `ExtensionParameters` | `IReadOnlyList<VcardContentLineParameter>` | The parameters RFC 6350 and its extensions don't define. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
- [IVcard40Property](IVcard40Property.md)
- [VcardPropertyList](VcardPropertyList.md)
