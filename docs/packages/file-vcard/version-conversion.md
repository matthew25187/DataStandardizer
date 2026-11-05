---
title: Converting between versions
parent: File.vCard
grand_parent: Packages
nav_order: 7
---

# Converting between versions

A `VcardCard` holds each concept once, in a canonical form, whichever version it was read from. So converting a card to another version is a matter of writing it in that version. vCard 4.0, 3.0 and 2.1 can be read and written.

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

**Labels.** A `LABEL` goes with an `ADR` that has the same group, if it has no `TYPE` or the same `TYPE` values. Failing that, it goes with one with no group and the same `TYPE` values, where having none means RFC 2426's default of `intl,postal,parcel,work`. A label with a parameter the address doesn't have stays a property of its own, and so does one that matches no address.

**Dates in `REV`.** A `REV` of vCard 4.0 has a time, so RFC 2426's `REV:1997-11-15` becomes the first moment of that day in UTC, and is written back as `1997-11-15T00:00:00Z`.

**Media types.** A `TYPE` becomes a media type through [VcardLegacyMediaTypes](reference/VcardLegacyMediaTypes.md). RFC 2426 says the value is an image or audio format that IANA registers, or a non-standard one, so any other token is taken as a subtype: `TYPE=HEIC` on a `PHOTO` is `image/heic`, and is written back as `HEIC`. A binary value with no `TYPE` is `application/octet-stream`.

**Agents.** An `AGENT` holds a card as escaped text. `Agents` gives what each agent holds, with `Card` read from that text, so changing it doesn't change the property. `AddAgent` and `RemoveAgents` change them.

```csharp
foreach (var agent in card.Agents)
{
    Console.WriteLine(agent.Card?.FormattedNames[0].Value ?? agent.Uri?.ToString() ?? agent.Text);
}
```

RFC 2426 §4 says the value "MUST be a valid vCard object", so a nested card is held to the same rules as the card around it, under the same options, when it is read and when it is written. The RFC's own example in §3.5.4 has no `VERSION` or `N`, and has a parameter with no name, so reading that form takes `AllowMissingRequiredProperties` and `AcceptLegacySyntax`. With them, a nested card with no `VERSION` is taken to be vCard 3.0. Writing a nested card with no `N` or `FN` as vCard 3.0 takes `AllowMissingRequiredProperties`. A `\n` is a line break in the nested card whatever the options, since RFC 2426 §5 says it stands for a CRLF. Nesting is limited to 8 levels.

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

## How vCard 2.1 maps to the model

A vCard 2.1 value is decoded by its `ENCODING` and `CHARSET` as it is read (see [Legacy syntax and character sets](legacy-syntax.md)), so the model holds the text, not the encoding. vCard 2.1 escapes nothing but `\;` in a compound value, so text gains the escaping of RFC 6350 §3.4.

| vCard 2.1 | Canonical form (vCard 4.0) |
| --- | --- |
| `TEL;WORK;VOICE:` or `TEL;TYPE=WORK;TYPE=VOICE:` | `TEL;TYPE=work,voice:`, each `knowntype` in lower case |
| `PREF` | `PREF=1`, on a property that has `PREF` in vCard 4.0, and otherwise `TYPE=pref` |
| `NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:Caf=C3=A9=0D=0Aa,b` | `NOTE:Café\na\,b` |
| `PHOTO;ENCODING=BASE64;TYPE=GIF:` and the lines up to the empty line, and the same for `LOGO`, `SOUND` and `KEY` | `PHOTO:data:image/gif;base64\,…` |
| `PHOTO;VALUE=URL;TYPE=GIF:…` | `PHOTO;MEDIATYPE=image/gif:…` |
| `SOUND;VALUE=CONTENT-ID:<a@host>` | `SOUND:cid:a@host`, the `cid` URI of RFC 2392 |
| `SOUND:JON Q PUBLIK`, and a `KEY` in clear text | `SOUND;VALUE=text:JON Q PUBLIK`, `KEY;VALUE=text;TYPE=pgp:…` |
| `AGENT:` followed by a card, or `AGENT:BEGIN:VCARD` | `RELATED;VALUE=text;TYPE=agent`, holding the card as text |
| `AGENT;VALUE=URL:…` | `RELATED;TYPE=agent:…` |
| `LABEL` | The `LABEL` parameter of the `ADR` it goes with, as for vCard 3.0 |
| `GEO:37.24,-17.87` | `GEO:geo:37.24\,-17.87` |
| `TZ:-0500` | `TZ;VALUE=utc-offset:-0500` |
| `BDAY:1995-04-15` | `BDAY:19950415` |
| `REV:1995-10-31T22:27:10Z` | `REV:19951031T222710Z` |
| `UID:` text | `UID;VALUE=text:`, unless it is a URI |
| `N` and `ADR` with fewer components | All their components |
| `MAILER`, and an `X-` property | Kept as they are |

**Media types.** A `knowntype` becomes a media type through [VcardLegacyMediaTypes](reference/VcardLegacyMediaTypes.md). A format with no media type, such as `PICT`, stays a `TYPE`, with `application/octet-stream` data. Quoted-printable is how vCard 2.1 writes text, so a `PHOTO`, `LOGO`, `SOUND` or `KEY` in quoted-printable is binary only where a `TYPE` names its format (§2.1.9), such as `GIF` or `PICT`, and not where it says what it is for, such as `WORK`.

