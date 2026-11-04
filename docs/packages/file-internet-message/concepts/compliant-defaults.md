---
title: Compliant defaults and the opt-ins
parent: File.InternetMessage
grand_parent: Packages
nav_order: 12
---

# Compliant defaults and the opt-ins

Every default in the package follows the standards. Real-world messages often break
the rules, so each kind of accommodation is a separate property of `ImfOptions` that you
turn on by name. All of them default to `false`. There is no single "lenient mode"
switch, so you accept only the problems you expect, and still catch the rest.

All public types here are in the `DataStandardizer.File.InternetMessage` namespace.

## What the defaults mean

With a default `ImfOptions`:

- **A reader rejects what the standard doesn't allow.** The first problem throws
  `ImfFileException`. A bare line feed, a date that doesn't follow its grammar, a
  multipart entity with no close delimiter and a MIME field with no MIME-Version field
  are all refused.
- **A reader accepts what the standard tells it to.** RFC 5322 §4 says that a receiver
  MUST accept the obsolete syntax, and the `obs-fields` of §4.5 sets no minimum or
  maximum for any field. So a message with no Date, or with two Subject fields, is read,
  and the problem is recorded as an `ImfDiagnostic`. No option is involved.
- **A writer refuses to write what the standard doesn't allow,** and it checks the
  whole message first, so nothing of a refused message reaches the output. A message
  with no From or Date is refused. The obsolete syntax is never generated, but a field
  that was read in that syntax and hasn't changed is copied as it is. See
  [Byte-exact round trips](round-trips.md).

For example, this message has a bare line feed, and is refused:

```text
Found a line feed without a carriage return.  RFC 5322 §2.2 allows LF only in a CRLF line break.
```

The exception's `Data` says where the problem is, under the keys in
`ImfFileException.DataItemName`: `BytePosition`, `LineNumber` and, where they apply,
`FieldName`, `EntityPath`, `MessageIndex` and `DiagnosticKind`. Each is there only when
it applies, so check with `Data.Contains` first.

## The opt-ins

```csharp
using DataStandardizer.File.InternetMessage;

var options = new ImfOptions
{
    AcceptBareLineFeed = true,
    AllowInvalidValues = true
};

using var reader = new ImfReader("message.eml", options);
var message = reader.ReadMessage();

foreach (var diagnostic in reader.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Kind} {diagnostic.FieldName}: {diagnostic.Message}");
}
```

For a message with `Date: yesterday`, this prints:

```text
InvalidFieldValue Date: The body of the Date field does not follow its grammar (RFC 5322 §3.6, RFC 6854 §2.1).
```

`ImfOptions` is a `record` on `net10.0`, so you can make a variation with `with`
there. On the .NET Standard targets it is a plain `class` with `set` accessors.

### Messages

| Option | What it accepts | Rule it relaxes |
| --- | --- | --- |
| `AcceptBareLineFeed` | A bare line feed as a line break. A bare carriage return is never accepted. | RFC 5322 §2.1 and §2.2 |
| `AllowLongLines` | A line longer than 998 octets, or a line of a `quoted-printable` or `base64` body longer than 76. | RFC 5322 §2.1.1, RFC 2045 §6.7 and §6.8 |
| `AllowEightBitHeaders` | A field body that isn't valid UTF-8. `HeaderFallbackCharset` decodes it. Field names are not affected. | RFC 6532 §3.2 |
| `AllowInvalidBodyOctets` | An octet or a line break that the transfer encoding doesn't allow. | RFC 5322 §2.3, RFC 2045 §2.7, §2.8 and §6.4 |
| `AllowInvalidValues` | A date with the wrong day of the week, or a field body that breaks its grammar. The accessor for the field then gives `null`. | RFC 5322 §3.3 and §3.6, RFC 6854 §2.1, RFC 3282 §2, RFC 2557 §4 |
| `AllowInvalidTextOctets` | Octets that aren't valid in the character set of a text body, when you call `GetText`. Each is read as U+FFFD. | RFC 2046 §4.1.2 |
| `AllowMissingMimeVersion` | A MIME field with no MIME-Version field. | RFC 2045 §4 |
| `AllowMissingRequiredFields` | When writing, a message with no Date or From, or no Sender when From has more than one mailbox. When reading or writing, an embedded message with no From, Subject or Date. | RFC 5322 §3.6, RFC 6854 §2.1, RFC 2046 §5.2.1 |
| `AllowCardinalityViolations` | When writing, a second field of a kind that may appear only once. | RFC 5322 §3.6 |
| `AllowMissingBoundaryTerminator` | A multipart entity with no close delimiter. | RFC 2046 §5.1.1 |
| `AllowMalformedEncodedWords` | Encoded words that break the rules. Without it, they stay as ordinary text. | RFC 2047 §2, §4, §5 and §6.3 |
| `AllowMalformedParameters` | Parameters of Content-Type or Content-Disposition that break the rules. | RFC 2231 §3, §4 and §7, RFC 2047 §5, RFC 2045 §5.1, RFC 2183 §2 |

