---
title: Standards compliance by default
parent: File.vCard
grand_parent: Packages
nav_order: 14
---

# Standards compliance by default

Every default in the package follows RFC 6350 and its extensions, with their errata
applied. A vCard 3.0 card follows RFC 2426 and a vCard 2.1 card the specification of
1996, each with the extensions that apply to it. Readers reject data that breaks a
MUST or MUST NOT rule, and writers refuse to write a card that breaks one. Real-world
cards often break the rules, so each kind of accommodation is a separate option that
you turn on by name.

All public types here are in the `DataStandardizer.File.vCard` namespace.

## What the defaults mean

With a default `VcardOptions`:

- **Readers reject non-compliant input.** `VcardReader`, `JcardReader` and
  `XcardReader` check each card by the rules of its version once they have read it.
  The first problem throws `VcardFileException`, and reading can then continue with
  the next card. A problem in the syntax of a line, or data that isn't valid UTF-8,
  throws too, and stops the reader, since where the next card starts is unknown.
- **Writers refuse non-compliant cards.** `VcardWriter`, `JcardWriter` and
  `XcardWriter` check each card before they write it, by the rules of the version
  they write. A card that breaks a rule throws `VcardFileException`, and nothing of
  that card is written. So does a card that holds data the version can't hold.
- **The input is strict UTF-8**, with no byte order mark, as RFC 6350 §3.1 requires.
- **Line breaks are CRLF**, as RFC 6350 §3.2 requires, on every operating system.

For example, a card with no `FN` is rejected by the reader with this diagnostic:

```text
RFC 6350 §6.2.1 requires at least one FN property.
```

and a `VcardWriter` refuses to write the same card for the same reason.

The exception's `Data` holds where the problem is, under the keys in
`VcardFileException.DataItemName`: `CardIndex`, `DiagnosticKind`, and, where they
apply, `LineNumber`, `PhysicalLineNumber`, `PropertyName` and `Group`. See
[VcardFileException](../reference/VcardFileException.md).

`VcardCard.Validate()` returns every problem in a card as a `VcardDiagnostic`,
without throwing, and `Validate(version)` checks a card by the rules of a version,
as a writer of that version does.

## One option for each accommodation

Each accommodation for common non-compliant data is a separate property of
`VcardOptions`. All of them default to `false`. There is no single "lenient mode"
switch, so you accept only the problems you expect and still catch the rest.

### Content lines

These apply to the content-line reader, so to every reader of `.vcf` data.

| Option | What it accepts | Rule it relaxes |
| --- | --- | --- |
| `AcceptBareLineFeed` | A bare line feed as a line break, both to end a line and in a fold. A bare carriage return is never a line break. | RFC 6350 §3.2 and §3.3: lines end with CRLF. |
| `AcceptMissingFinalLineBreak` | A last line with no line break. | RFC 6350 §3.3: the `contentline` rule ends with CRLF. |
| `AcceptByteOrderMark` | A byte order mark at the start of the data, which is skipped. It never selects the encoding. | RFC 6350 §3.3: the `vcard` rule starts with `BEGIN:VCARD`; RFC 3629 §6. |
| `AcceptCaretEncodingInAllVersions` | RFC 6868 caret encoding on every line, not just those of a vCard 4.0 card. | RFC 6868 updates RFC 6350 only. |
| `AcceptLegacySyntax` | The lexical forms of vCard 2.1 on every line: parameters with no name, quoted-printable, `BASE64`, `ENCODING` and `CHARSET`, and an escape sequence RFC 6350 §3.4 doesn't have, such as `\:`. | RFC 6350 §5 and Appendix A, RFC 2426 §5. |
| `AcceptRfc6350FoldsInVersion21` | A vCard 2.1 card folded by the later rule, which removes one white space character. | vCard 2.1 §2.1.3 keeps it. |
| `AcceptEightBitWithoutEncoding` | A value in a vCard 2.1 card with characters outside US-ASCII and no `ENCODING=8BIT`. | vCard 2.1 §2.1.5: 7-bit is the default. |
| `AllowUnknownCharsetAsLatin1` | A `CHARSET` that can't be resolved, decoded as ISO-8859-1. `GetDecodedText(out bool)` says it was. | A value must be decoded by its own character set. |

`Encoding` is an option of a different kind: setting it reads or writes the data in
another encoding, which relaxes RFC 6350 §3.1 on purpose. `MaxLineLength` limits the
length of a line, which RFC 6350 doesn't, and is recommended for untrusted data.

### Cards

These apply to `VcardReader`, `JcardReader` and `XcardReader`, and those that aren't
for reading only apply to the writers too.

| Option | What it accepts | Rule it relaxes |
| --- | --- | --- |
| `AllowVersionNotFirst` | A `VERSION` that isn't straight after `BEGIN:VCARD`. Reading only. | RFC 6350 §6.7.9. |
| `AllowMissingRequiredProperties` | A card with no `FN`, a vCard 3.0 card with no `N` or `FN`, a vCard 2.1 card with no `N`, or, reading, a card with no `VERSION`. Also a card whose `CONTACT-URI` properties have no `mailto`, `http` or `https` URI. | RFC 6350 §6.2.1 and §6.7.9, RFC 2426 §5, vCard 2.1 §2.2.2, RFC 8605 §2.1. |
| `AllowCardinalityViolations` | More instances of a property than it allows, or a `MEMBER` in a card that isn't `KIND:group`. | RFC 6350 §6 and §6.6.5. |
| `AllowInvalidValues` | A value that doesn't follow its syntax, a `VALUE` the property doesn't allow, a parameter the property doesn't allow, a `PREF` outside 1 to 100, a `PID` source with no `CLIENTPIDMAP`, and the parameter rules of the extensions. The line is kept as written. A value that doesn't follow its syntax is left out of the typed members. A value that does stays in its typed member when only a parameter breaks a rule, and the typed parameter is then unset: with `PREF=101`, `Pref` is `null`. | RFC 6350 §4, §5 and §6, and the rules of each extension. |
| `AcceptUnescapedCommasInUris` | A comma with no backslash in a URI value of a vCard 4.0 card, which is escaped as the line is read. Reading only. | RFC 6350 §3.4 and its errata 3845 and 3846. |
| `AcceptOrgUriProperty` | A property named `ORG-URI`, read as `ORG-DIRECTORY`. Reading only. | RFC 6715 §2.4, and its errata 3340 and 3341. |
| `AllowLossyVersionConversion` | Writing a card that holds data the version written can't hold, which is left out. Writing only. | RFC 2426 and vCard 2.1 can't hold it. |

