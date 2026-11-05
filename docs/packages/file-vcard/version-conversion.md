---
title: Converting between versions
parent: File.vCard
grand_parent: Packages
nav_order: 7
---

# Converting between versions

A `VcardCard` holds each concept once, in a canonical form, whichever version it was read from. So converting a card to another version is a matter of writing it in that version. vCard 4.0 and 3.0 can be read and written so far. vCard 2.1 is still to come.

```csharp
var options = new VcardOptions { WriteVersion = VcardVersion.V3_0 };
using (var reader = new VcardReader("version40.vcf"))
using (var writer = new VcardWriter("version30.vcf", options))
{
    foreach (var card in reader.ReadCards())
    {
        writer.WriteCard(card);
    }
}
```

A stream can hold cards of different versions, and `SourceVersion` says which each was. `WriteVersion` is 4.0 by default, whatever a card was read from.

## How vCard 3.0 maps to the model

| vCard 3.0 | Canonical form (vCard 4.0) |
| --- | --- |
| `TYPE=pref` | `PREF=1` |
| `PHOTO;ENCODING=b;TYPE=JPEG:…`, and the same for `LOGO`, `SOUND` and `KEY` | `PHOTO:data:image/jpeg;base64\,…` |
| `PHOTO;VALUE=uri;TYPE=JPEG:…` | `PHOTO;MEDIATYPE=image/jpeg:…` |
| `LABEL` | The `LABEL` parameter of the `ADR` it goes with |
| `SORT-STRING` | `SORT-AS` on `N`, or else on `ORG` |
| `AGENT` holding a card, a URI or text | `RELATED;TYPE=agent`, with `VALUE=text` for a card or text |
| `GEO:37.386013;-122.082932` | `GEO:geo:37.386013\,-122.082932` |
| `TZ:-05:00` | `TZ;VALUE=utc-offset:-0500` |
| `BDAY:1996-04-15` | `BDAY:19960415` |
| `REV:1995-10-31T22:27:10Z` | `REV:19951031T222710Z` |
| `UID:` text | `UID;VALUE=text:`, unless it is a URI |
| A comma in a URI | Escaped with a backslash, as RFC 6350 §3.4 and its errata 3845 and 3846 say. vCard 3.0 has no such rule. |
| `N` and `ADR` with fewer components | All their components |
| `MAILER`, `CLASS`, `NAME`, `PROFILE`, `CAPURI` | Kept as they are, with `Mailer`, `Classification`, `SourceName`, `Profile` and `CalendarAccessUris` |

**Labels.** A `LABEL` goes with an `ADR` that has the same group. Failing that, it goes with one with no group and the same `TYPE` values, where having none means RFC 2426's default of `intl,postal,parcel,work`. A label with a parameter the address doesn't have stays a property of its own, and so does one that matches no address.

**Dates in `REV`.** A `REV` of vCard 4.0 has a time, so RFC 2426's `REV:1997-11-15` becomes the first moment of that day in UTC, and is written back as `1997-11-15T00:00:00Z`.

**Media types.** A `TYPE` becomes a media type through [VcardLegacyMediaTypes](reference/VcardLegacyMediaTypes.md). RFC 2426 says the value is an image or audio format that IANA registers, or a non-standard one, so any other token is taken as a subtype: `TYPE=HEIC` on a `PHOTO` is `image/heic`, and is written back as `HEIC`. A binary value with no `TYPE` is `application/octet-stream`.

**Agents.** An `AGENT` holds a card as escaped text. `Agents` gives what each agent holds, with `Card` read from that text, so changing it doesn't change the property. `AddAgent` and `RemoveAgents` change them.

```csharp
foreach (var agent in card.Agents)
{
    Console.WriteLine(agent.Card?.FormattedNames[0].Value ?? agent.Uri?.ToString() ?? agent.Text);
}
```

RFC 2426 §4 says the value "MUST be a valid vCard object", but its own example has no `VERSION` or `N`, and has a parameter with no name. So a nested card is read with those accepted, and one with no `VERSION` is taken to be of the version of the card around it.

**Language tags.** RFC 2426 cites RFC 1766, so a tag is one to eight letters per part. That holds `i-klingon` and not `es-419`, whose digits came with RFC 4646. The tag is kept as it was written. `Language` on a property gives it if `Bcp47LanguageTag` can hold it too, as it can a grandfathered tag such as `i-klingon`.

## What vCard 3.0 can't hold

Writing a card as vCard 3.0 throws `VcardFileException`, and writes nothing of that card, if it holds data that version can't hold. That is a `Lossy` diagnostic. `AllowLossyVersionConversion` leaves the data out instead.

