---
title: MimeQuotedPrintableMode Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeQuotedPrintableMode Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A form of quoted-printable: the `quoted-printable` Content-Transfer-Encoding of a body, or
the "Q" encoding of an encoded word in one of the places it can appear. Every form writes
an octet that needs it as `=` and two upper-case hexadecimal digits. The "Q" forms write a
space as `_`, and have no line breaks.

**Syntax**

```csharp
public enum MimeQuotedPrintableMode
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Body` | 0 | The `quoted-printable` Content-Transfer-Encoding of RFC 2045 §6.7: lines of at most 76 characters, with `=` at the end of a line marking a soft line break. This is also the form of vCard 2.1 `ENCODING=QUOTED-PRINTABLE` values. |
| `EncodedWordText` | 1 | "Q" for unstructured text such as Subject (RFC 2047 §5(1)): any printable ASCII character other than `=`, `?` and `_` can be written as itself. |
| `EncodedWordComment` | 2 | "Q" for a comment (RFC 2047 §5(2)): as `EncodedWordText`, except that `(`, `)` and `"` are encoded too. When encoding, `\` is written as `=5C` as well, as it would start a quoted-pair (RFC 5322 §3.2.2). When decoding, a literal `\` is accepted. |
| `EncodedWordPhrase` | 3 | "Q" for a phrase, such as the display name of an address (RFC 2047 §5(3)): only ASCII letters, digits, `!`, `*`, `+`, `-` and `/` can be written as themselves. |
