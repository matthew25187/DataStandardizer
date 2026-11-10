---
title: ImfHeaderText Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderText Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Reads and writes the text of an unstructured header field or a comment, with RFC 2047 encoded words where RFC 2047 §5 allows them.

**Syntax**

```csharp
public static class ImfHeaderText
```

## Remarks

Encoded words in a phrase are handled by the types that hold one: [`ImfMailbox`](ImfMailbox.md), [`ImfGroup`](ImfGroup.md), [`ImfAddressList`](ImfAddressList.md) and [`ImfPhraseList`](ImfPhraseList.md). An encoded word is never read in a quoted-string, an addr-spec, a message identifier or a parameter value.

A word that has the form of an encoded word but can't be decoded, such as one whose character set can't be resolved, stays as ordinary text (RFC 2047 §6.1 and §6.2). [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) accepts some of them.

Decoded text can hold anything that the words encode, including control characters and line breaks, so check it before using it where those matter. A writer refuses such text, so it can't add a header field.

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `static DecodeUnstructured(string value)` | `string` | Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)). **Returns:** The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent encoded words removed (RFC 2047 §6.2). **Throws** `ArgumentNullException`: `value` is `null`. |
| `static DecodeUnstructured(ReadOnlySpan<byte> value)` | `string` | Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)). **Returns:** The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent encoded words removed (RFC 2047 §6.2). **Throws** `FormatException`: The value is not valid UTF-8. |
| `static DecodeUnstructured(string value, ImfOptions options)` | `string` | Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)). **Returns:** The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent encoded words removed (RFC 2047 §6.2). **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static DecodeUnstructured(ReadOnlySpan<byte> value, ImfOptions options)` | `string` | Decodes the body of an unstructured header field, such as Subject (RFC 2047 §5(1)). **Returns:** The text, without the white space at either end of the body, and with each encoded word decoded and the white space between adjacent encoded words removed (RFC 2047 §6.2). **`value`:** The field body, folded or unfolded. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid UTF-8, and [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `false`. |
| `static FormatComment(string text, ImfHeaderEncoding encoding)` | `string` | Writes text as a comment, for the body of a structured header field. <br><br> Words are encoded as in `FormatUnstructured`, and any other parenthesis or backslash is written as a quoted-pair, so `GetComments(string)` gives the same text back, including white space at either end. **Returns:** The comment, with its parentheses, unfolded. **Throws** `ArgumentNullException`: `text` is `null`. **Throws** `ArgumentException`: `text` holds a control character other than tab or an unpaired surrogate. **Throws** `ArgumentOutOfRangeException`: `encoding` is not an [`ImfHeaderEncoding`](ImfHeaderEncoding.md) member. |
| `static FormatUnstructured(string text, ImfHeaderEncoding encoding)` | `string` | Writes text as the body of an unstructured header field, such as Subject. <br><br> A word that has to be encoded becomes encoded words, together with any such words next to it and the white space between them, which would otherwise be lost (RFC 2047 §6.2). Other words, and the white space around them, are written as they are. So is white space at either end of the text, which a reader doesn't count as part of it. <br><br> With [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md), a word has to be encoded if it isn't US-ASCII. With either encoding, it has to be if it holds `=?`, so that a reader doesn't take text that looks like an encoded word for one. **Returns:** The field body, unfolded. Every encoded word in it is short enough that it can be folded to fit a line of 78 characters. **Throws** `ArgumentNullException`: `text` is `null`. **Throws** `ArgumentException`: `text` holds a control character other than tab or an unpaired surrogate. **Throws** `ArgumentOutOfRangeException`: `encoding` is not an [`ImfHeaderEncoding`](ImfHeaderEncoding.md) member. |
| `static GetComments(string value)` | `IReadOnlyList<string>` | Gets the text of each outermost comment in the body of a structured header field, such as `Mary <mary@example.net> (=?UTF-8?Q?caf=C3=A9?=)`. **Returns:** The text of each comment, in order, as `TryGetComments(ReadOnlySpan<byte>, ImfOptions, out IReadOnlyList<string>)` gives it. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value does not divide into the tokens of RFC 5322 §3.2. |
| `static GetComments(ReadOnlySpan<byte> value)` | `IReadOnlyList<string>` | Gets the text of each outermost comment in the body of a structured header field, such as `Mary <mary@example.net> (=?UTF-8?Q?caf=C3=A9?=)`. **Returns:** The text of each comment, in order, as `TryGetComments(ReadOnlySpan<byte>, ImfOptions, out IReadOnlyList<string>)` gives it. **Throws** `FormatException`: The value is not valid UTF-8, or does not divide into the tokens of RFC 5322 §3.2. |
| `static GetComments(string value, ImfOptions options)` | `IReadOnlyList<string>` | Gets the text of each outermost comment in the body of a structured header field, such as `Mary <mary@example.net> (=?UTF-8?Q?caf=C3=A9?=)`. **Returns:** The text of each comment, in order, as `TryGetComments(ReadOnlySpan<byte>, ImfOptions, out IReadOnlyList<string>)` gives it. **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value does not divide into the tokens of RFC 5322 §3.2. |
| `static GetComments(ReadOnlySpan<byte> value, ImfOptions options)` | `IReadOnlyList<string>` | Gets the text of each outermost comment in the body of a structured header field, such as `Mary <mary@example.net> (=?UTF-8?Q?caf=C3=A9?=)`. **Returns:** The text of each comment, in order, as `TryGetComments(ReadOnlySpan<byte>, ImfOptions, out IReadOnlyList<string>)` gives it. **`value`:** The field body, folded or unfolded. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value does not divide into the tokens of RFC 5322 §3.2, or is not valid UTF-8 and [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `false`. |
| `static TryGetComments(string? value, ImfOptions options, out IReadOnlyList<string>? comments)` | `bool` | Gets the text of each outermost comment in the body of a structured header field. The text of a comment has no outer parentheses, and its folds are unfolded. Each quoted-pair stands for its character, and the parentheses of a nested comment are kept, so `(a \( (b))` gives `a ( (b)`. An encoded word is read in each run of `ctext` and white space (RFC 2047 §5(2)), but never in a quoted-pair or another token. **Returns:** `true` if the value divides into the tokens of RFC 5322 §3.2; otherwise `false`, including for `null`. **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryGetComments(ReadOnlySpan<byte> value, ImfOptions options, out IReadOnlyList<string>? comments)` | `bool` | Gets the text of each outermost comment in the body of a structured header field. The text of a comment has no outer parentheses, and its folds are unfolded. Each quoted-pair stands for its character, and the parentheses of a nested comment are kept, so `(a \( (b))` gives `a ( (b)`. An encoded word is read in each run of `ctext` and white space (RFC 2047 §5(2)), but never in a quoted-pair or another token. **Returns:** `true` if the value divides into the tokens of RFC 5322 §3.2; otherwise `false`. **`value`:** The field body, folded or unfolded. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfAddressList](ImfAddressList.md)
- [ImfGroup](ImfGroup.md)
- [ImfHeaderEncoding](ImfHeaderEncoding.md)
- [ImfMailbox](ImfMailbox.md)
- [ImfOptions](ImfOptions.md)
- [ImfPhraseList](ImfPhraseList.md)
- [File.InternetMessage API reference](index.md)
