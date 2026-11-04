---
title: ImfContentDisposition Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfContentDisposition Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The value of a Content-Disposition header field (RFC 2183), such as `attachment; filename=genome.jpeg`.

**Syntax**

```csharp
public sealed class ImfContentDisposition : IEquatable<ImfContentDisposition>
```

## Remarks

The type and the parameter names are compared case-insensitively, parameter values case-sensitively, and the order of the parameters doesn't matter. Parameters in the forms of RFC 2231 are joined and decoded when read, and each is written as [`ImfMimeFields.FormatContentType`](ImfMimeFields.md) writes one.

A file name comes from whoever sent the message, so treat it with care. RFC 2183 §2.3 says not to respect any directory path in it, and §5 warns against a name that overwrites a file or gets a program run. It isn't checked here.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfContentDisposition(string type)` | Initializes a new disposition with no parameters. **Throws** `ArgumentNullException`: `type` is `null`. **Throws** `ArgumentException`: `type` is not a valid name. |
| `ImfContentDisposition(string type, IEnumerable<MimeParameter> parameters)` | Initializes a new disposition. **Throws** `ArgumentNullException`: `type` or `parameters` is `null`. **Throws** `ArgumentException`: `type` is not a valid name, or `parameters` holds a parameter that is not initialised, more than one parameter with the same name, or a date or size that isn't valid (RFC 2183 §2). |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CreationDate` | `ImfDateTime? CreationDate { get; }` | Gets the date the file was created (RFC 2183 §2.4), or `null` if there is none, or it isn't valid and [`ImfOptions.AllowMalformedParameters`](ImfOptions.md) kept it. |
| `FileName` | `string? FileName { get; }` | Gets the suggested file name (RFC 2183 §2.3), or `null` if there is none. See the remarks on this type before using it. |
| `IsAttachment` | `bool IsAttachment { get; }` | Gets whether to treat the entity as an attachment: its type is anything other than `inline`, as RFC 2183 §2.8 says of a type that isn't recognised. |
| `IsInline` | `bool IsInline { get; }` | Gets whether the type is `inline`, compared case-insensitively. |
| `ModificationDate` | `ImfDateTime? ModificationDate { get; }` | Gets the date the file was last modified (RFC 2183 §2.5), or `null` if there is none, or it isn't valid and [`ImfOptions.AllowMalformedParameters`](ImfOptions.md) kept it. |
| `Parameters` | `IReadOnlyList<MimeParameter> Parameters { get; }` | Gets all the parameters, in the order they are written, including those that have properties here. |
| `ReadDate` | `ImfDateTime? ReadDate { get; }` | Gets the date the file was last read (RFC 2183 §2.6), or `null` if there is none, or it isn't valid and [`ImfOptions.AllowMalformedParameters`](ImfOptions.md) kept it. |
| `Size` | `long? Size { get; }` | Gets the approximate size of the file in octets (RFC 2183 §2.7), or `null` if there is none, or it isn't valid and [`ImfOptions.AllowMalformedParameters`](ImfOptions.md) kept it. |
| `Type` | `string Type { get; }` | Gets the disposition type, as written, such as `attachment`. |

## Fields

