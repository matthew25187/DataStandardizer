---
title: VcardCard Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardCard Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A vCard: an open list of properties, with typed accessors that don't depend on the version the card was read from. See [The card object model](../card-model.md).

**Syntax**

```csharp
public sealed partial class VcardCard : IVcard40Card, IVcard30Card, IVcard21Card
```

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardCard()` | A card with no properties. It needs at least one `FN` to conform to RFC 6350. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Properties` | `IList<VcardContentLine>` | Every property in order, including unknown and `X-` properties, whether or not its value is valid. It can be changed. It doesn't hold `BEGIN`, `VERSION` or `END`. |
| `SourceVersion` | `VcardVersion?` | The version the card was read from. `null` if it was built in code, or read with no `VERSION`. |
| `Kind` | `VcardKind` | What the card represents. With no `KIND` it is `Individual`, as RFC 6350 §6.1.4 says. RFC 6473 adds `Application` and RFC 6869 adds `Device`. |
| `KindText` | `string?` | The value of `KIND` as written, which holds any value that RFC 6350 and its extensions don't define. |
| `ExtensionProperties` | `IReadOnlyList<VcardProperty<string>>` | The properties RFC 6350 and its extensions don't define. Each value is as it appears in the content line. |
| `Agents` | `IReadOnlyList<VcardAgent>` | The `RELATED` properties with `TYPE=agent`, each a card, a URI or text. See [VcardAgent](VcardAgent.md). |

### Properties with at most one instance

Each is `null` if the card has none with a valid value. The setters replace every instance, including alternatives with the same `ALTID`, in the place of the first.

| Property | Name | Value type | Setter |
| --- | --- | --- | --- |
| `Name` | `N` | `VcardName` | `SetName(value)`, `SetName(value, parameters)` |
| `Birthday` | `BDAY` | `VcardTypedValue` (date-and-or-time or text) | `SetBirthday(value)`, `SetBirthday(value, parameters)` |
| `Anniversary` | `ANNIVERSARY` | `VcardTypedValue` (date-and-or-time or text) | `SetAnniversary(value)`, `SetAnniversary(value, parameters)` |
| `Gender` | `GENDER` | `VcardGender` | `SetGender(value)`, `SetGender(value, parameters)` |
| `ProductId` | `PRODID` | `string` | `SetProductId(value)` |
| `Revision` | `REV` | `VcardTimestamp` | `SetRevision(value)` |
| `Uid` | `UID` | `VcardTypedValue` (URI or text) | `SetUid(value)` |
| `BirthPlace` | `BIRTHPLACE` (RFC 6474 §2.1) | `VcardTypedValue` (text or URI) | `SetBirthPlace(value)`, `SetBirthPlace(value, parameters)` |
| `DeathPlace` | `DEATHPLACE` (RFC 6474 §2.2) | `VcardTypedValue` (text or URI) | `SetDeathPlace(value)`, `SetDeathPlace(value, parameters)` |
| `DeathDate` | `DEATHDATE` (RFC 6474 §2.3) | `VcardTypedValue` (date-and-or-time or text) | `SetDeathDate(value)`, `SetDeathDate(value, parameters)` |
| `Created` | `CREATED` (RFC 9554 §3.1) | `VcardTimestamp` | `SetCreated(value)` |
| `DefaultLanguage` | `LANGUAGE` (RFC 9554 §3.3) | `Bcp47LanguageTag` | `SetDefaultLanguage(value)` |
| `Mailer` | `MAILER` (RFC 2426 §3.3.3) | `string` | `SetMailer(value)` |
| `Classification` | `CLASS` (RFC 2426 §3.7.1) | `string` | `SetClassification(value)` |
| `SourceName` | `NAME` (RFC 2426 §5) | `string` | `SetSourceName(value)` |
| `Profile` | `PROFILE` (RFC 2426 §5) | `string` | `SetProfile(value)` |

`SetKind` takes a `VcardKind`, or the text of a value that RFC 6350 and its extensions don't define. A value that isn't initialised throws `ArgumentException`, and leaves the card as it was.

A setter throws `InvalidOperationException` if an instance has `DERIVED=true`, since RFC 9554 §4.4 says a client "MUST NOT update the property". Remove it first to work the value out again. `SetName` also puts the components of RFC 9554 in the older components, as `Addresses.Add` does; see [The card object model](../card-model.md). `Name` leaves an instance with `PHONETIC` for last.

### Properties with any number of instances

Each is a [VcardPropertyList](VcardPropertyList.md), a live view of `Properties`.

