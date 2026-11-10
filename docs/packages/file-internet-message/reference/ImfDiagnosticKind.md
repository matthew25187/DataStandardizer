---
title: ImfDiagnosticKind Enum
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfDiagnosticKind Enum

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The kinds of problem that an [`ImfDiagnostic`](ImfDiagnostic.md) reports.

**Syntax**

```csharp
public enum ImfDiagnosticKind
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `UnknownTransferEncoding` | 0 | A body has a Content-Transfer-Encoding that is an extension mechanism (RFC 2045 §6.3), so it could not be decoded. |
| `MissingMimeVersion` | 1 | The header of a message has a MIME field and no MIME-Version field, which RFC 2045 §4 requires. |
| `MissingRequiredField` | 2 | The header has no field that RFC 5322 requires: Date or From (§3.6), Sender when From has more than one mailbox (§3.6.2, RFC 6854 §2.1), or in a block of resent fields, Resent-Date, Resent-From, or Resent-Sender when Resent-From has more than one mailbox (§3.6.6). |
| `CardinalityViolation` | 3 | The header has more than one of a field that the table in RFC 5322 §3.6 allows at most once. |
| `InvalidFieldValue` | 4 | The body of a field that RFC 5322 §3.6 defines does not follow its grammar. |
| `InvalidRelatedStructure` | 5 | A `multipart/related` entity breaks a rule of RFC 2387 §3 or RFC 2557 §7: it has no parts, it has no `type` parameter, its `type` parameter doesn't match its root part, its `start` parameter names no part, or two of its parts have a Content-Location that resolves to the same URL. Two parts with the same Content-ID, which RFC 2045 §7 doesn't allow anywhere, are reported the same way. |
| `MissingMboxSeparator` | 6 | An mbox file doesn't start with a separator line, which RFC 4155 Appendix A requires before each message. |
| `InvalidMboxSeparator` | 7 | A line of an mbox file starts with `From ` and doesn't have the form of a separator line that RFC 4155 Appendix A requires. |
| `MissingMboxBlankLine` | 8 | A message of an mbox file is not ended by the empty line that RFC 4155 Appendix A requires. |
| `InvalidMboxContentLength` | 9 | A message of an mbox file in the [`MboxVariant.Mboxcl`](MboxVariant.md) or [`MboxVariant.Mboxcl2`](MboxVariant.md) variant has a Content-Length field that is missing or not valid, or that doesn't match the length of its body. |
| `MboxCarriageReturnLineFeed` | 10 | An mbox file uses a carriage return and a line feed as a line break, which RFC 4155 Appendix A doesn't allow. |
| `EightBitMboxContent` | 11 | A message of an mbox file holds an octet above 127, which RFC 4155 Appendix A doesn't allow. |
| `InvalidBodyOctets` | 12 | A body holds an octet or a line break that RFC 5322 §2.3 or its transfer encoding doesn't allow (RFC 2045 §2.7, §2.8 and §6.4). |
| `LongBodyLine` | 13 | A body has a line longer than RFC 5322 §2.1.1, or the 76 characters of RFC 2045 §6.7 and §6.8 for an encoded body, allow. |
| `InvalidBodyStructure` | 14 | [`ImfMessage.Validate`](ImfMessage.md) found that the body breaks a rule of RFC 2046 §5 that the options don't accept, so what it holds could not be checked. |
| `InvalidEmlxByteCount` | 15 | The first line of an EMLX file isn't a byte count, or the count doesn't fit the file: it passes the end of the file, or the bytes after it don't start a property list. |
| `InvalidEmlxMetadata` | 16 | An EMLX file has no property list after its message, or what it has isn't a well-formed XML property list with a `dict` at its root. A writer also reports metadata whose values nest more deeply than [`ImfOptions.MaxNestingDepth`](ImfOptions.md) this way. |
| `MissingEmlxPart` | 17 | A part of a partial EMLX file is stored in another file, and no content was supplied for it. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [ImfDiagnostic](ImfDiagnostic.md)
- [ImfMessage](ImfMessage.md)
- [ImfOptions](ImfOptions.md)
- [MboxVariant](MboxVariant.md)
- [File.InternetMessage API reference](index.md)
