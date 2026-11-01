---
title: MimeBase64Mode Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeBase64Mode Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A form of base64, which sets how encoded data is laid out. Both forms use the alphabet
of RFC 4648 §4, with `=` padding the last group, and both decode to the same bytes.

**Syntax**

```csharp
public enum MimeBase64Mode
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Strict` | 0 | The base64 of RFC 4648 §4: padding is required, and line breaks, whitespace and other characters outside the alphabet aren't allowed. This is the form of an iCalendar BINARY value (RFC 5545 §3.3.1), and of the RFC 2047 §4.1 "B" encoding used in encoded words and in vCard 3.0 `ENCODING=b`. |
| `Mime` | 1 | The `base64` Content-Transfer-Encoding of RFC 2045 §6.8: lines of at most 76 characters, separated by CRLF. When decoding, characters outside the alphabet are ignored, unless `RejectNonAlphabetCharacters` is set. |
