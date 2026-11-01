---
title: MimeQuotedPrintableDecoderOptions Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeQuotedPrintableDecoderOptions Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Forms that a quoted-printable decoder accepts although RFC 2045 §6.7 and RFC 2047 §4.2
don't allow them, because many producers write them.

**Syntax**

```csharp
[Flags]
public enum MimeQuotedPrintableDecoderOptions
```

## Fields

| Field | Value | Accepts |
| --- | --- | --- |
| `None` | 0 | Only the forms the mode allows. |
| `AllowLowercaseHexDigits` | 1 | Lower-case digits after `=`, as in `=3d`. RFC 2045 §6.7 rule 1 requires upper-case digits, and says a robust decoder might accept lower-case ones. |
| `AllowBareLineFeeds` | 2 | In `Body` mode, a bare LF as a line break, decoded as LF. RFC 2045 §6.7 allows only CRLF. Trailing whitespace before a bare LF is removed, and `=` before it is a soft line break. A bare CR is never accepted. |
| `AllowLiteralEqualsSigns` | 4 | An `=` that isn't followed by two hexadecimal digits or a line break, decoded as itself, with the characters after it. RFC 2045 §6.7 note (2) suggests this for a robust decoder. It includes an `=` at the end of the data. |
