---
title: MimeMediaTypeParseOptions Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeMediaTypeParseOptions Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Accommodations for media types that don't follow the strict grammar. The default,
`None`, parses strictly.

**Syntax**

```csharp
[Flags]
public enum MimeMediaTypeParseOptions
```

## Fields

| Field | Value | Accepts |
| --- | --- | --- |
| `None` | 0 | Nothing beyond the standards. |
| `AllowWhitespaceAroundDelimiters` | 1 | Spaces and tabs before and after each `;` and `=`, and at the end, as in `text/plain; charset = us-ascii`. Comments and folding are still not accepted. |
| `AllowRfc2045Tokens` | 2 | Names that are RFC 2045 §5.1 tokens but not RFC 6838 §4.2 restricted names, such as names longer than 127 characters, or that start with a symbol, or that contain `*`, `%`, `'`, `` ` ``, `{`, `\|`, `}` or `~`. |
| `AllowMissingSections` | 4 | Sections not numbered from 0 with no gaps (RFC 2231 §3). They're joined in numeric order. |
| `AllowDuplicateSections` | 8 | Two sections of the same number (RFC 2231 §3). The first is used. |
| `AllowLeadingZeroSectionNumbers` | 16 | A section number such as `title*01` (RFC 2231 §3), read as section 1. |
| `AllowUnknownCharsets` | 32 | An extended value whose character set the resolver doesn't resolve, or whose octets aren't US-ASCII when it gives no character set (RFC 2231 §4). The name must still be a valid `mime-charset` (RFC 2978 §2.3). Each octet is read as the character with the same number. |
| `AllowQuotedExtendedValues` | 64 | An extended value written as a quoted-string (RFC 2231 §7). |
| `AllowLowercaseHexDigits` | 128 | Lower-case digits in the `%` escapes of an extended value, such as `%c3%a9` (RFC 2231 §7). |
| `AllowInvalidOctets` | 256 | An extended value whose octets aren't valid in its character set (RFC 2231 §4, and RFC 3629 §3 for UTF-8). Each invalid sequence is read as U+FFFD. |
| `AllowNonstandardSections` | 28 | `AllowMissingSections`, `AllowDuplicateSections` and `AllowLeadingZeroSectionNumbers`. |
| `AllowNonstandardExtendedValues` | 192 | `AllowQuotedExtendedValues` and `AllowLowercaseHexDigits`. `AllowUnknownCharsets` isn't included, as it changes how a value is decoded. |

## Remarks

A parameter name in an RFC 2231 form, such as `title*`, `title*1` or `title*1*`, is
always read as that form, and only the part before the form must be a valid name.

The `netstandard1.1` build can't change how an encoding treats invalid octets. So there,
without `AllowInvalidOctets`, a value is accepted only if writing it with the encoding
gives the same octets back. A valid value in a character set that can write a character
in more than one way needs the option on that build.