### File formats

| Option | What it accepts | Rule it relaxes |
| --- | --- | --- |
| `AllowInvalidRelatedStructure` | A `multipart/related` entity with no `type` parameter, no root, or parts labelled the same. Word writes web pages with no `type`. | RFC 2387 §3, RFC 2557 §7, RFC 2045 §7 |
| `AllowMissingMboxSeparator` | An mbox file that doesn't start with a separator line. Everything before the first is skipped. | RFC 4155 Appendix A |
| `AllowInvalidMboxSeparator` | A separator line in another form, as Thunderbird and Gmail write. | RFC 4155 Appendix A |
| `AllowMissingMboxBlankLine` | A message of an mbox file that isn't ended by an empty line. | RFC 4155 Appendix A |
| `AllowMboxCarriageReturnLineFeed` | An mbox file with CRLF line breaks. | RFC 4155 Appendix A |
| `AllowEightBitMboxContent` | A message of an mbox file with octets above 127. Many mailboxes on disk have them. | RFC 4155 Appendix A |
| `AllowInvalidMboxContentLength` | A Content-Length field that is missing or wrong, in the `Mboxcl` and `Mboxcl2` variants. | The rule of those variants |
| `AllowInvalidEmlxByteCount` | An EMLX file whose first line is wrong. The reader makes its best guess at where the property list starts. | The layout of EMLX files |
| `AllowInvalidEmlxMetadata` | An EMLX file whose property list is missing or not well formed. | The layout of EMLX files |

Each option's page entry in [ImfOptions](../reference/ImfOptions.md) gives the rule in
full, and says what happens to the value.

### Settings that aren't accommodations

The other options don't relax a rule:

| Option | Meaning |
| --- | --- |
| `AllowEightBitTransferEncoding`, `AllowBinaryTransferEncoding` | Whether the destination can carry `8bit` (RFC 6152) or `binary` (RFC 3030). Without them, `ImfEntityWriter` encodes such a body. |
| `DecodeTransferEncoding` | Whether `ImfEntityReader` decodes bodies as it reads them. |
| `HeaderEncoding` | Whether a writer writes header text that isn't US-ASCII as encoded words (RFC 2047, the default) or as UTF-8 (RFC 6532). |
| `CharsetResolver`, `HeaderFallbackCharset` | How character sets are found. See [Read text in other character sets](../how-to/register-code-pages.md). |
| `Max...` | The limits on what a message can make the package do. See [Limits for hostile input](hostile-input-limits.md). |

## What is never accepted

These have no option:

- A bare carriage return as a line break in a header. RFC 5322 §4.1 lists it in the
  obsolete syntax, but where a line ends would then be unclear.
- An octet above 127 in a field name (RFC 6532 leaves names as printable US-ASCII).
- A second Content-Type or Content-Transfer-Encoding field in an entity, when an
  `ImfEntityReader` needs them to find its parts.
- A transfer encoding other than `7bit`, `8bit` or `binary` on a multipart or message
  entity (RFC 2045 §6.4). `message/global` may have others (RFC 6532 §3.7).

## Checking a message with `Validate`

`ImfMessage.Validate()` makes the checks of the readers and writers, and returns every
problem as a list of `ImfDiagnostic`, without throwing. Use it on a message built in
code, or one read with accommodations on:

```csharp
foreach (var diagnostic in new ImfMessage { Subject = "No From or Date" }.Validate())
{
    Console.WriteLine($"{diagnostic.Kind} {diagnostic.FieldName}: {diagnostic.Message}");
}
```

```text
MissingRequiredField Date: The header has no Date field, which RFC 5322 §3.6 requires.
MissingRequiredField From: The header has no From field, which RFC 5322 §3.6 requires.
```

Each diagnostic has a `Kind`, a `Message`, and, where they apply, a `FieldName`, an
`EntityPath` and a `BytePosition`. The [ImfDiagnosticKind](../reference/ImfDiagnosticKind.md)
page lists the kinds.

Readers keep what they find in `Diagnostics`. `MboxReader` starts its list again with
each message, and the problems in a message are in `MboxEntry.Diagnostics`, so a mailbox
of any size takes the same memory.

## Related

- [Limits for hostile input](hostile-input-limits.md)
- [Byte-exact round trips](round-trips.md)
- [ImfOptions](../reference/ImfOptions.md), [ImfDiagnostic](../reference/ImfDiagnostic.md)
  and [ImfFileException](../reference/ImfFileException.md)
