---
title: MimeEncodedWordEncoding Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeEncodedWordEncoding Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

The encoding of the text of an RFC 2047 encoded word (RFC 2047 §4).

**Syntax**

```csharp
public enum MimeEncodedWordEncoding
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `B` | 0 | The "B" encoding of RFC 2047 §4.1, which is base64. It suits text that's mostly not ASCII. |
| `Q` | 1 | The "Q" encoding of RFC 2047 §4.2, which is like quoted-printable. It suits text that's mostly ASCII, which it leaves readable. |
