---
title: VcardPropertyNames Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardPropertyNames Class

## Definition

Namespace: `DataStandardizer.File.vCard`

The names of the properties RFC 6350 §6 defines. Names are case-insensitive; these are in the upper case the RFC uses.

**Syntax**

```csharp
public static class VcardPropertyNames
```

## Fields

Each is a `const string`.

| Field | Value | Notes |
| --- | --- | --- |
| `Address` | `ADR` | RFC 6350 §6.3.1: a delivery address. |
| `Anniversary` | `ANNIVERSARY` | RFC 6350 §6.2.6: the date of marriage, or equivalent. |
| `Begin` | `BEGIN` | RFC 6350 §6.1.1: the start of a card.  A reader or writer deals with it, so it is not in `VcardCard.Properties`. |
| `Birthday` | `BDAY` | RFC 6350 §6.2.5: the date of birth. |
| `CalendarAddressUri` | `CALADRURI` | RFC 6350 §6.9.2: where to send a scheduling request. |
| `CalendarUri` | `CALURI` | RFC 6350 §6.9.3: a calendar. |
| `Categories` | `CATEGORIES` | RFC 6350 §6.7.1: application categories, also known as tags. |
| `ClientPidMap` | `CLIENTPIDMAP` | RFC 6350 §6.7.7: the global meaning of a PID source identifier. |
| `Email` | `EMAIL` | RFC 6350 §6.4.2: an electronic mail address. |
| `End` | `END` | RFC 6350 §6.1.2: the end of a card.  A reader or writer deals with it, so it is not in `VcardCard.Properties`. |
| `FormattedName` | `FN` | RFC 6350 §6.2.1: the formatted name. |
| `FreeBusyUrl` | `FBURL` | RFC 6350 §6.9.1: busy time. |
| `Gender` | `GENDER` | RFC 6350 §6.2.7: sex and gender identity. |
| `Geo` | `GEO` | RFC 6350 §6.5.2: a global position. |
| `InstantMessaging` | `IMPP` | RFC 6350 §6.4.3: instant messaging and presence. |
| `Key` | `KEY` | RFC 6350 §6.8.1: a public key or authentication certificate. |
| `Kind` | `KIND` | RFC 6350 §6.1.4: the kind of object the card represents. |
| `Language` | `LANG` | RFC 6350 §6.4.4: a language that may be used for contact. |
| `Logo` | `LOGO` | RFC 6350 §6.6.3: a logo. |
| `Member` | `MEMBER` | RFC 6350 §6.6.5: a member of the group the card represents. |
| `Name` | `N` | RFC 6350 §6.2.2: the components of the name. |
| `Nickname` | `NICKNAME` | RFC 6350 §6.2.3: nicknames. |
| `Note` | `NOTE` | RFC 6350 §6.7.2: a supplemental comment. |
| `Organization` | `ORG` | RFC 6350 §6.6.4: the organization name and units. |
| `Photo` | `PHOTO` | RFC 6350 §6.2.4: an image or photograph. |
| `ProductId` | `PRODID` | RFC 6350 §6.7.3: the product that created the card. |
| `Related` | `RELATED` | RFC 6350 §6.6.6: a relationship with another entity. |
| `Revision` | `REV` | RFC 6350 §6.7.4: the revision of the card. |
| `Role` | `ROLE` | RFC 6350 §6.6.2: a function or part played. |
| `Sound` | `SOUND` | RFC 6350 §6.7.5: a digital sound, often the pronunciation of the name. |
| `Source` | `SOURCE` | RFC 6350 §6.1.3: the source of the directory information. |
| `Telephone` | `TEL` | RFC 6350 §6.4.1: a telephone number. |
| `TimeZone` | `TZ` | RFC 6350 §6.5.1: a time zone. |
| `Title` | `TITLE` | RFC 6350 §6.6.1: a position or job. |
| `Uid` | `UID` | RFC 6350 §6.7.6: a globally unique identifier of the entity. |
| `Url` | `URL` | RFC 6350 §6.7.8: a uniform resource locator. |
| `Version` | `VERSION` | RFC 6350 §6.7.9: the version of the specification.  A reader or writer deals with it, so it is not in `VcardCard.Properties`. |
| `Xml` | `XML` | RFC 6350 §6.1.5: extended XML-encoded vCard data. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardParameterNames](VcardParameterNames.md)
- [VcardCard](VcardCard.md)
