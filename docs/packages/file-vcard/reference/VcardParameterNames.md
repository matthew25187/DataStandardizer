---
title: VcardParameterNames Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardParameterNames Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The names of the parameters of vCard 4.0: those RFC 6350 defines, and those of the extensions RFC 6715, RFC 8605 and RFC 9554. Names are case-insensitive; these are in the upper case the RFC uses.

**Syntax**

```csharp
public static class VcardParameterNames
```

## Fields

Each is a `const string`.

| Field | Value | Notes |
| --- | --- | --- |
| `AlternativeId` | `ALTID` | RFC 6350 §5.4: tags instances of a property as alternative representations of the same logical property. |
| `Author` | `AUTHOR` | RFC 9554 §4.1: the author of a value, as a URI. |
| `AuthorName` | `AUTHOR-NAME` | RFC 9554 §4.2: the name of the author of a value. |
| `CalendarScale` | `CALSCALE` | RFC 6350 §5.8: the calendar system of a date or date-time. |
| `CountryCode` | `CC` | RFC 8605 §3.1: the ISO 3166 country code of the country name of an address. |
| `Created` | `CREATED` | RFC 9554 §4.3: when a property was created in a card. |
| `Derived` | `DERIVED` | RFC 9554 §4.4: that a value is derived from other properties of the card. |
| `Geo` | `GEO` | RFC 6350 §5.10: a global position specific to an address. |
| `Index` | `INDEX` | RFC 6715 §3.1: the position of a value among the instances of a property. |
| `Label` | `LABEL` | RFC 6350 §6.3.1, RFC 9554 §4.5: a delivery address label. |
| `Language` | `LANGUAGE` | RFC 6350 §5.1: the language of a value. |
| `Level` | `LEVEL` | RFC 6715 §3.2: the level of an expertise, hobby or interest. |
| `MediaType` | `MEDIATYPE` | RFC 6350 §5.7: a hint at the media type of the resource a URI identifies. |
| `Phonetic` | `PHONETIC` | RFC 9554 §4.6: that a value is the phonetic representation of another property of the same name, and in which system. |
| `Pid` | `PID` | RFC 6350 §5.5: identifies a specific instance of a property. |
| `Preference` | `PREF` | RFC 6350 §5.3: the level of preference, from 1 (most preferred) to 100. |
| `PropertyId` | `PROP-ID` | RFC 9554 §4.7: identifies a property among those of the same name. |
| `Script` | `SCRIPT` | RFC 9554 §4.8: the script a value is written in. |
| `ServiceType` | `SERVICE-TYPE` | RFC 9554 §4.9: the online service of a messaging or social media profile. |
| `SortAs` | `SORT-AS` | RFC 6350 §5.9: the strings to sort by. |
| `TimeZone` | `TZ` | RFC 6350 §5.11: a time zone specific to an address. |
| `Type` | `TYPE` | RFC 6350 §5.6: class characteristics of a property. |
| `Username` | `USERNAME` | RFC 9554 §4.10: the user of a messaging or social media service. |
| `Value` | `VALUE` | RFC 6350 §5.2: the value type. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardPropertyNames](VcardPropertyNames.md)
- [VcardContentLine](VcardContentLine.md)
