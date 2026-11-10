---
title: ImfPhraseList Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfPhraseList Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

An ordered, read-only list of phrases, as in a Keywords header field (RFC 5322 §3.6.5).

**Syntax**

```csharp
public sealed class ImfPhraseList : IEquatable<ImfPhraseList>, IReadOnlyList<string>
```

## Remarks

Each element holds the words of one phrase, separated by a space. The empty elements of the obsolete form (RFC 5322 §4.1) are not kept, and that form allows no phrases at all, so a parsed list can be empty. The modern form needs at least one, so an empty list formats as an empty string.

Each element holds the text of any RFC 2047 encoded words in the phrase (RFC 2047 §5(3)), and a quoted-string of that form stays plain text. A word that can't be decoded stays as written (RFC 2047 §6.2), as [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) describes. Decoded text can hold anything that the words encode, including control characters and line breaks, so check it before using it where those matter.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfPhraseList(IEnumerable<string> phrases)` | Initializes a new list. **Throws** `ArgumentNullException`: `phrases` is `null`. **Throws** `ArgumentException`: `phrases` is empty, as RFC 5322 §3.6.5 needs at least one phrase, holds a `null` element, or holds one with a control character other than tab or an unpaired surrogate. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | The number of items. |
| `this[]` | `string this[int index] { get; }` | The item at an index. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfPhraseList? other)` | `bool` | Indicates whether this list equals another: it has the same phrases in the same order, compared case-sensitively as text, so `=?UTF-8?Q?a?=` and `a` are equal. |
| `GetEnumerator()` | `IEnumerator<string>` | Gets an enumerator over the items. |
| `static Parse(string value)` | `ImfPhraseList` | Parses a list of phrases. **Returns:** The list. **`value`:** The list, which may be folded, such as `first, "second one"`. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid list of phrases. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfPhraseList` | Parses a list of phrases. **Returns:** The list. **`value`:** The list as UTF-8 (RFC 6532), which may be folded, such as `first, "second one"`. **Throws** `FormatException`: The value is not a valid list of phrases. |
| `static Parse(string value, ImfOptions options)` | `ImfPhraseList` | Parses a phrase list. **Returns:** The phrase list. **`value`:** The phrase list, which may be folded, such as `first, "second one"`. **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid phrase list. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfPhraseList` | Parses a phrase list. **Returns:** The phrase list. **`value`:** The phrase list, which may be folded, such as `first, "second one"`. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid phrase list. |
| `ToString()` | `string` | Formats the list in the modern form, such as `first, "second one"`. An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8. A word right next to a period is the exception: the phrase is written as new encoded words, as only the obsolete form could keep it. Use `ToString(ImfHeaderEncoding)` to choose. **Returns:** The list. |
| `ToString(ImfHeaderEncoding encoding)` | `string` | Formats the list in the modern form, for a writer with the given header encoding. With [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md), a phrase that isn't US-ASCII is written as RFC 2047 encoded words, in UTF-8, whatever form it was read in. With [`ImfHeaderEncoding.Utf8`](ImfHeaderEncoding.md), it is written as UTF-8 (RFC 6532), and an encoded word that was read is written as its text. A control character in a phrase has no form that can be written here, so it is left out. **Returns:** The list, unfolded. **Throws** `ArgumentOutOfRangeException`: `encoding` is not an [`ImfHeaderEncoding`](ImfHeaderEncoding.md) member. |
| `static TryParse(string? value, out ImfPhraseList? result)` | `bool` | Parses a list of phrases. **Returns:** `true` if the value is a valid list of phrases; otherwise `false`, including for `null`. **`value`:** The list, which may be folded, such as `first, "second one"`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfPhraseList? result)` | `bool` | Parses a list of phrases. **Returns:** `true` if the value is a valid list of phrases; otherwise `false`. **`value`:** The list as UTF-8 (RFC 6532), which may be folded, such as `first, "second one"`. |
| `static TryParse(string? value, ImfOptions options, out ImfPhraseList? result)` | `bool` | Parses a phrase list. **Returns:** `true` if the value is a valid phrase list; otherwise `false`, including for `null`. **`value`:** The phrase list, which may be folded, such as `first, "second one"`. **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfPhraseList? result)` | `bool` | Parses a phrase list. **Returns:** `true` if the value is a valid phrase list; otherwise `false`. **`value`:** The phrase list, which may be folded, such as `first, "second one"`. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfPhraseList? left, ImfPhraseList? right)` | `bool` | Indicates whether two lists differ. |
| `operator ==(ImfPhraseList? left, ImfPhraseList? right)` | `bool` | Indicates whether two lists are equal. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfHeaderEncoding](ImfHeaderEncoding.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
