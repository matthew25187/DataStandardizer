---
title: jCard (JSON)
parent: File.vCard
grand_parent: Packages
nav_order: 8
---

# Read and write jCard

jCard ([RFC 7095](https://www.rfc-editor.org/rfc/rfc7095)) is the JSON form of
vCard 4.0. `JcardReader` and `JcardWriter` read and write it into the same card
object model (`VcardCard`) that `VcardReader` and `VcardWriter` use for `.vcf`
files. So a card read from either format can be written in the other.

jCard uses System.Text.Json, so `JcardReader` and `JcardWriter` are available on .NET
Standard 2.0 and later only, **not on .NET Standard 1.1**. On .NET 10,
System.Text.Json is part of the framework. On .NET Standard 2.0 and 2.1, the package
depends on the System.Text.Json NuGet package, which NuGet installs for you.

## Read a jCard document

```csharp
using DataStandardizer.File.vCard;

using var reader = new JcardReader("contacts.json");
foreach (var card in reader.ReadCards())
{
    Console.WriteLine(card.FormattedNames[0].Value);
}
```

The reader takes a file path or a `Stream`, each with an optional `VcardOptions`.
JSON is always UTF-8, so there's no `TextReader` overload. A byte order mark at the
start is skipped.

A jCard document is either one card, `["vcard", [...]]`, or an array of them,
`[["vcard", ...], ["vcard", ...]]`. The reader accepts both. It reads one card at a
time, so a large document doesn't have to fit in memory.

## Write a jCard document

```csharp
using var writer = new JcardWriter("contacts.json");
writer.WriteCards(cards);
```

The writer takes a file path or a `Stream`. The output is compact UTF-8 JSON without
a byte order mark.

- **One card** is written as its jCard object, `["vcard", [...]]`.
- **Two or more cards** are written as an array of jCard objects.

Until the writer knows which of the two it is writing, it holds the first card in
memory. It writes the first card when the second arrives or when the writer is
disposed, so dispose the writer, with `using` or `Dispose`, before you read the
output. `Flush` doesn't write a card that is still held. A writer that writes no
cards writes nothing.

jCard represents vCard 4.0 alone. A card read from vCard 3.0 or 2.1 is written in
the form of vCard 4.0, as `VcardWriter` writes it with `WriteVersion` set to vCard
4.0, and its `version` property is always `4.0`.

## Convert between `.vcf` and jCard

Read with one format and write with the other:

```csharp
using (var reader = new VcardReader("contacts.vcf"))
using (var writer = new JcardWriter("contacts.json"))
{
    writer.WriteCards(reader.ReadCards());
}
```

A round trip keeps every property, parameter and value. It doesn't keep JSON
formatting or the order of an object's keys. A few things can come back spelled
differently but with the same meaning:

- **Names:** property and parameter names come back in upper case, and a group in
  upper case, as RFC 7095 §4 and §3.3.1.2.1 recommend.
- **TEXT escaping** is applied again, so `\N` comes back as `\n`, and a semicolon
  that was escaped in a value of one piece of text comes back unescaped.
- **BOOLEAN values** come back in upper case.
- **INTEGER and FLOAT values** are JSON numbers, which have no plus sign or leading
  zeros, so `+7` comes back as `7`.
- **List parameters:** the values of `TYPE`, `PID` and `SORT-AS` come back as
  separate values, so `TYPE="work,voice"` and `TYPE=work;TYPE=voice` both come back
  as `TYPE=work,voice`.
- **The `VALUE` parameter** comes back after the property's other parameters. One
  that names the property's default type, such as `KEY;VALUE=uri`, doesn't come
  back, since jCard can't tell it apart from none.

## How vCard maps to jCard

| vCard | jCard |
| --- | --- |
| A card | An array of two elements: the string `"vcard"` and an array of its properties. `BEGIN` and `END` aren't properties. |
| `VERSION` | The first property, `["version", {}, "text", "4.0"]`. |
| A property | An array: the name in lower case, an object holding the parameters, the type identifier, such as `"date-and-or-time"`, and the value or values. |
| A group | The `group` parameter, in lower case: `CONTACT.FN` is `["fn", {"group": "contact"}, ...]`. |
| Parameters | An object whose keys are the parameter names in lower case. A parameter with more than one value is an array of strings. |
| The `VALUE` parameter | Not written, since the type identifier gives the type. When reading, it is added if the type isn't the property's default. |
| A value whose type isn't known, such as `X-FOO` with no `VALUE` | The type `unknown`, with the raw text as a string, so it converts without loss. |
| A `VALUE` that names a type RFC 6350 doesn't define, such as `x-mytype` | That type identifier in lower case, with the raw text as a string. When reading, `VALUE=x-mytype` is added back. |
| TEXT, URI and LANGUAGE-TAG | A string. TEXT has no escaping of vCard, and a URI has none on its commas. |
| DATE, TIME, DATE-TIME, DATE-AND-OR-TIME, TIMESTAMP and UTC-OFFSET | A string in the extended format of ISO 8601 where it applies, such as `"--02-03"`, `"2009-08-08T14:30:00-05:00"` and `"-05:00"`. |
| BOOLEAN | `true` or `false`. |
| INTEGER and FLOAT | A number. |
| NICKNAME and CATEGORIES | One value for each item in the list. |
| N, ADR, ORG, GENDER and CLIENTPIDMAP | An array with an element for each component, and an array for a component with more than one value, such as `["Perreault", "Simon", "", "", ["ing. jr", "M.Sc."]]`. A value of one component, such as `ORG:Viagenie`, is a single string. |

The number of components of a structured value is the number it has in the text
form. So an `N` without the components of RFC 9554 has five, and one with them has
seven.

When reading, the reader also accepts:

- **A string for any type:** a string as the value of any type, so that a value that
  doesn't follow its type reaches the conformance check rather than failing the read.
- **A structured value as one string:** a single string where a structured value is
  expected, which is its one component.
- **Numbers with decimals or exponents:** an INTEGER with a fraction or an exponent,
  and a FLOAT with an exponent. RFC 7095 §3.5.9 and §3.5.10 say they "MUST be
  eliminated" when the value becomes one of vCard, so `3.14` is read as the INTEGER
  `3`, and `2e3` as `2000`.

## Value types in jCard form

Each value type parses and formats its jCard form with `VcardValueFormat.Json`, the
same way it does the `.vcf` form with `VcardValueFormat.Text`:

```csharp
var anniversary = VcardDateAndOrTime.Parse("2009-08-08T14:30:00-05:00", VcardValueFormat.Json);
var text = anniversary.ToString(VcardValueFormat.Text);   // "20090808T143000-0500"
```

jCard represents vCard 4.0 alone, so a method that takes a `VcardVersion` throws
`ArgumentException` for `VcardValueFormat.Json` with any other version. N, ADR, ORG,
GENDER and CLIENTPIDMAP have no single jCard string, since they are JSON arrays, so
`VcardName`, `VcardAddress`, `VcardOrganization`, `VcardGender` and
`VcardClientPidMap` throw `NotSupportedException` for `VcardValueFormat.Json`.

`VcardValueFormat.Json` needs no JSON library, so the value types support it on every
target, .NET Standard 1.1 included. Only `JcardReader` and `JcardWriter` need .NET
Standard 2.0 or later.

## Errors and compliance

The checks are on by default, as they are for `.vcf`:

- **Conformance:** each card is checked against RFC 6350 when it is read and before
  it is written. A card that breaks a rule throws `VcardFileException` unless an
  option, such as `AllowMissingRequiredProperties`, accepts that kind of problem. The
  reader can then go on to the next card. RFC 7095's own examples put the type `date`
  on `BDAY` and `date-time` on `ANNIVERSARY`, which RFC 6350 §6.2.5 and §6.2.6 don't
  allow, so they need `AllowInvalidValues`.
- **Invalid values:** a value that isn't valid for its type is kept, and the check
  reports it unless `AllowInvalidValues` is set. When writing, such a value is
  written as a string with its own type identifier, which reads back the same. A
  TEXT, URI or structured value that isn't valid is written with the type `unknown`
  instead, so that its escaping isn't changed.
- **The structure of jCard:** JSON that isn't valid, or doesn't follow the structure
  of jCard, throws `VcardFileException`. That includes these rules of RFC 7095, each
  a MUST:
  - Property names, parameter names and type identifiers are in lower case (§3.3,
    §3.4).
  - `BEGIN` and `END` aren't properties (§3.2).
  - The parameters object has no `value` key (§3.4.1).
  - The `group` parameter is a string of letters, digits and hyphens (§7.1).

  A `version` other than `4.0` throws too, since jCard represents vCard 4.0 alone.
  The exception's `Data` holds:
  - `BytePosition`: the offset in bytes of the JSON value at fault.
  - `JsonPath`: where the value has been read, its path, such as `$[1][3][0]` for
    the name of the fourth property of a single card.
  - `LineNumber` and `LinePosition`: for JSON that isn't valid, the line and the
    byte position on that line.

  After such an error, the reader can't continue.
- **What the writer refuses:** a card that jCard can't hold. That is a name or group
  that isn't letters, digits and hyphens, a `BEGIN` or `END` property, two parameters
  of the same name on one property other than `TYPE`, `PID` or `SORT-AS` (a JSON
  object can't hold both), a `GROUP` parameter, which RFC 7095 §7.1 reserves for
  jCard, or a value with a lone surrogate. The writer checks and encodes each card in
  full before it writes any of it.

## Asynchronous reading and writing

`ReadCardAsync`, `ReadCardsAsync`, `WriteCardAsync`, `WriteCardsAsync`, `FlushAsync`
and `DisposeAsync` work as they do on `VcardReader` and `VcardWriter`.

- **Reading:** the reader works through the data in pooled blocks of bytes, and the
  asynchronous members fill each block with `Stream.ReadAsync`.
- **Writing:** the writer encodes each card into a pooled buffer, then the
  asynchronous members write it with `Stream.WriteAsync`.

Neither does any synchronous I/O. Cancellation is checked before each block is read,
and between cards when writing, so a canceled write never leaves part of a card.

## Limitations

- **Characters outside the Basic Multilingual Plane:** characters such as emoji are
  written as `\u` escape pairs, since System.Text.Json always escapes them. Other
  characters outside ASCII are written as they are.
- **No raw JSON view:** the reader always builds the card object model. There is no
  lower-level, allocation-free view of the JSON.

## See also

- [The card object model](../card-model.md)
- [Converting between versions](../version-conversion.md)
- [JcardReader](../reference/JcardReader.md)
- [JcardWriter](../reference/JcardWriter.md)
