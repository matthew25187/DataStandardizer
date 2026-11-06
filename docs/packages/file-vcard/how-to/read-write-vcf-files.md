---
title: Read and write .vcf files
parent: File.vCard
grand_parent: Packages
nav_order: 1
---

# Read and write .vcf files

`VcardReader` reads `.vcf` data into cards, and `VcardWriter` writes them. A file
can hold any number of cards, of vCard 4.0, 3.0 or 2.1.

## Read a file

```csharp
using DataStandardizer.File.vCard;

using var reader = new VcardReader("contacts.vcf");
foreach (var card in reader.ReadCards())
{
    // FN is required from vCard 3.0, but a vCard 2.1 card can have N alone.
    var name = card.FormattedNames.GetPreferred()?.Value ?? "(no name)";
    var email = card.Emails.GetPreferred()?.Value;
    Console.WriteLine($"{name} <{email}>");
}
```

`ReadCards` reads one card at a time, as the sequence is enumerated, so a large
file isn't held in memory. To read one card, call `ReadCard`, which returns `null` at
the end of the data.

Each card is checked by the rules of its version once it has been read. A card that
breaks one throws `VcardFileException`. See [Errors](#errors).

### Constructors

| Source | Constructor | Notes |
| --- | --- | --- |
| A file path | `new VcardReader(path)` | Opens the file, and closes it when the reader is disposed. .NET Standard 2.0 or later. |
| A `Stream` | `new VcardReader(stream)` | Read as strict UTF-8. The stream is left open. |
| A `TextReader` | `new VcardReader(textReader)` | Already decoded. The text reader is left open. |

Each has an overload that takes a [VcardOptions](../reference/VcardOptions.md).

## The model at a glance

A [VcardCard](../reference/VcardCard.md) holds every property of the card, in order,
in `Properties`. Its typed members are views of that list:

| Member | Property | Value |
| --- | --- | --- |
| `FormattedNames` | `FN` | `string` |
| `Name` | `N` | `VcardName`, with `FamilyNames`, `GivenNames` and the other components |
| `Telephones` | `TEL` | `VcardTypedValue`: a `tel:` URI, or text |
| `Emails` | `EMAIL` | `string` |
| `Addresses` | `ADR` | `VcardAddress` |
| `Organizations` | `ORG` | `VcardOrganization` |
| `Birthday` | `BDAY` | `VcardTypedValue`: a `VcardDateAndOrTime`, or text |
| `Uid` | `UID` | `VcardTypedValue`: a URI, or text |
| `ExtensionProperties` | `X-` and unknown properties | `string`, as written |

A member for a property that can have many instances, such as `Emails`, is a
[VcardPropertyList](../reference/VcardPropertyList.md). Each item is a
[VcardProperty](../reference/VcardProperty.md) with its `Value`, its group and its
parameters, such as `Types`, `Pref` and `Language`. A member for a property that can
have one, such as `Name`, is a `VcardProperty`, or `null` if the card has none.

The model is the same whichever version the card was read from, and `SourceVersion`
says which that was. See
[The common model and the versioned interfaces](../concepts/common-model.md).

## Write a file

```csharp
using var writer = new VcardWriter("contacts.vcf");
writer.WriteCards(cards);
```

Each card is written as `BEGIN:VCARD`, a `VERSION` line, its properties in order, and
`END:VCARD`. The version is `VcardOptions.WriteVersion`, which is vCard 4.0 by
default, whatever the card was read from. See
[Convert between 4.0, 3.0 and 2.1](convert-versions.md).

The writer takes the same kinds of target as the reader: a file path, which it
creates or overwrites, a `Stream`, or a `TextWriter`. A stream is written as UTF-8
with no byte order mark, as RFC 6350 §3.1 requires.

### Line breaks and folding

Each line ends with CRLF, whatever the operating system, as RFC 6350 §3.2 requires.
A line longer than 75 octets of UTF-8 is folded, and a fold never splits a
character. `FoldLength`, `FoldWhitespace` and `LineBreak` on `VcardOptions` change
this, within what the RFC allows.

### Flush and dispose

A writer that opened its own file or stream writer flushes and closes it when it is
disposed. A `Stream` you passed in is left open. A `TextWriter` you passed in is
neither flushed nor closed, so call `Flush` first.

### Report progress

`WriteCards` takes an optional `IProgress<VcardWriteProgress>`, which is called after
each card with the totals of the writer so far, `CardsWritten` and `LinesWritten`.

```csharp
writer.WriteCards(cards, new Progress<VcardWriteProgress>(
    value => Console.WriteLine($"{value.CardsWritten} cards written")));
```

## Build a card in code

```csharp
var card = new VcardCard();
card.FormattedNames.Add("Jane Doe");
card.SetName(new VcardName(["Doe"], ["Jane"], [], [], []));
card.Emails.Add("jane@example.com", new VcardPropertyParameters { Types = { "work" } });
card.Telephones.Add(new VcardTypedValue(new Uri("tel:+1-555-555-0100")),
    new VcardPropertyParameters { Types = { "cell" }, Pref = 1 });

using var writer = new VcardWriter(Console.Out);
writer.WriteCard(card);
writer.Flush();
```

```text
BEGIN:VCARD
VERSION:4.0
FN:Jane Doe
N:Doe;Jane;;;
EMAIL;TYPE=work:jane@example.com
TEL;VALUE=uri;PREF=1;TYPE=cell:tel:+1-555-555-0100
END:VCARD
```

The typed members take values without any escaping, and the writer adds what the
format needs. `VALUE=uri` is added because a `TEL` is text by default (RFC 6350
§6.4.1).

A property with no typed member goes in through `Properties`, or
`AddExtensionProperty` for an `X-` property:

```csharp
card.AddExtensionProperty("X-ASSISTANT", "Tama");
```

### The writer refuses a card that breaks RFC 6350

Before it writes a card, the writer checks it as `Validate` does. A card with no
`FN`, for example, throws `VcardFileException` and nothing of it is written:

```csharp
var card = new VcardCard();
card.Emails.Add("jane@example.com");

foreach (var diagnostic in card.Validate())
{
    Console.WriteLine(diagnostic.Message); // RFC 6350 §6.2.1 requires at least one FN property.
}
```

`Validate` returns every problem without throwing, so you can check a card before
you write it. See [Standards compliance by default](../concepts/standards-compliance.md).

## Counters

| Member | Counts |
| --- | --- |
| `VcardReader.CardsRead` | The cards read so far, including any that failed a check. |
| `VcardReader.LinesRead` | The logical (unfolded) lines read so far. |
| `VcardReader.PhysicalLinesRead` | The physical lines read so far. |
| `VcardWriter.CardsWritten` | The cards written so far. |
| `VcardWriter.LinesWritten` | The logical lines written so far. |

## Errors

The reader and writer throw `VcardFileException` for a problem in the data or in a
card. `Data` says where it was found, under the keys in
`VcardFileException.DataItemName`:

```csharp
try
{
    var card = reader.ReadCard();
}
catch (VcardFileException exception)
{
    var cardIndex = exception.Data[VcardFileException.DataItemName.CardIndex];
    var kind = exception.Data[VcardFileException.DataItemName.DiagnosticKind];
    var line = exception.Data[VcardFileException.DataItemName.LineNumber];
    Console.WriteLine($"Card {cardIndex}, line {line}: {kind}: {exception.Message}");
}
```

| Problem | What happens next |
| --- | --- |
| A card breaks a rule that the options don't accept | The card has been read, so reading can go on with the next card. |
| Anything else: a line that breaks the content-line syntax, including a byte order mark or a bare line feed; a `BEGIN` or `END` that isn't paired; a version that can't be read; data that isn't valid UTF-8; or a line longer than `MaxLineLength` | The reader can't go on, and later reads throw `InvalidOperationException`. See [Read non-compliant cards](read-non-compliant-cards.md). |

A reader or writer runs one operation at a time. A call made while another is in
progress throws `InvalidOperationException`.

## Related

- [Read asynchronously](read-asynchronously.md)
- [Read with zero copies](read-zero-copy.md)
- [Read non-compliant cards](read-non-compliant-cards.md)
- [The common model and the versioned interfaces](../concepts/common-model.md)
- [VcardReader](../reference/VcardReader.md),
  [VcardWriter](../reference/VcardWriter.md),
  [VcardCard](../reference/VcardCard.md) and
  [VcardFileException](../reference/VcardFileException.md)
