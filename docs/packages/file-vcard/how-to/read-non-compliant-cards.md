---
title: Read non-compliant cards
parent: File.vCard
grand_parent: Packages
nav_order: 6
---

# Read non-compliant cards

By default, the readers reject data that breaks a MUST or MUST NOT rule of RFC 6350,
or of RFC 2426 or vCard 2.1 for a card of those versions, and the writers refuse to
write it. Many real files break a rule or two, especially exports from older phones
and address books. Each common kind of problem has its own option on `VcardOptions`
that accepts it. All of them are off by default.

There is no single "lenient" switch. Turn on only the options your data needs, so
that other problems are still reported. See
[Standards compliance by default](../concepts/standards-compliance.md) for why.

## Find out what is wrong

Read the file with the default options first. The `VcardFileException` names the
problem, and its `Data` says where it is:

```csharp
using DataStandardizer.File.vCard;

try
{
    using var reader = new VcardReader("contacts.vcf");
    var cards = reader.ReadCards().ToList();
}
catch (VcardFileException exception)
{
    Console.WriteLine(exception.Message);
    foreach (System.Collections.DictionaryEntry item in exception.Data)
    {
        Console.WriteLine($"  {item.Key} = {item.Value}");
    }
}
```

For a broken rule of a card, `Data` holds a `DiagnosticKind`, a
`VcardDiagnosticKind` that names the option that accepts it:

| `VcardDiagnosticKind` | Accepted with |
| --- | --- |
| `MissingRequiredProperty` | `AllowMissingRequiredProperties` |
| `CardinalityViolation` | `AllowCardinalityViolations` |
| `InvalidValue` | `AllowInvalidValues`, or `AcceptUnescapedCommasInUris` for a comma in a URI |
| `InvalidParameter` | `AllowInvalidValues` |
| `LegacySyntax` | `AcceptLegacySyntax` |
| `InvalidStructure` | `AllowVersionNotFirst`, for a `VERSION` that isn't first; otherwise never accepted |
| `UnsupportedVersion` | Never accepted |

`Data` also holds the `CardIndex`, counting from 0, the `LineNumber` and, where one
property is at fault, the `PropertyName`. After such an error the reader can go on
to the next card, unless it is `UnsupportedVersion` or a broken structure, such as a
`BEGIN` with no `END`.

A problem in the line syntax itself, such as a bare line feed, has no
`DiagnosticKind`. Its `Data` holds the `LineNumber` and `PhysicalLineNumber`, and
after it the reader can't go on.

## The options

| Option | Reading | Writing | Formats |
| --- | --- | --- | --- |
| `AcceptBareLineFeed` | Yes | No (see `LineBreak`) | `.vcf` |
| `AcceptMissingFinalLineBreak` | Yes | No | `.vcf` |
| `AcceptByteOrderMark` | Yes | No | `.vcf` |
| `AcceptCaretEncodingInAllVersions` | Yes | No | `.vcf` |
| `AcceptLegacySyntax` | Yes | No | `.vcf` |
| `AcceptRfc6350FoldsInVersion21` | Yes | No | `.vcf` |
| `AcceptEightBitWithoutEncoding` | Yes | No | `.vcf` |
| `AllowUnknownCharsetAsLatin1` | Yes | No | `.vcf` |
| `AcceptUnescapedCommasInUris` | Yes | No | `.vcf` |
| `AcceptOrgUriProperty` | Yes | No | All |
| `AllowVersionNotFirst` | Yes | No | `.vcf` |
| `AllowMissingRequiredProperties` | Yes | Yes | All |
| `AllowCardinalityViolations` | Yes | Yes | All |
| `AllowInvalidValues` | Yes | Yes | All |
| `MaxLineLength` | Yes | No | `.vcf` |

An option that applies to writing works the same way there: without it, the writer
refuses a card with that problem and writes nothing of it. So if you read with such
an option and then write what you read, give the writer the same option.

### AcceptBareLineFeed

RFC 6350 §3.2 requires a CRLF at the end of each line. Files written on Unix, or by
hand, often end their lines with a bare line feed (`\n`) instead. Without the
option:

```text
VcardFileException: Found a line feed without a carriage return.  RFC 6350 requires a CRLF line break.
    LineNumber = 1
    PhysicalLineNumber = 1
```

With `AcceptBareLineFeed`, a bare line feed ends a line, and also works in a fold. A
bare carriage return is never a line break. What a writer ends lines with is set by
`VcardOptions.LineBreak`, CRLF by default.

`AcceptMissingFinalLineBreak` accepts a file whose last line, usually `END:VCARD`, has
no line break at all.

