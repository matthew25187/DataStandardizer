---
title: MimeBase64DecoderOptions Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeBase64DecoderOptions Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Checks that a base64 decoder makes beyond those of its
[MimeBase64Mode](MimeBase64Mode.md).

**Syntax**

```csharp
[Flags]
public enum MimeBase64DecoderOptions
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `None` | 0 | Decode as the mode requires. |
| `RejectNonAlphabetCharacters` | 1 | In `Mime` mode, accept only CRLF line breaks between the characters of the alphabet, and reject any other character, including a bare CR or LF. RFC 2045 §6.8 says to ignore such characters, but also that they probably mean a transmission error. This option finds that error. It has no effect in `Strict` mode. |