| Property | Name | Value type |
| --- | --- | --- |
| `CalendarAccessUris` | `CAPURI` (RFC 2739 §2.3.3) | `Uri` |
| `Sources` | `SOURCE` | `Uri` |
| `Xml` | `XML` | `string` |
| `FormattedNames` | `FN` | `string` |
| `Nicknames` | `NICKNAME` | `IReadOnlyList<string>` |
| `Photos` | `PHOTO` | `Uri` |
| `Addresses` | `ADR` | `VcardAddress` |
| `Telephones` | `TEL` | `VcardTypedValue` (text or URI) |
| `Emails` | `EMAIL` | `string` |
| `InstantMessaging` | `IMPP` | `Uri` |
| `Languages` | `LANG` | `Bcp47LanguageTag` |
| `TimeZones` | `TZ` | `VcardTypedValue` (text, URI or UTC offset) |
| `GeoLocations` | `GEO` | `Uri` |
| `Titles` | `TITLE` | `string` |
| `Roles` | `ROLE` | `string` |
| `Logos` | `LOGO` | `Uri` |
| `Organizations` | `ORG` | `VcardOrganization` |
| `Members` | `MEMBER` | `Uri` |
| `Related` | `RELATED` | `VcardTypedValue` (URI or text) |
| `Categories` | `CATEGORIES` | `IReadOnlyList<string>` |
| `Notes` | `NOTE` | `string` |
| `Sounds` | `SOUND` | `Uri` |
| `ClientPidMaps` | `CLIENTPIDMAP` | `VcardClientPidMap` |
| `Urls` | `URL` | `Uri` |
| `Keys` | `KEY` | `VcardTypedValue` (URI or text) |
| `FreeBusyUrls` | `FBURL` | `Uri` |
| `CalendarAddressUris` | `CALADRURI` | `Uri` |
| `CalendarUris` | `CALURI` | `Uri` |
| `Expertise` | `EXPERTISE` (RFC 6715 §2.1) | `string` |
| `Hobbies` | `HOBBY` (RFC 6715 §2.2) | `string` |
| `Interests` | `INTEREST` (RFC 6715 §2.3) | `string` |
| `OrganizationDirectories` | `ORG-DIRECTORY` (RFC 6715 §2.4) | `Uri` |
| `ContactUris` | `CONTACT-URI` (RFC 8605 §2.1) | `Uri` |
| `GrammaticalGenders` | `GRAMGENDER` (RFC 9554 §3.2) | `string`: `animate`, `common`, `feminine`, `inanimate`, `masculine`, `neuter`, or a token |
| `Pronouns` | `PRONOUNS` (RFC 9554 §3.4) | `string` |
| `SocialProfiles` | `SOCIALPROFILE` (RFC 9554 §3.5) | `VcardTypedValue` (URI or text) |

The typed members leave out a property whose value isn't valid, which `AllowInvalidValues` lets a reader accept, and one with a `CALSCALE` other than `gregorian`, which RFC 6350 §5.8 says to ignore. Both stay in `Properties`.

## Methods

| Method | Notes |
| --- | --- |
| `AddAgent(card)`, `AddAgent(uri)` | Adds a `RELATED` with `TYPE=agent`. A card is held as a copy, in the text of the property. |
| `RemoveAgents()` | Removes every `RELATED` with `TYPE=agent`, and gives how many there were. |
| `AsVersion21()` | The card as an [IVcard21Card](IVcard21Card.md). It is this object, so a change through either is a change to both. |
| `AsVersion30()` | The card as an [IVcard30Card](IVcard30Card.md). It is this object, so a change through either is a change to both. |
| `AsVersion40()` | The card as an [IVcard40Card](IVcard40Card.md). It is this object, so a change through either is a change to both. |
| `GetProperties(string name)` | The instances of a property, whether or not their values are valid. |
| `GetPreferred(string name)` | The first instance with the lowest `PREF`; an instance without it is least preferred (RFC 6350 §5.3). `null` if there are none. |
| `GetAlternatives(string name)` | The instances grouped by `ALTID`, in the order each group first appears (RFC 6350 §5.4). An instance without it is a group of its own. |
| `GetDuplicatePropertyIds()` | The instances that have the same valid `PROP-ID` as another of the same name, as groups. RFC 9554 §4.7 leaves what to do about them to the application, so `Validate()` doesn't report them. |
| `RemoveProperties(string name)` | Removes every instance, and gives how many. |
| `AddExtensionProperty(string name, string value)` | Adds a property that RFC 6350 and its extensions don't define. There is an overload with parameters. |
| `ResolveMembers(IEnumerable<VcardCard> cards)` | The cards of a set that the `MEMBER` properties link to by `UID`. One that doesn't resolve, such as a `mailto` URI, is left out. |
| `FindByUid(IEnumerable<VcardCard> cards, string uid)` | Static. The first card with a `UID`, compared as RFC 6350 §7.1.1 says. `null` if there is none. |
| `Validate()` | Every [VcardDiagnostic](VcardDiagnostic.md), without throwing for a problem in the card. An exception from a `CharsetResolver` of the caller's isn't caught. |
| `Validate(version)` | What `VcardWriter` checks for that `WriteVersion`, including what the version can't hold, as `Lossy` items. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [IVcard40Card](IVcard40Card.md)
- [VcardReader](VcardReader.md)
- [VcardWriter](VcardWriter.md)
- [The card object model](../card-model.md)
