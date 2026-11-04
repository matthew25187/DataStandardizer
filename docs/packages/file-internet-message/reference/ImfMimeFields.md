---
title: ImfMimeFields Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMimeFields Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Reads and writes the bodies of the MIME header fields whose values are not types of this package: MIME-Version, Content-Type, Content-Transfer-Encoding and Content-Language.

**Syntax**

```csharp
public static class ImfMimeFields
```

## Remarks

Each one is a structured field, so comments and folding white space may come between its tokens (RFC 5322 §3.2.2). Each format method gives an unfolded body with a space at each place it can be folded, for [`ImfHeaderFieldKind.Structured`](ImfHeaderFieldKind.md).

The other MIME fields have their own types. A Content-ID is an [`ImfMessageId`](ImfMessageId.md) (RFC 2045 §7), a Content-Description is the unstructured text of [`ImfHeaderText`](ImfHeaderText.md) (RFC 2045 §8), and a Content-Disposition is an [`ImfContentDisposition`](ImfContentDisposition.md) (RFC 2183).

## Fields

| Field | Signature | Notes |
| --- | --- | --- |
| `MimeVersion` | `static readonly Version MimeVersion` | The version that RFC 2045 §4 defines and requires a message to give, which formats as `1.0`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `static FormatContentLanguage(IEnumerable<Bcp47LanguageTag> languageTags)` | `string` | Writes language tags as the body of a Content-Language field, such as `en, fr`. **Returns:** The field body, unfolded. **Throws** `ArgumentNullException`: `languageTags` is `null`. **Throws** `ArgumentException`: `languageTags` is empty, as RFC 3282 §2 needs at least one, or holds a tag that is not initialised. |
| `static FormatContentType(MimeMediaType mediaType)` | `string` | Writes a media type as the body of a Content-Type field, such as `text/plain; charset=us-ascii`. A parameter is written as a token or a quoted-string. One that isn't US-ASCII, or that has a language, is written as an extended value, and one too long for a line of 78 characters is split into sections (RFC 2231). **Returns:** The field body, unfolded. **Throws** `ArgumentException`: `mediaType` is not initialised. |
| `static ParseContentLanguage(string value)` | `IReadOnlyList<Bcp47LanguageTag>` | Parses the body of a Content-Language field. **Returns:** The language tags, in order. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not valid, as `TryParseContentLanguage` describes. |
| `static ParseContentLanguage(ReadOnlySpan<byte> value)` | `IReadOnlyList<Bcp47LanguageTag>` | Parses the body of a Content-Language field. **Returns:** The language tags, in order. **Throws** `FormatException`: The value is not valid, as `TryParseContentLanguage` describes. |
| `static ParseContentLanguage(string value, ImfOptions options)` | `IReadOnlyList<Bcp47LanguageTag>` | Parses the body of a Content-Language field. **Returns:** The language tags, in order. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid, as `TryParseContentLanguage` describes. |
| `static ParseContentLanguage(ReadOnlySpan<byte> value, ImfOptions options)` | `IReadOnlyList<Bcp47LanguageTag>` | Parses the body of a Content-Language field. **Returns:** The language tags, in order. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid, as `TryParseContentLanguage` describes. |
| `static ParseContentTransferEncoding(string value)` | `MimeTransferEncoding` | Parses the body of a Content-Transfer-Encoding field. **Returns:** The transfer encoding. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid mechanism (RFC 2045 §6.1). |
| `static ParseContentTransferEncoding(ReadOnlySpan<byte> value)` | `MimeTransferEncoding` | Parses the body of a Content-Transfer-Encoding field. **Returns:** The transfer encoding. **Throws** `FormatException`: The value is not a valid mechanism (RFC 2045 §6.1). |
| `static ParseContentType(string value)` | `MimeMediaType` | Parses the body of a Content-Type field. **Returns:** The media type. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not valid, as `TryParseContentType` describes. |
| `static ParseContentType(ReadOnlySpan<byte> value)` | `MimeMediaType` | Parses the body of a Content-Type field. **Returns:** The media type. **Throws** `FormatException`: The value is not valid, as `TryParseContentType` describes. |
| `static ParseContentType(string value, ImfOptions options)` | `MimeMediaType` | Parses the body of a Content-Type field. **Returns:** The media type. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid, as `TryParseContentType` describes. |
| `static ParseContentType(ReadOnlySpan<byte> value, ImfOptions options)` | `MimeMediaType` | Parses the body of a Content-Type field. **Returns:** The media type. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid, as `TryParseContentType` describes. |
| `static ParseMimeVersion(string value)` | `Version` | Parses the body of a MIME-Version field. Any version is read, but only `MimeVersion` is defined, and RFC 2045 §4 says that a message with another can't be assumed to follow it. A number too large for an `Int32` is rejected. **Returns:** The version. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid version. |
| `static ParseMimeVersion(ReadOnlySpan<byte> value)` | `Version` | Parses the body of a MIME-Version field. Any version is read, but only `MimeVersion` is defined, and RFC 2045 §4 says that a message with another can't be assumed to follow it. A number too large for an `Int32` is rejected. **Returns:** The version. **Throws** `FormatException`: The value is not a valid version. |
| `static TryParseContentLanguage(string? value, out IReadOnlyList<Bcp47LanguageTag>? result)` | `bool` | Parses the body of a Content-Language field. **Returns:** `true` if the value is valid, as `TryParseContentLanguage` describes; otherwise `false`, including for `null`. |
| `static TryParseContentLanguage(ReadOnlySpan<byte> value, out IReadOnlyList<Bcp47LanguageTag>? result)` | `bool` | Parses the body of a Content-Language field. **Returns:** `true` if the value is valid, as `TryParseContentLanguage` describes; otherwise `false`. |
| `static TryParseContentLanguage(string? value, ImfOptions options, out IReadOnlyList<Bcp47LanguageTag>? result)` | `bool` | Parses the body of a Content-Language field. **Returns:** `true` if the value is valid, as `TryParseContentLanguage` describes; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParseContentLanguage(ReadOnlySpan<byte> value, ImfOptions options, out IReadOnlyList<Bcp47LanguageTag>? result)` | `bool` | Parses the body of a Content-Language field. RFC 3282 §2 needs at least one tag, and no empty elements. A tag has to be well-formed by the current edition of BCP 47 (RFC 5646 §2.2.9). One that isn't is rejected, unless [`ImfOptions.AllowInvalidValues`](ImfOptions.md) is `true`, when it is left out of the list. The list is then empty if no tag was well-formed. **Returns:** `true` if the value is valid; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParseContentTransferEncoding(string? value, out MimeTransferEncoding result)` | `bool` | Parses the body of a Content-Transfer-Encoding field. **Returns:** `true` if the value is a valid mechanism (RFC 2045 §6.1); otherwise `false`, including for `null`. |
| `static TryParseContentTransferEncoding(ReadOnlySpan<byte> value, out MimeTransferEncoding result)` | `bool` | Parses the body of a Content-Transfer-Encoding field. **Returns:** `true` if the value is a valid mechanism (RFC 2045 §6.1); otherwise `false`. |
| `static TryParseContentType(string? value, out MimeMediaType result)` | `bool` | Parses the body of a Content-Type field. **Returns:** `true` if the value is valid, as `TryParseContentType` describes; otherwise `false`, including for `null`. |
| `static TryParseContentType(ReadOnlySpan<byte> value, out MimeMediaType result)` | `bool` | Parses the body of a Content-Type field. **Returns:** `true` if the value is valid, as `TryParseContentType` describes; otherwise `false`. |
| `static TryParseContentType(string? value, ImfOptions options, out MimeMediaType result)` | `bool` | Parses the body of a Content-Type field. **Returns:** `true` if the value is valid, as `TryParseContentType` describes; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParseContentType(ReadOnlySpan<byte> value, ImfOptions options, out MimeMediaType result)` | `bool` | Parses the body of a Content-Type field. <br><br> The grammar is that of RFC 2045 §5.1, so a name may be any token, not only the narrower names that RFC 6838 §4.2 requires of a registration. Parameters in the forms of RFC 2231 are joined and decoded. A quoted-string may hold raw UTF-8, as RFC 6532 §3.2 allows. A token must be US-ASCII, as RFC 2045 §5.1 requires. <br><br> A value breaks RFC 2231 if its sections or extended value aren't valid, or a character set can't be resolved, and it breaks RFC 2047 §5 if a parameter holds an encoded word, which is never allowed there. Each is rejected unless [`ImfOptions.AllowMalformedParameters`](ImfOptions.md) is `true`, which describes what is then accepted. **Returns:** `true` if the value is valid; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParseMimeVersion(string? value, out Version? result)` | `bool` | Parses the body of a MIME-Version field. **Returns:** `true` if the value is a valid version; otherwise `false`, including for `null`. |
| `static TryParseMimeVersion(ReadOnlySpan<byte> value, out Version? result)` | `bool` | Parses the body of a MIME-Version field. **Returns:** `true` if the value is a valid version; otherwise `false`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfContentDisposition](ImfContentDisposition.md)
- [ImfHeaderFieldKind](ImfHeaderFieldKind.md)
- [ImfHeaderText](ImfHeaderText.md)
- [ImfMessageId](ImfMessageId.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
