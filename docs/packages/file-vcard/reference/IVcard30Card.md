---
title: IVcard30Card Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard30Card Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

A card in the shape of vCard 3.0 (RFC 2426, with RFC 2739 and RFC 4770). Get one from `VcardCard.AsVersion30()`. See [Versions and canonical forms](../concepts/versions-and-canonical-forms.md).

**Syntax**

```csharp
public interface IVcard30Card
```

`VcardCard` implements this explicitly, so a view is the same object, not a copy. The interface exposes only what vCard 3.0 defines, so code written against it is checked by the compiler against that version. It doesn't inherit from the interface of another version, and it doesn't expose `VcardCard.Properties`. A view hides what vCard 3.0 can't show.

## Lists

Each is an [IVcard30PropertyList](IVcard30PropertyList.md), a live view of the card.

| Property | Name | Value type | Notes |
| --- | --- | --- | --- |
| `FormattedNames` | `FN` | `string` | |
| `Nicknames` | `NICKNAME` | `IReadOnlyList<string>` | |
| `Photos`, `Logos`, `Sounds` | `PHOTO`, `LOGO`, `SOUND` | [VcardMediaValue](VcardMediaValue.md) | Inline data or a URI. Inline data of any length is here, and can be added. Adding a `TYPE` that can't name a media type throws too. |
| `Addresses` | `ADR` | `VcardAddress` | Without the components of RFC 9554. One with several items in a component, or with `PHONETIC`, is hidden. |
| `Labels` | `LABEL` | `string` | Held on the `ADR` it goes with, where there is one. |
| `Telephones` | `TEL` | `string` | A `tel` URI gives its number, and another URI is hidden. |
| `Emails` | `EMAIL` | `string` | |
| `TimeZones` | `TZ` | `VcardTypedValue` | A UTC offset or text. A URI is hidden. |
| `GeoLocations` | `GEO` | `VcardGeo` | A latitude and a longitude. Anything else is hidden. |
| `Titles`, `Roles`, `Notes` | `TITLE`, `ROLE`, `NOTE` | `string` | |
| `Organizations` | `ORG` | `VcardOrganization` | |
| `Categories` | `CATEGORIES` | `IReadOnlyList<string>` | |
| `Urls`, `Sources` | `URL`, `SOURCE` | `Uri` | |
| `Keys` | `KEY` | [VcardMediaValue](VcardMediaValue.md) | Inline data or text. A URI is hidden. Adding inline data whose `data:` URI has more than 65519 characters throws `ArgumentException`, since the canonical `KEY` holds a `Uri`. |
| `InstantMessaging` | `IMPP` (RFC 4770) | `Uri` | |
| `FreeBusyUrls`, `CalendarAddressUris`, `CalendarAccessUris`, `CalendarUris` | `FBURL`, `CALADRURI`, `CAPURI`, `CALURI` (RFC 2739) | `Uri` | |

## Single properties

Each is `null` if the card has none that vCard 3.0 can show.

| Property | Name | Type | Setter |
| --- | --- | --- | --- |
| `Name` | `N` | `IVcard30Property<VcardName>?` | `SetName(value)` |
| `Birthday` | `BDAY` | `IVcard30Property<VcardDateAndOrTime>?` | `SetBirthday(value)`, which throws `ArgumentException` for a value vCard 3.0 can't hold |
| `Mailer` | `MAILER` | `IVcard30Property<string>?` | `SetMailer(value)` |
| `Agent` | `AGENT` holding a card | `IVcard30Card?`, read from the text of the property | `SetAgent(card)`, whose argument must be a `VcardCard` |
| `AgentUri` | `AGENT` holding a URI | `Uri?` | `SetAgent(uri)` |
| `ProductId` | `PRODID` | `IVcard30Property<string>?` | `SetProductId(value)` |
| `Revision` | `REV` | `IVcard30Property<VcardTimestamp>?` | `SetRevision(value)` |
| `SortString` | `SORT-STRING` | `string?`, held on `N` or `ORG` | `SetSortString(value)` |
| `Uid` | `UID` | `IVcard30Property<string>?`, which is text in this version | `SetUid(value)` |
| `Classification` | `CLASS` | `IVcard30Property<string>?` | `SetClassification(value)` |
| `SourceName` | `NAME` | `IVcard30Property<string>?` | `SetSourceName(value)` |
| `Profile` | `PROFILE` | `IVcard30Property<string>?` | `SetProfile(value)` |

## Other members

| Member | Notes |
| --- | --- |
| `ExtensionProperties` | The properties no version defines, in order, each with its value as it appears in the content line. |
| `AddExtensionProperty(name, value)` | Adds one. It throws `ArgumentException` for a name a version defines. |
| `RemoveProperties(name)` | Removes every instance, by its name in vCard 3.0. `LABEL` and `SORT-STRING` also come off what holds them, and `AGENT` removes the agents. |
| `Validate()` | As `VcardCard.Validate(VcardVersion.V3_0)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