| Field | Signature | Notes |
| --- | --- | --- |
| `Attachment` | `static readonly ImfContentDisposition Attachment` | The `attachment` disposition (RFC 2183 §2.2), with no parameters. |
| `Inline` | `static readonly ImfContentDisposition Inline` | The `inline` disposition (RFC 2183 §2.1), with no parameters. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfContentDisposition? other)` | `bool` | Indicates whether this disposition equals another: the types and parameter names are the same, compared case-insensitively, and so are the parameter values, compared case-sensitively, in any order. |
| `static Parse(string value)` | `ImfContentDisposition` | Parses the body of a Content-Disposition field. **Returns:** The disposition. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not valid, as `TryParse` describes. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfContentDisposition` | Parses the body of a Content-Disposition field. **Returns:** The disposition. **Throws** `FormatException`: The value is not valid, as `TryParse` describes. |
| `static Parse(string value, ImfOptions options)` | `ImfContentDisposition` | Parses the body of a Content-Disposition field. **Returns:** The disposition. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid, as `TryParse` describes. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfContentDisposition` | Parses the body of a Content-Disposition field. **Returns:** The disposition. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not valid, as `TryParse` describes. |
| `ToString()` | `string` | Writes the disposition as the body of a Content-Disposition field, such as `attachment; filename=genome.jpeg`. A parameter is written as a token or a quoted-string. One that isn't US-ASCII, or that has a language, is written as an extended value, and one too long for a line of 78 characters is split into sections (RFC 2231), as RFC 2183 §2 requires. **Returns:** The field body, unfolded, with a space at each place it can be folded, for [`ImfHeaderFieldKind.Structured`](ImfHeaderFieldKind.md). |
| `static TryParse(string? value, out ImfContentDisposition? result)` | `bool` | Parses the body of a Content-Disposition field. **Returns:** `true` if the value is valid, as `TryParse` describes; otherwise `false`, including for `null`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfContentDisposition? result)` | `bool` | Parses the body of a Content-Disposition field. **Returns:** `true` if the value is valid, as `TryParse` describes; otherwise `false`. |
| `static TryParse(string? value, ImfOptions options, out ImfContentDisposition? result)` | `bool` | Parses the body of a Content-Disposition field. **Returns:** `true` if the value is valid, as `TryParse` describes; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfContentDisposition? result)` | `bool` | Parses the body of a Content-Disposition field. <br><br> One semicolon may end the value. The grammar of RFC 2183 §2 has no place for it, but the example in its §3 has one. A date has to be a date-time with a numeric zone (RFC 2183 §2), in the modern or obsolete syntax, and [`ImfOptions.AllowInvalidValues`](ImfOptions.md) applies to its day of the week as it does in [`ImfDateTime`](ImfDateTime.md). A size has to be digits, and one too large for a `Int64` is not valid. A parameter value with raw non-ASCII text is rejected, as RFC 2183 §2 requires the form of RFC 2231 for one. <br><br> A value breaks RFC 2231 if its sections or extended value aren't valid, or a character set can't be resolved, and it breaks RFC 2047 §5 if a parameter holds an encoded word, which is never allowed there. Those, and a date or size that isn't valid, are rejected unless [`ImfOptions.AllowMalformedParameters`](ImfOptions.md) is `true`, which describes what is then accepted. **Returns:** `true` if the value is valid; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `WithCreationDate(ImfDateTime date)` | `ImfContentDisposition` | Gets a copy of this disposition with the given creation date. **Returns:** The modified copy. **Throws** `ArgumentException`: `date` was made with `default`. |
| `WithFileName(string fileName)` | `ImfContentDisposition` | Gets a copy of this disposition with the given file name. **Returns:** The modified copy. **Throws** `ArgumentNullException`: `fileName` is `null`. **Throws** `ArgumentException`: `fileName` contains an unpaired surrogate. |
| `WithModificationDate(ImfDateTime date)` | `ImfContentDisposition` | Gets a copy of this disposition with the given modification date. **Returns:** The modified copy. **Throws** `ArgumentException`: `date` was made with `default`. |
| `WithParameter(MimeParameter parameter)` | `ImfContentDisposition` | Gets a copy of this disposition with a parameter added, or with the value of the parameter of the same name replaced, which keeps its position. **Returns:** The modified copy. **Throws** `ArgumentException`: `parameter` is not initialised, or is a date or size that isn't valid (RFC 2183 §2). |
| `WithReadDate(ImfDateTime date)` | `ImfContentDisposition` | Gets a copy of this disposition with the given read date. **Returns:** The modified copy. **Throws** `ArgumentException`: `date` was made with `default`. |
| `WithSize(long size)` | `ImfContentDisposition` | Gets a copy of this disposition with the given size. **Returns:** The modified copy. **Throws** `ArgumentOutOfRangeException`: `size` is negative. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfContentDisposition? left, ImfContentDisposition? right)` | `bool` | Indicates whether two dispositions differ. |
| `operator ==(ImfContentDisposition? left, ImfContentDisposition? right)` | `bool` | Indicates whether two dispositions are equal. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfDateTime](ImfDateTime.md)
- [ImfHeaderFieldKind](ImfHeaderFieldKind.md)
- [ImfMimeFields](ImfMimeFields.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
