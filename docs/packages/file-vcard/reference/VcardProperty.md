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
| `Label` | `string?` | `LABEL` (§6.3.1). |
| `Level` | `VcardLevel?` | `LEVEL` (RFC 6715 §3.2), or `null` if there is none that is valid for the property. |
| `Index` | `long?` | `INDEX` (RFC 6715 §3.1), or `null` if there is no valid one or the property doesn't allow it. See `VcardPropertyList<TValue>.GetOrderedByIndex()`. |
| `CountryCode` | `Iso3166Part1Alpha2Country?` | `CC` (RFC 8605 §3.1), or `null` if there is no valid one or the property doesn't allow it. |
| `ExtensionParameters` | `IReadOnlyList<VcardContentLineParameter>` | The parameters RFC 6350 and its extensions don't define. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardCard](VcardCard.md)
- [IVcard40Property](IVcard40Property.md)
- [VcardPropertyList](VcardPropertyList.md)