### AcceptByteOrderMark

The `vcard` rule of RFC 6350 §3.3 starts with `BEGIN:VCARD`, so a byte order mark,
which some editors add when they save a file as UTF-8, isn't allowed:

```text
VcardFileException: Expected a property name at the start of the content line.
    LineNumber = 1
    PhysicalLineNumber = 1
```

`AcceptByteOrderMark` skips it. It never selects the encoding: the data is still read
as UTF-8.

### AllowMissingRequiredProperties

vCard 4.0 requires an `FN` (RFC 6350 §6.2.1), vCard 3.0 an `N` and an `FN`
(RFC 2426 §5), and vCard 2.1 an `N` (§2.2.2):

```text
VcardFileException: RFC 6350 §6.2.1 requires at least one FN property.
    DiagnosticKind = MissingRequiredProperty
    CardIndex = 0
    LineNumber = 1
    PropertyName = FN
```

The option also accepts a card with no `VERSION`, which is read as vCard 4.0 with a
`SourceVersion` of `null`.

### AllowCardinalityViolations

A property that RFC 6350 §6 allows once appears more than once:

```text
VcardFileException: vCard 4.0 allows at most one BDAY property.
    DiagnosticKind = CardinalityViolation
    CardIndex = 0
    LineNumber = 5
    PropertyName = BDAY
```

With the option, a typed member such as `Birthday` gives the first. Instances with
the same `ALTID` count as one, so they need no option.

### AllowInvalidValues

A value that doesn't follow the syntax of its value type, for example a date in the
extended format of ISO 8601, which vCard 4.0 doesn't use:

```text
VcardFileException: The value of BDAY is not a valid date-and-or-time.
    DiagnosticKind = InvalidValue
    CardIndex = 0
    LineNumber = 4
    PropertyName = BDAY
```

The option also accepts a parameter that the property doesn't allow, such as a
`TYPE` on the `URL` of a vCard 3.0 card, which some exports write. The value is kept
as written in `Properties`, and the typed members leave it out, so `Birthday` is
`null` for the card above.

### AcceptUnescapedCommasInUris

RFC 6350 §3.4 says a comma in a value "MUST be escaped", and its verified errata 3845
and 3846 apply that to URI values. So `GEO:geo:37.386013,-122.082932`, which many
writers produce, and which the RFC itself prints, isn't valid:

```text
VcardFileException: The value of GEO is not a valid uri.
    DiagnosticKind = InvalidValue
    CardIndex = 0
    LineNumber = 4
    PropertyName = GEO
```

With the option, the comma is escaped as the line is read, so the typed members give
the value, and a writer writes it escaped.

### AcceptLegacySyntax

Some software writes the forms of vCard 2.1, such as quoted-printable, in a vCard 3.0
or 4.0 card:

```text
VcardFileException: The line has a form of vCard 2.1 that RFC 2426 §5 removed: a parameter with no name, CHARSET, or an ENCODING other than b.
    DiagnosticKind = LegacySyntax
    CardIndex = 0
    LineNumber = 5
    PropertyName = NOTE
```