| Lost | From |
| --- | --- |
| `KIND`, `GENDER`, `ANNIVERSARY`, `MEMBER`, `LANG`, `CLIENTPIDMAP`, `XML`, and `RELATED` other than an agent | RFC 6350 |
| The parameters `PID`, `ALTID`, `CALSCALE`, and `GEO` and `TZ` on `ADR` | RFC 6350 |
| The order of a `PREF` that isn't the lowest of its property, and a `PREF` on a property with no `TYPE` | RFC 6350 |
| A `BDAY` that is text or not a complete date, a `TZ` that is a URI, a `KEY` that is a URI, a `TEL` that isn't a `tel` URI, the parameters of a `tel` URI, and a `GEO` that isn't a latitude and a longitude | RFC 6350 |
| `BIRTHPLACE`, `DEATHPLACE`, `DEATHDATE` | RFC 6474 |
| `EXPERTISE`, `HOBBY`, `INTEREST`, `ORG-DIRECTORY`, `LEVEL`, `INDEX` | RFC 6715 |
| `CONTACT-URI`, `CC` | RFC 8605 |
| `CREATED`, `GRAMGENDER`, `LANGUAGE`, `PRONOUNS`, `SOCIALPROFILE`, and the parameters `AUTHOR`, `AUTHOR-NAME`, `CREATED`, `DERIVED`, `PHONETIC`, `PROP-ID`, `SCRIPT`, `SERVICE-TYPE` and `USERNAME` | RFC 9554 |
| The added components of `N` and `ADR`, once they are in the older components, a whole line with `PHONETIC`, and `TYPE=billing` and `TYPE=delivery` | RFC 9554 |
| A parameter that the type doesn't allow in RFC 2426 §4, such as `TYPE` on a `NOTE` | RFC 2426 |
| A language tag that isn't one of RFC 1766, and a double quote or a line break in a parameter | RFC 2426 |

`AllowInvalidValues` keeps a parameter that the type doesn't allow, rather than leaving it out.

`VcardVersionConverter.Analyze` lists what would be lost, without writing anything. `Convert` gives a copy of a card with only what a version can hold.

```csharp
foreach (var item in VcardVersionConverter.Analyze(card, VcardVersion.V3_0))
{
    Console.WriteLine(item);
}

var copy = VcardVersionConverter.Convert(card, VcardVersion.V3_0, new VcardOptions { AllowLossyVersionConversion = true });
```

vCard 3.0 requires `N` as well as `FN` (RFC 2426 §5), and vCard 4.0 doesn't. So a card with no `N` throws with `MissingRequiredProperty` when it is written as vCard 3.0, unless `AllowMissingRequiredProperties` is set. No name is made up from `FN`.

## What stays when writing vCard 4.0

Nothing is lost when writing vCard 4.0. A property vCard 4.0 doesn't define stays as a property nothing defines in that version: `MAILER`, `CLASS`, `NAME`, `PROFILE`, `CAPURI`, and a `LABEL` or `SORT-STRING` that couldn't be attached. `Analyze(card, VcardVersion.V4_0)` lists them as `VersionConversion` items. Those aren't problems, and they never stop a writer.

A card that is valid in vCard 3.0 may break a rule of vCard 4.0. For example, RFC 2426 allows two `BDAY` properties and RFC 6350 doesn't. Writing it as vCard 4.0 then throws as for any other card, and `Validate(VcardVersion.V4_0)` lists what to fix. A reader checks a vCard 3.0 card only by the rules of its own version.

## The shape of vCard 3.0

`AsVersion30()` gives the card as an [IVcard30Card](reference/IVcard30Card.md), which has only what vCard 3.0 defines, in its shape. It is the same object, not a copy, and it hides what vCard 3.0 can't show.

```csharp
IVcard30Card version30 = card.AsVersion30();
version30.Emails.Add("jane@example.com", new Vcard30PropertyParameters { IsPreferred = true, Types = { "internet" } });
VcardMediaValue photo = version30.Photos[0].Value;
byte[] data = photo.GetData();
```

## What a reader accepts

A vCard 3.0 card is held to RFC 2426 with its errata, RFC 2739 and RFC 4770. `VERSION` may be anywhere in the card, and `BEGIN`, `VERSION` and `END` may have a group, as RFC 2426 §4 allows.

| Rule | Reference | Accepted with |
| --- | --- | --- |
| An `N` and an `FN` | RFC 2426 §5 | `AllowMissingRequiredProperties` |
| The value and `VALUE` of each type | RFC 2426 §4 | `AllowInvalidValues` |
| The parameters of each type, such as none on `URL`, and `LANGUAGE` by RFC 1766 | RFC 2426 §4 | `AllowInvalidValues` |
| No quoted-printable, `CHARSET` or parameter with no name | RFC 2426 §5 | `AcceptLegacySyntax` |

Some software writes quoted-printable in vCard 3.0 cards, and `AcceptLegacySyntax` reads those. The value is decoded as the card is read, so a writer has no legacy forms to refuse.

A `TYPE` on `URL`, which some exports write, needs `AllowInvalidValues`. A `\:` in a URL isn't accepted by any option.

## See also

- [The card object model](card-model.md)
- [Legacy syntax and character sets](legacy-syntax.md)
- [VcardVersionConverter](reference/VcardVersionConverter.md)
- [IVcard30Card](reference/IVcard30Card.md)