The same options apply to reading and writing:

```csharp
var options = new VcardOptions
{
    AcceptByteOrderMark = true,
    AllowMissingRequiredProperties = true
};

using var reader = new VcardReader("contacts.vcf", options);
```

The rules each option relaxes are listed in full below for vCard 4.0, and in
[Versions and canonical forms](versions-and-canonical-forms.md#what-a-reader-accepts)
for vCard 3.0 and 2.1. Turning an option on doesn't make the writer write
non-compliant output where it doesn't have to: a value read with
`AcceptUnescapedCommasInUris` is written escaped, and a quoted-printable value in a
vCard 3.0 card read with `AcceptLegacySyntax` is decoded as it is read, so it is
written without the legacy forms.

## The rules of vCard 4.0

| Rule | Reference | Accepted with |
| --- | --- | --- |
| `VERSION` immediately after `BEGIN` | §6.7.9 | `AllowVersionNotFirst`, reading only |
| A `VERSION`, and at least one `FN` | §6.7.9, §6.2.1 | `AllowMissingRequiredProperties` |
| A `CONTACT-URI` with a `mailto`, `http` or `https` URI, in a card that has others | RFC 8605 §2.1 | `AllowMissingRequiredProperties` |
| The cardinality of each property, and `MEMBER` only with `KIND:group` | §6, §6.6.5 | `AllowCardinalityViolations` |
| The syntax of each value, and `VALUE` only with a type the property allows | §4, §6 | `AllowInvalidValues` |
| A comma in a URI value escaped with a backslash | §3.4 and errata 3845 and 3846 | `AcceptUnescapedCommasInUris`, reading only |
| Parameters only on the properties that allow them, `PREF` from 1 to 100, and a `CLIENTPIDMAP` for each `PID` source | §5, §6, §6.7.7 | `AllowInvalidValues` |
| `LEVEL` by property, `INDEX` strictly positive, `CC` an ISO 3166 code on `ADR`, and a `PREF` among several `CONTACT-URI` properties | RFC 6715 §3, RFC 8605 | `AllowInvalidValues` |
| `SERVICE-TYPE` on a `SOCIALPROFILE` whose value is text, and `USERNAME` only with a URI | RFC 9554 §3.5, §4.10 | `AllowInvalidValues` |
| `GRAMGENDER` instances that differ in `LANGUAGE`, and the `TYPE` values `billing` and `delivery` only on `ADR` | RFC 9554 §3.2, §5 | `AllowInvalidValues` |
| `PHONETIC` only on `N` and `ADR`, with a registered value, an `ALTID` and a related property that has its components; `SCRIPT` with `PHONETIC=script`; and different `LANGUAGE` values among those with the same `ALTID` | RFC 9554 §4.6 | `AllowInvalidValues` |
| No forms of vCard 2.1 or 3.0: a parameter with no name, `ENCODING` or `CHARSET` | §5, Appendix A | `AcceptLegacySyntax`, reading only |

A card read with `AcceptLegacySyntax` that has `ENCODING` or `CHARSET` in a vCard 4.0
card can't be written as vCard 4.0, because the writer never writes those forms
there.

## What is never accepted

Some problems have no option:

- **A `BEGIN` or `END` that isn't paired**, a nested `BEGIN:VCARD` outside the
  `AGENT` of a vCard 2.1 card, a parameter on `BEGIN` or `END`, a group on them other
  than in a vCard 3.0 card (RFC 2426 §4 allows one), and a version that can't be
  read, such as `VERSION:5.0`.
- **A line that doesn't follow the content-line syntax**, such as one with no `:`.
- **A bare carriage return**, which is never a line break.
- **A character that can't be written**, such as a control character in a parameter
  value, or a group or name that isn't valid.

A problem in a card once it has been read doesn't stop the reader: catch the
exception, and read on with the next card. A problem in the data itself, such as a
`BEGIN` with no `END`, does, since where the next card starts is unknown. See
[Read non-compliant cards](../how-to/read-non-compliant-cards.md).

## Why the defaults are strict

A reader that quietly accepts anything hides problems in the data until they matter,
and a writer that writes what a standard doesn't allow passes the problem on to
every program that reads the output. Strict defaults find the problem where it
starts. Each option says, in its name, which rule it relaxes, so code that reads
real-world data says what it expects to find.

## See also

- [Read non-compliant cards](../how-to/read-non-compliant-cards.md)
- [Handle legacy character sets](../how-to/handle-legacy-character-sets.md)
- [Versions and canonical forms](versions-and-canonical-forms.md)
- [VcardOptions](../reference/VcardOptions.md)
- [VcardDiagnosticKind](../reference/VcardDiagnosticKind.md)
- [VcardFileException](../reference/VcardFileException.md)