With the option, every line gets the forms of vCard 2.1, and a value in a vCard 3.0
card is decoded as it is read. See
[Handle legacy character sets](handle-legacy-character-sets.md#cards-that-break-the-rules).

### vCard 2.1 files

Exports in vCard 2.1 often break two rules of their own specification:

- **Folds.** vCard 2.1 §2.1.3 unfolds a line by removing only the line break, so the
  white space at the start of the next line stays. Many writers fold by the later
  rule instead, which removes it too. `AcceptRfc6350FoldsInVersion21` unfolds by
  that rule.
- **8-bit text.** A value with characters outside US-ASCII needs `ENCODING=8BIT` or
  quoted-printable (§2.1.5). `AcceptEightBitWithoutEncoding` accepts it without.

A file with raw 8-bit values in a character set other than UTF-8, or a `CHARSET`
that can't be resolved, needs more. See
[Handle legacy character sets](handle-legacy-character-sets.md).

### AllowVersionNotFirst

RFC 6350 §6.7.9 says `VERSION` comes straight after `BEGIN:VCARD`, which some
writers don't do:

```text
VcardFileException: RFC 6350 §6.7.9 requires the VERSION property immediately after BEGIN:VCARD.
    DiagnosticKind = InvalidStructure
    CardIndex = 0
    LineNumber = 3
    PropertyName = VERSION
```

vCard 3.0 and 2.1 allow `VERSION` anywhere in the card, so their cards need no option.

### AcceptOrgUriProperty

RFC 6715 as published names one property both `ORG-DIRECTORY` and `ORG-URI`. Its
errata 3340 and 3341, and the IANA registry, say `ORG-DIRECTORY`, so by default
`ORG-URI` is an unknown property, kept in `ExtensionProperties`. With the option it
is read as `ORG-DIRECTORY`, and written with that name.

## MaxLineLength

`MaxLineLength` isn't an accommodation but a limit. RFC 6350 sets no maximum line
length, so the default is `null`, meaning no limit. The reader keeps each unfolded
line in memory, so when you read data you don't trust, set a limit:

```csharp
var options = new VcardOptions { MaxLineLength = 100_000 };
```

A longer line throws `VcardFileException`:

```text
VcardFileException: The content line is longer than the maximum of 100 characters.
    LineNumber = 4
    PhysicalLineNumber = 4
```

The length is in characters of the unfolded line, not counting its line break. A
`PHOTO` with inline data can be long, so leave room for it. The value must be at
least 1, and the reader can't go on after this error.

## Problems no option accepts

- **`UnsupportedVersion`:** a `VERSION` other than 4.0, 3.0 or 2.1, such as
  `VERSION:5.0`. This is reported as soon as the `VERSION` line is read.
- **Broken structure:** a `BEGIN` without a matching `END`, a `BEGIN:VCARD` inside a
  card other than for an `AGENT` of vCard 2.1, or a line that doesn't follow the
  content-line syntax.
- **A `\:` in a URL**, which some older software writes. `AcceptLegacySyntax` accepts
  a backslash that RFC 6350 §3.4 doesn't define in text, but not in a URI.

## Combine options

Turn on each option you need:

```csharp
var options = new VcardOptions
{
    AcceptBareLineFeed = true,
    AcceptByteOrderMark = true,
    AcceptUnescapedCommasInUris = true,
    AllowMissingRequiredProperties = true,
    MaxLineLength = 100_000
};
```

On `net10.0`, `VcardOptions` is a record, so you can derive one set of options from
another with `with`. On the .NET Standard targets, set the properties.

## Skip the cards that break a rule

A problem in a card that has been read in full doesn't stop the reader. To keep the
cards that pass and report the rest, catch the exception for each card:

```csharp
using var reader = new VcardReader("contacts.vcf");
var cards = new List<VcardCard>();
while (true)
{
    try
    {
        var card = reader.ReadCard();
        if (card is null) break;
        cards.Add(card);
    }
    catch (VcardFileException exception)
        when (exception.Data.Contains(VcardFileException.DataItemName.DiagnosticKind))
    {
        Console.WriteLine($"Skipped card {exception.Data[VcardFileException.DataItemName.CardIndex]}: {exception.Message}");
    }
}
```

An exception with no `DiagnosticKind` is a problem in the data itself, which the
filter lets through. Some problems with a `DiagnosticKind` stop the reader too, such
as `UnsupportedVersion`, or an `InvalidStructure` for a `BEGIN` with no `END`. Then
the next read throws `InvalidOperationException`, which the loop doesn't catch.

## Check a card with Validate

`Validate()` on a card checks it against the same rules the readers and writers
enforce. It returns every problem as a `VcardDiagnostic`, whatever the options, and
throws nothing. Use it to see what a card read with accommodations still breaks, or
to check a card you built in code before you write it:

```csharp
var options = new VcardOptions { AllowMissingRequiredProperties = true, AllowInvalidValues = true };
using var reader = new VcardReader("contacts.vcf", options);
foreach (var card in reader.ReadCards())
{
    foreach (var diagnostic in card.Validate())
    {
        Console.WriteLine($"{diagnostic.Kind} at line {diagnostic.LineNumber}: {diagnostic.Message}");
    }
}
```

```text
MissingRequiredProperty at line 1: RFC 6350 §6.2.1 requires at least one FN property.
InvalidValue at line 4: The value of BDAY is not a valid date-and-or-time.
```

`Validate()` checks a card by the rules of vCard 4.0. `Validate(version)` checks it by
the rules of another version, as a writer of that version does, including what that
version can't hold.

## Related

- [Standards compliance by default](../concepts/standards-compliance.md)
- [Handle legacy character sets](handle-legacy-character-sets.md)
- [VcardOptions](../reference/VcardOptions.md),
  [VcardDiagnosticKind](../reference/VcardDiagnosticKind.md),
  [VcardDiagnostic](../reference/VcardDiagnostic.md) and
  [VcardFileException](../reference/VcardFileException.md)
