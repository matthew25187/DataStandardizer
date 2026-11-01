---
title: MimeEncodedWord Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeEncodedWord Struct

## Definition

Namespace: `DataStandardizer.Communication.Mime`

An RFC 2047 encoded word, such as `=?ISO-8859-1?Q?Andr=E9?=`, which carries text that isn't
US-ASCII in a message header: its character set, the RFC 2231 §5 language of its text, if any,
its encoding, and its decoded text.

**Syntax**

```csharp
public readonly struct MimeEncodedWord : IEquatable<MimeEncodedWord>, ISpanFormattable, ISpanParsable<MimeEncodedWord>
```

`ISpanFormattable` and `ISpanParsable<T>` are on the `net8.0` and `net10.0` builds only.

## Remarks

A parsed word is written back as it was written. A word that's created is written in UTF-8.
Two words are equal when their character sets, languages, encodings and texts are equal, even
when they're written differently. Character sets and languages are compared
case-insensitively, and texts case-sensitively.

This type doesn't decide where an encoded word may appear (RFC 2047 §5). That's up to the
grammar of the header field, which the caller parses.

See [Use encoded words](../how-to/use-encoded-words.md).

## Fields

| Field | Type | Notes |
| --- | --- | --- |
| `MaxLength` | `int` | 75: the most characters an encoded word may have, including its delimiters (RFC 2047 §2). |

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeEncodedWord(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context)` | Holds text in UTF-8. Throws `ArgumentException` if the text doesn't fit in one word. |
| `MimeEncodedWord(string text, MimeEncodedWordEncoding encoding, MimeEncodedWordContext context, string language)` | With the language of the text, which must be an RFC 1766 tag (RFC 2231 §5). |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Charset` | [MimeCharset](MimeCharset.md) | The character set of the text, as written. The text is already decoded from it. |
| `Language` | `string` | The language of the text, such as `EN` in `=?US-ASCII*EN?Q?Keith_Moore?=`; or an empty string. |
| `Encoding` | [MimeEncodedWordEncoding](MimeEncodedWordEncoding.md) | The encoding the text is written in. |
| `Text` | `string` | The decoded text. |

The properties throw `InvalidOperationException` on a value that isn't initialised.

## Methods

| Method | Notes |
| --- | --- |
| `static IReadOnlyList<MimeEncodedWord> EncodeText(string text, MimeEncodedWordContext context)` | Writes text of any length as words in UTF-8, in whichever of "B" and "Q" is shorter, or "Q" if they're the same length. A character or surrogate pair is never split between words. |
| `static IReadOnlyList<MimeEncodedWord> EncodeText(string text, MimeEncodedWordContext context, string language)` | With the language of the text. |
| `static string DecodeText(text)` | Decodes the words in a run of text strictly, as in an unstructured header field. `text` is a `string` or `ReadOnlySpan<char>`. Anything that isn't a valid word is kept as it is. |
| `static string DecodeText(text, MimeEncodedWordContext context, MimeEncodedWordParseOptions options, IMimeCharsetResolver charsetResolver)` | With a context, accommodations and a resolver. |
| `static bool TryParse(value, out MimeEncodedWord result)` | Parses one word strictly, as in unstructured text. `value` is a `string` or `ReadOnlySpan<char>`. |
| `static bool TryParse(value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options, IMimeCharsetResolver charsetResolver, out MimeEncodedWord result)` | With a context, accommodations and a resolver. |
| `static MimeEncodedWord Parse(value)` | Throws `FormatException` if the value isn't valid. |
| `static MimeEncodedWord Parse(value, MimeEncodedWordContext context, MimeEncodedWordParseOptions options, IMimeCharsetResolver charsetResolver)` | With a context, accommodations and a resolver. |
| `bool TryFormat(Span<char> destination, out int charsWritten)` | As it was written, if it was parsed. Writes nothing if the value isn't initialised. |
| `string ToString()` | As `TryFormat`. An empty string if the value isn't initialised. |

The type also has `Equals`, `GetHashCode`, and the `==` and `!=` operators.
