---
title: ImfAddress Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfAddress Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

An address (RFC 5322 §3.4): an [`ImfMailbox`](ImfMailbox.md) or an [`ImfGroup`](ImfGroup.md).

**Syntax**

```csharp
public abstract class ImfAddress : IEquatable<ImfAddress>
```

## Remarks

The white space, comments and obsolete syntax of the input (RFC 5322 §4) are not kept. `Object.ToString` writes the modern form.

`DisplayName` holds the text of any RFC 2047 encoded words in a display name (RFC 2047 §5(3)), and a quoted-string of that form stays plain text. A word that can't be decoded stays as written (RFC 2047 §6.2), as [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) describes. Encoded words are never read in a local part or domain. Decoded text can hold anything that the words encode, including control characters and line breaks, so check it before using it where those matter.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `DisplayName` | `string DisplayName { get; }` | Gets the display name, or an empty string if a mailbox has none. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfAddress? other)` | `bool` | Indicates whether this address equals another. A mailbox never equals a group. See [`ImfMailbox.Equals`](ImfMailbox.md) and [`ImfGroup.Equals`](ImfGroup.md) for how each compares. |
| `static Parse(string value)` | `ImfAddress` | Parses an address. **Returns:** The address. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid address. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfAddress` | Parses an address. **Returns:** The address. **Throws** `FormatException`: The value is not a valid address. |
| `static Parse(string value, ImfOptions options)` | `ImfAddress` | Parses an address. **Returns:** The address. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid address. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfAddress` | Parses an address. **Returns:** The address. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid address. |
| `ToString(ImfHeaderEncoding encoding)` | `string` | Formats the address in the modern form, for a writer with the given header encoding. <br><br> With [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md), a display name that isn't US-ASCII is written as RFC 2047 encoded words, in UTF-8, whatever form it was read in. With [`ImfHeaderEncoding.Utf8`](ImfHeaderEncoding.md), it is written as UTF-8 (RFC 6532), and an encoded word that was read is written as its text. <br><br> A local part or domain can't hold an encoded word (RFC 2047 §5), so one that isn't US-ASCII stays as it is, and a writer with [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md) refuses it. A control character in a display name has no form that can be written here, so it is left out. **Returns:** The address, unfolded. **Throws** `ArgumentOutOfRangeException`: `encoding` is not an [`ImfHeaderEncoding`](ImfHeaderEncoding.md) member. |
| `static TryParse(string? value, out ImfAddress? result)` | `bool` | Parses an address. **Returns:** `true` if the value is a valid address; otherwise `false`, including for `null`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfAddress? result)` | `bool` | Parses an address. **Returns:** `true` if the value is a valid address; otherwise `false`. |
| `static TryParse(string? value, ImfOptions options, out ImfAddress? result)` | `bool` | Parses an address. **Returns:** `true` if the value is a valid address; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfAddress? result)` | `bool` | Parses an address. **Returns:** `true` if the value is a valid address; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfAddress? left, ImfAddress? right)` | `bool` | Indicates whether two addresses differ. |
| `operator ==(ImfAddress? left, ImfAddress? right)` | `bool` | Indicates whether two addresses are equal. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfGroup](ImfGroup.md)
- [ImfHeaderEncoding](ImfHeaderEncoding.md)
- [ImfMailbox](ImfMailbox.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
