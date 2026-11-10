---
title: xCard (XML)
parent: File.vCard
grand_parent: Packages
nav_order: 9
---

# Read and write xCard

xCard ([RFC 6351](https://www.rfc-editor.org/rfc/rfc6351)) is the XML form of
vCard 4.0. `XcardReader` and `XcardWriter` read and write it into the same card
object model (`VcardCard`) that `VcardReader` and `VcardWriter` use for `.vcf`
files, and that `JcardReader` and `JcardWriter` use for jCard. So a card read from
any of the three formats can be written in the others.

xCard uses the XML reader and writer of .NET, so it needs no extra package and is
available on **every** target. On .NET Standard 1.1, `XcardReader` and
`XcardWriter` have no asynchronous members and no file-path constructors, as for
the other readers and writers of that build.

## Read an xCard document

```csharp
using DataStandardizer.File.vCard;

using var reader = new XcardReader("contacts.xml");
foreach (var card in reader.ReadCards())
{
    Console.WriteLine(card.FormattedNames[0].Value);
}
```

The reader takes a file path, a `Stream`, a `TextReader` or an `XmlReader`, each with
an optional `VcardOptions`. From a stream or a file it reads the encoding the XML
declaration names, or UTF-8 if there is none. `VcardOptions.Encoding` overrides it,
unless the data starts with a byte order mark.

The root of the document is a `vcards` element in the namespace
`urn:ietf:params:xml:ns:vcard-4.0`, holding a `vcard` element for each card. RFC 6351
§5 says the `vcards` element MUST be there even for one card, so a bare `vcard`
element, as XMPP sends one, isn't a document the reader takes. The reader reads one
card at a time, so a large document doesn't have to fit in memory.

An `XmlReader` may be part of a larger document. The reader starts at its current
node and stops at the end of the `vcards` element, leaving the `XmlReader` on that
element's end tag.

## Write an xCard document

```csharp
using var writer = new XcardWriter("contacts.xml");
writer.WriteCards(cards);
```

The writer takes a file path, a `Stream`, a `TextWriter` or an `XmlWriter`. The
output is UTF-8 without a byte order mark, indented by two spaces, with CRLF line
breaks: `VcardOptions.Encoding` and `VcardOptions.LineBreak` change those. The
`vcards` element starts with the first card and ends when the writer is disposed,
so dispose the writer before you read the output. A writer that writes no cards
writes nothing.

xCard represents vCard 4.0 alone. A card read from vCard 3.0 or 2.1 is written in
the form of vCard 4.0, as `VcardWriter` writes it with `WriteVersion` set to vCard
4.0. The card's `VERSION` isn't written, since the namespace plays its role (RFC
6351 §5.1), and the reader gives each card `VERSION:4.0`.

## Convert between `.vcf`, jCard and xCard

Read with one format and write with another:

```csharp
using (var reader = new VcardReader("contacts.vcf"))
using (var writer = new XcardWriter("contacts.xml"))
{
    writer.WriteCards(reader.ReadCards());
}
```

A round trip of a vCard 4.0 card keeps every property, parameter and value. A card
of vCard 3.0 or 2.1 comes back as vCard 4.0, in the canonical form it has in the
model. A round trip doesn't keep XML formatting, such as white space between
elements or namespace prefixes. A few things can come back spelled differently but
with the same meaning:

- **Names:** property and parameter names come back in upper case. A group comes back
  as it was.
- **TEXT escaping** is applied again, so `\N` comes back as `\n`, and a semicolon
  that was escaped in a value of one piece of text comes back unescaped.
- **BOOLEAN values** come back in upper case.
- **List parameters:** the values of `TYPE`, `PID` and `SORT-AS` come back as
  separate values, so `TYPE="work,voice"` and `TYPE=work;TYPE=voice` both come back
  as `TYPE=work,voice`.
- **The `VALUE` parameter** comes back after the property's other parameters. One
  that names the property's default type, such as `KEY;VALUE=uri`, doesn't come
  back, since xCard can't tell it apart from none. One that names a type RFC 6350
  doesn't define, such as `VALUE=x-mytype`, doesn't come back either, since xCard
  has no element for it.
- **`XML` properties** come back with the same element, attributes and text, though
  the markup may be spelled differently, such as double quotes around an attribute
  value.

## How vCard maps to xCard

| vCard | xCard |
| --- | --- |
| A card | A `vcard` element. `BEGIN`, `END` and `VERSION` aren't written. |
| A property | An element named for the property in lower case, holding an optional `parameters` element first, then the value elements. |
| A group | A `group` element whose `name` attribute holds the group: `CONTACT.FN` is `<group name="CONTACT"><fn>...</fn></group>`. |
| Parameters | An element for each parameter, named in lower case, holding a value element for each value: `<type><text>work</text><text>voice</text></type>`. A parameter that isn't recognised has `unknown` elements. |
| The `VALUE` parameter | Not written, since the value element gives the type. When reading, it is added if the type isn't the property's default. |
| A value whose type isn't known, such as `X-FOO` with no `VALUE` | An `unknown` element holding the raw text, so it converts without loss. |
| TEXT, URI and LANGUAGE-TAG | `text`, `uri` and `language-tag` elements. TEXT has no escaping of vCard, and a URI has none on its commas. |
| DATE, TIME, DATE-TIME, TIMESTAMP and UTC-OFFSET | `date`, `time`, `date-time`, `timestamp` and `utc-offset` elements, in the basic format of ISO 8601, as in `.vcf`, such as `--0203` and `-0500`. |
| DATE-AND-OR-TIME, such as `BDAY` | A `date`, `date-time` or `time` element. A time alone has no `T`: `BDAY:T1230` is `<bday><time>1230</time></bday>`. |
| BOOLEAN, INTEGER and FLOAT | `boolean` (`true` or `false`), `integer` and `float` elements. |
| NICKNAME and CATEGORIES | A `text` element for each item in the list. |
| N and ADR | An element for each component, such as `surname`, `given`, `additional`, `prefix` and `suffix`, repeated for each value of a component, and empty for a component with none. |
| ORG | A `text` element for each component. |
| GENDER | A `sex` element, which may be empty, and an optional `identity` element. |
| CLIENTPIDMAP | A `sourceid` element and a `uri` element. |
| `XML` | The element the value holds, copied as it is (RFC 6351 §6). |

RFC 9554 adds components to `N` and `ADR` and gives them no xCard form. The reader
and writer use these elements for them:

| Property | Components | Elements |
| --- | --- | --- |
| `N` | 6 and 7 | `surname2`, `generation` |
| `ADR` | 8 to 18 | `room`, `apartment`, `floor`, `streetnumber`, `streetname`, `building`, `block`, `subdistrict`, `district`, `landmark`, `direction` |

A value with the components of RFC 9554 has an element for each of them, so an `N`
has five or seven components and an `ADR` seven or eighteen.

When reading, the reader also:

- **Wraps a property in another namespace in an `XML` property,** as RFC 6351 §6
  says. A child of a `vcard` or `group` element in another namespace becomes an
  `XML` property holding the element, its attributes and its text.
- **Ignores what it doesn't recognise:** an element or attribute whose name it
  doesn't recognise, a comment and a processing instruction (RFC 6351 §5.1).
- **Takes XML white space around a value of `xsd:` type:** a BOOLEAN, INTEGER, FLOAT
  or URI may have white space around it, which is dropped, and a FLOAT may have an
  exponent, which is moved into the digits, so `1.5E3` becomes `1500`.

## Value types in xCard form

Each value type parses and formats its xCard form with `VcardValueFormat.Xml`, the
same way it does the `.vcf` form with `VcardValueFormat.Text`:

```csharp
var flag = VcardValue.TryParseBoolean("1", VcardValueFormat.Xml, out var value);   // true, true
var time = VcardDateAndOrTime.Parse("1230", VcardValueFormat.Xml);
var text = time.ToString(VcardValueFormat.Text);   // "T1230"
```

The xCard form of dates, times and UTC offsets is the basic format of vCard 4.0,
except that RFC 6351's pattern for a DATE has no year alone. So `1985` doesn't parse
in that form, and a date of a year alone throws `InvalidOperationException` when
formatted in it. The writer writes such a value in an `unknown` element, which reads
back the same when the type is the property's default, as for `BDAY:1985`. It refuses
one with a `VALUE` parameter, such as `X-A;VALUE=date:1985`, whose type would be
lost.

xCard represents vCard 4.0 alone, so a method that takes a `VcardVersion` throws
`ArgumentException` for `VcardValueFormat.Xml` with any other version. N, ADR, ORG,
GENDER and CLIENTPIDMAP have no single xCard string, since they are trees of
elements, so `VcardName`, `VcardAddress`, `VcardOrganization`, `VcardGender` and
`VcardClientPidMap` throw `NotSupportedException` for `VcardValueFormat.Xml`.

## Errors and compliance

The checks are on by default, as they are for `.vcf`:

- **Conformance:** each card is checked against RFC 6350 when it is read and before
  it is written. A card that breaks a rule throws `VcardFileException` unless an
  option, such as `AllowMissingRequiredProperties`, accepts that kind of problem. The
  reader can then go on to the next card.
- **Values not in the form xCard gives their type:** such a value is kept as it is
  and reported as `InvalidValue`, even where its text would be valid in `.vcf`. That
  covers a BOOLEAN of `TRUE`, a DATE of a year alone or in the extended format, a
  structured value that lacks the element of a component, and a parameter value in
  an element other than the one RFC 6351 gives the parameter, such as
  `<pref><text>1</text></pref>`. `AllowInvalidValues` accepts them.
- **Invalid values:** a value that isn't valid for its type is kept, and the check
  reports it unless `AllowInvalidValues` is set. When writing, such a value is
  written as it is in the element of its type, which reads back the same. A TEXT,
  URI, DATE-AND-OR-TIME or structured value that isn't valid is written in an
  `unknown` element instead, so that its escaping isn't changed.
- **The structure of xCard:** XML that isn't well-formed, or doesn't follow the
  structure of xCard, throws `VcardFileException`, with the line and position of the
  XML document in its `Data` under `LineNumber` and `LinePosition`. That includes:
  - a root other than `vcards` in the xCard namespace (RFC 6351 §5);
  - names that aren't in lower case (§5.1);
  - a `begin`, `end` or `version` property, or a `value` parameter, whose role the
    elements play;
  - a `parameters` element that isn't the property's first child, a property with no
    value element, or values of more than one type;
  - a `group` element with no name, or in another group;
  - the components of a structured value out of order;
  - a document type definition (DTD), even with an `XmlReader` that would parse one.

  After such an error, the reader can't continue.
- **What the writer refuses:** a card that xCard can't hold. That is a name or group
  that isn't letters, digits and hyphens, a `BEGIN`, `END` or `GROUP` property, whose
  names xCard gives to its own elements, a property with two `VALUE` parameters, a
  `VALUE` parameter on a DATE of a year alone, or a value with a character XML can't
  hold, such as U+FFFE. The writer checks and builds
  each card in full before it writes any of it.

## Asynchronous reading and writing

`ReadCardAsync`, `ReadCardsAsync`, `WriteCardAsync`, `WriteCardsAsync`, `FlushAsync`
and `DisposeAsync` work as they do on `VcardReader` and `VcardWriter`, on .NET
Standard 2.0 and later. They use the asynchronous members of the XML reader and
writer, so a reader or writer made from your own `XmlReader` or `XmlWriter` needs its
`Async` setting to be `true`. Neither does synchronous I/O on a stream or file it
opened.

## See also

- [The common model and the versioned interfaces](../concepts/common-model.md)
- [Versions and canonical forms](../concepts/versions-and-canonical-forms.md)
- [Read and write jCard](read-write-jcard.md)
- [XcardReader](../reference/XcardReader.md)
- [XcardWriter](../reference/XcardWriter.md)