**Agents.** The card of an `AGENT` follows its line, up to its own `END:VCARD` (§2.5.4). The example of §2.5.4 has it on the next line, and the grammar of §2.9 starts it on the `AGENT` line, as `AGENT:BEGIN:VCARD`. A reader takes both. The card is read under the options of the card around it, and is vCard 2.1 unless its `VERSION` says otherwise. Nesting is limited to 8 levels. A `BEGIN:VCARD` inside a card with no `AGENT` before it, as in the distribution list of §2.8.1, isn't allowed by the grammar, and throws `InvalidStructure`.

## Writing vCard 2.1

`WriteVersion = VcardVersion.V2_1` writes a card in the forms of vCard 2.1. For example, `N:Müller;Hans;;;` is written as

```
N;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:M=C3=BCller;Hans;;;
```

- **Quoted-printable.** A value with a character outside printable US-ASCII, such as a line break or `ü`, is written in quoted-printable, with `CHARSET=UTF-8` if it has a character outside US-ASCII.
- **No folds.** vCard 2.1 and the later versions unfold a line differently, so no fold is read the same way by every reader. A line longer than 75 characters is written in quoted-printable instead, with soft line breaks.
- **Binary.** Inline data is written in `BASE64`, on the lines after the property's own, with the empty line that ends it (§2.9).
- **Types.** Each `knowntype` is written alone, as `TEL;WORK;VOICE:`, which is the form of every example in the specification. `WriteNamedTypesInVersion21` writes `TEL;TYPE=WORK;TYPE=VOICE:`. A parameter of vCard 2.1 has one value, so each `TYPE` is a parameter of its own either way.
- **Agents.** A card held by `RELATED;TYPE=agent` is written after `AGENT:BEGIN:VCARD`, in vCard 2.1, ending with its `END:VCARD`.
- **Media.** A URL is `VALUE=URL`, and a `cid` URI is `VALUE=CONTENT-ID` with the Content-ID in angle brackets.

## What vCard 2.1 can't hold

As for vCard 3.0, writing a card as vCard 2.1 throws `VcardFileException` with a `Lossy` diagnostic for data that version can't hold, and `AllowLossyVersionConversion` leaves it out.

| Lost | Why |
| --- | --- |
| Every property vCard 2.1 doesn't define, such as `NICKNAME`, `CATEGORIES`, `PRODID`, `SORT-STRING` and the properties of RFC 2739 and RFC 4770, as well as those vCard 3.0 can't hold | vCard 2.1 §2.9 |
| A property whose name isn't one of vCard 2.1 and doesn't start with `X-` | vCard 2.1 §2.9 and §2.8.1 |
| A `TYPE` value that isn't a `knowntype` or an `X-` word, such as `text` on a `TEL` | vCard 2.1 §2.9 |
| A media type with no `knowntype`, such as `image/png`. The data is written without a `TYPE`. | vCard 2.1 §2.9 |
| `SORT-AS`, and an `X-` parameter with more than one value or a value that isn't a `word` | vCard 2.1 §2.9 |
| The order of a `PREF` that isn't the lowest of its property | vCard 2.1 §2.4.1 |
| A `BDAY` that isn't a complete date, including one with a time; a `TZ` that isn't an offset from UTC | vCard 2.1 §2.2.4, §2.4.5 |
| Several values in a component of `N` or `ADR`, which are joined with a space, and a component that ends with a backslash, which would escape the `;` after it | vCard 2.1 §2.9 |
| A language tag that isn't one of RFC 1766 | vCard 2.1 §2.1.7 |
| An `AGENT` that is text and not a card | vCard 2.1 §2.5.4 |

vCard 2.1 requires `N` and not `FN` (§2.2.1, §2.2.2), so a card with no `N` throws with `MissingRequiredProperty` unless `AllowMissingRequiredProperties` is set, and so does the card of an agent.

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

## The shape of vCard 2.1

`AsVersion21()` gives the card as an [IVcard21Card](reference/IVcard21Card.md), which has only what vCard 2.1 defines, in its shape: bare `TYPE` tokens, inline data as a `knowntype` and its octets, and `Agent` as the card it holds.

```csharp
IVcard21Card version21 = card.AsVersion21();
version21.Telephones.Add("+1-555-0100", new Vcard21PropertyParameters { IsPreferred = true, Types = { "CELL" } });
IVcard21Card agent = version21.Agent;
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

A vCard 2.1 card is held to the specification of 1996. `VERSION` may be anywhere in the card, as it is one of the items of §2.9, and `BEGIN` and `END` have no group.

| Rule | Reference | Accepted with |
| --- | --- | --- |
| An `N` | §2.2.2 | `AllowMissingRequiredProperties` |
| Only the properties of §2.9, and those whose names start with `X-` | §2.9, §2.8.1 | `AllowInvalidValues` |
| `TYPE`, `VALUE`, `ENCODING`, `CHARSET`, `LANGUAGE` and `X-` parameters, each with one value of those §2.9 gives, and `LANGUAGE` by RFC 1766 | §2.9, §2.1.7 | `AllowInvalidValues` |
| The value of each property, such as a complete date in `BDAY` and a date and a time in `REV`, and a picture that is binary or elsewhere | §2.2 to §2.7 | `AllowInvalidValues` |
| 7-bit values, unless `ENCODING` says otherwise | §2.1.5 | `AcceptEightBitWithoutEncoding` |

Parameters with no name, quoted-printable, `BASE64` and `CHARSET` are the forms of vCard 2.1, so they need no option there. Many writers of vCard 2.1 fold lines by the later rule, which `AcceptRfc6350FoldsInVersion21` reads.

## See also

- [The card object model](card-model.md)
- [Legacy syntax and character sets](legacy-syntax.md)
- [VcardVersionConverter](reference/VcardVersionConverter.md)
- [IVcard30Card](reference/IVcard30Card.md)
- [IVcard21Card](reference/IVcard21Card.md)
