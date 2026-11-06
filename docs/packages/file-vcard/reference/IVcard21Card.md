---
title: IVcard21Card Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard21Card Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

A card in the shape of vCard 2.1, the versit specification of 1996. Get one from `VcardCard.AsVersion21()`. See [Versions and canonical forms](../concepts/versions-and-canonical-forms.md).

**Syntax**

```csharp
public interface IVcard21Card
```

`VcardCard` implements this explicitly, so a view is the same object, not a copy. The interface exposes only what vCard 2.1 defines, so code written against it is checked by the compiler against that version. It doesn't inherit from the interface of another version, and it doesn't expose `VcardCard.Properties`. A view hides what vCard 2.1 can't show.

`ENCODING` and `CHARSET` aren't in the view: a reader decodes a value by them, and a writer chooses them for a value.

## Lists

Each is an [IVcard21PropertyList](IVcard21PropertyList.md), a live view of the card.

| Property | Name | Value type | Notes |
| --- | --- | --- | --- |
| `FormattedNames` | `FN` | `string` | |
| `Photos`, `Logos` | `PHOTO`, `LOGO` | [VcardMediaValue](VcardMediaValue.md) | Inline data with the `knowntype` of its format, such as `GIF`, or a URL. A format with no media type, such as `PICT`, is held as a `TYPE`. Data of a format with no `knowntype`, such as PNG, has no token. Adding a token that isn't a format of §2.9 throws `ArgumentException`, and so does text. |
| `Addresses` | `ADR` | `VcardAddress` | Without the components of RFC 9554. One with several items in a component, a component that ends with a backslash, or `PHONETIC`, is hidden. |
| `Labels` | `LABEL` | `string` | Held on the `ADR` it goes with, where there is one. |
| `Telephones` | `TEL` | `string` | A `tel` URI gives its number, and another URI is hidden. |
| `Emails` | `EMAIL` | `string` | |
| `TimeZones` | `TZ` | `VcardUtcOffset` | An offset from UTC (§2.4.5). Text and a URI are hidden. |
| `GeoLocations` | `GEO` | `VcardGeo` | A latitude and a longitude. Anything else is hidden. |
| `Titles`, `Roles`, `Notes` | `TITLE`, `ROLE`, `NOTE` | `string` | |
| `Organizations` | `ORG` | `VcardOrganization` | One with a component that ends with a backslash is hidden. |
| `Sounds` | `SOUND` | [VcardMediaValue](VcardMediaValue.md) | Inline data, a URL, or text: "a string representation" of how the name is said (§2.6.3). |
| `Urls` | `URL` | `Uri` | |
| `Keys` | `KEY` | [VcardMediaValue](VcardMediaValue.md) | Inline data, a URL, or text, whose "default format is clear-text" (§2.7.1), with its `TYPE` such as `PGP`. |

## Single properties

Each is `null` if the card has none that vCard 2.1 can show.

| Property | Name | Type | Setter |
| --- | --- | --- | --- |
| `Name` | `N` | `IVcard21Property<VcardName>?` | `SetName(value)`, which throws `ArgumentException` for a name vCard 2.1 can't hold |
| `Birthday` | `BDAY` | `IVcard21Property<VcardDate>?`, a complete date | `SetBirthday(value)`, which throws `ArgumentException` for a date that isn't complete |
| `Mailer` | `MAILER` | `IVcard21Property<string>?` | `SetMailer(value)` |
| `Agent` | `AGENT` holding a card | `IVcard21Card?` | `SetAgent(card)`, whose argument must be a `VcardCard` |
| `AgentUri` | `AGENT` with `VALUE=URL` or `VALUE=CONTENT-ID` | `Uri?`, with a Content-ID as a `cid` URI (RFC 2392) | `SetAgent(uri)` |
| `Revision` | `REV` | `IVcard21Property<VcardTimestamp>?` | `SetRevision(value)` |
| `Uid` | `UID` | `IVcard21Property<string>?` | `SetUid(value)` |

## Other members

| Member | Notes |
| --- | --- |
| `ExtensionProperties` | The properties whose names start with `X-` (§2.8.1), in order, each with its value as it is held. |
| `AddExtensionProperty(name, value)` | Adds one. It throws `ArgumentException` for a name that doesn't start with `X-`, isn't a `word` of §2.9, or that a version defines. |
| `RemoveProperties(name)` | Removes every instance, by its name in vCard 2.1. `LABEL` also comes off the addresses that hold it, and `AGENT` removes the agents. |
| `Validate()` | As `VcardCard.Validate(VcardVersion.V2_1)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
