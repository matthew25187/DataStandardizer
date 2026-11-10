---
title: MimeQuotedPrintableEncoderOptions Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeQuotedPrintableEncoderOptions Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

How a quoted-printable encoder treats the data it encodes.

**Syntax**

```csharp
[Flags]
public enum MimeQuotedPrintableEncoderOptions
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `None` | 0 | Encode the data as text: in `Body` mode, each CRLF or bare LF is a line break, and is written as a CRLF line break. |
| `Binary` | 1 | In `Body` mode, encode every CR and LF as `=0D` and `=0A`, so the encoded data has only soft line breaks. RFC 2045 §6.7 rule 4 says to encode data that isn't text this way. The "Q" modes always encode CR and LF, so this has no effect on them. |
