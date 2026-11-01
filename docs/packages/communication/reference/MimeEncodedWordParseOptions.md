---
title: MimeEncodedWordParseOptions Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeEncodedWordParseOptions Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Accommodations for RFC 2047 encoded words that don't follow the rules, as many mail clients write
them. The default, `None`, parses strictly.

**Syntax**

```csharp
[Flags]
public enum MimeEncodedWordParseOptions
```

## Fields

| Field | Value | Accepts |
| --- | --- | --- |
| `None` | 0 | Nothing beyond the standards. |
| `AllowLongWords` | 1 | A word longer than 75 characters, which RFC 2047 §2 doesn't allow, and which RFC 2047 §6.1 says to read as ordinary text. |
| `AllowEmbeddedWords` | 2 | In `DecodeText`, a word that isn't separated from the text or word next to it by whitespace, such as the one in `Re:=?UTF-8?Q?caf=C3=A9?=` (RFC 2047 §5, §6.1). The text around it is kept as it is. |
| `AllowUnknownCharsets` | 4 | A word whose character set the resolver doesn't resolve. The name must still be a valid `mime-charset` (RFC 2978 §2.3). Each octet is read as the character with the same number, which is the "best effort" display that RFC 2047 §6.2 (b) allows. `Charset` holds the name as written. |
| `AllowLowercaseHexDigits` | 8 | Lower-case digits after `=` in "Q"-encoded text, as in `=c3=a9` (RFC 2047 §4.2 (1), RFC 2045 §6.7 rule 1). |
| `AllowInvalidOctets` | 16 | A word whose octets aren't valid in its character set, such as `=?UTF-8?Q?=FF?=` (RFC 2047 §6.3 (2), and RFC 3629 §3 for UTF-8). Each invalid sequence is read as U+FFFD. |
| `AllowSplitCharacters` | 32 | In `DecodeText`, a character that a word ends partway through, read with the octets that finish it at the start of the next words that have the same character set and language (RFC 2047 §5). The unfinished part is at most 3 octets. If a run of such words doesn't finish its last character, or its octets aren't valid together, its first word is decoded by itself, and the words after it are read again. |

## Remarks

`TryParse` rejects a word that breaks a rule no option here accepts. `DecodeText` leaves such a word
as it's written, as RFC 2047 §6.1 and §6.2 say to. `AllowEmbeddedWords` and `AllowSplitCharacters` have
no effect on `TryParse`, which reads one whole word.

The `netstandard1.1` build can't change how an encoding treats invalid octets. So there, without
`AllowInvalidOctets`, the text is accepted only if writing it with the encoding gives the same octets
back. A valid word in a character set that can write a character in more than one way needs the option
on that build.
