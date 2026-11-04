---
title: ImfAddressList Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfAddressList Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

An ordered, read-only list of addresses (RFC 5322 §3.4), with at least one.

**Syntax**

```csharp
public sealed class ImfAddressList : IEquatable<ImfAddressList>, IReadOnlyList<ImfAddress>
```

## Remarks

A group is parsed wherever an address may appear, as RFC 6854 allows in From and Sender. Which forms a given field allows is not checked here.

The empty elements of the obsolete form (RFC 5322 §4.4), such as in `a@b.test, , c@d.test`, are not kept.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfAddressList(IEnumerable<ImfAddress> addresses)` | Initializes a new list. **Throws** `ArgumentNullException`: `addresses` is `null`. **Throws** `ArgumentException`: `addresses` is empty, as RFC 5322 §3.4 needs at least one address, or holds a `null` element. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Count` | `int Count { get; }` | The number of items. |
| `this[]` | `ImfAddress this[int index] { get; }` | The item at an index. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfAddressList? other)` | `bool` | Indicates whether this list equals another: it has equal addresses in the same order. |
| `GetEnumerator()` | `IEnumerator<ImfAddress>` | Gets an enumerator over the items. |
| `static Parse(string value)` | `ImfAddressList` | Parses a list of addresses. **Returns:** The list. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid list of addresses. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfAddressList` | Parses a list of addresses. **Returns:** The list. **Throws** `FormatException`: The value is not a valid list of addresses. |
| `static Parse(string value, ImfOptions options)` | `ImfAddressList` | Parses an address list. **Returns:** The address list. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid address list. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfAddressList` | Parses an address list. **Returns:** The address list. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid address list. |
| `ToString()` | `string` | Formats the list in the modern form, such as `Mary Smith <mary@x.test>, jdoe@example.org`. An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8. A word right next to a period is the exception: the phrase is written as new encoded words, as only the obsolete form could keep it. Use `ToString` to choose. **Returns:** The list. |
| `ToString(ImfHeaderEncoding encoding)` | `string` | Formats the list in the modern form, for a writer with the given header encoding. <br><br> With [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md), a display name that isn't US-ASCII is written as RFC 2047 encoded words, in UTF-8, whatever form it was read in. With [`ImfHeaderEncoding.Utf8`](ImfHeaderEncoding.md), it is written as UTF-8 (RFC 6532), and an encoded word that was read is written as its text. <br><br> A local part or domain can't hold an encoded word (RFC 2047 §5), so one that isn't US-ASCII stays as it is, and a writer with [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md) refuses it. A control character in a display name has no form that can be written here, so it is left out. **Returns:** The list, unfolded. **Throws** `ArgumentOutOfRangeException`: `encoding` is not an [`ImfHeaderEncoding`](ImfHeaderEncoding.md) member. |
| `static TryParse(string? value, out ImfAddressList? result)` | `bool` | Parses a list of addresses. **Returns:** `true` if the value is a valid list of addresses; otherwise `false`, including for `null`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfAddressList? result)` | `bool` | Parses a list of addresses. **Returns:** `true` if the value is a valid list of addresses; otherwise `false`. |
| `static TryParse(string? value, ImfOptions options, out ImfAddressList? result)` | `bool` | Parses an address list. **Returns:** `true` if the value is a valid address list; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfAddressList? result)` | `bool` | Parses an address list. **Returns:** `true` if the value is a valid address list; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfAddressList? left, ImfAddressList? right)` | `bool` | Indicates whether two lists differ. |
| `operator ==(ImfAddressList? left, ImfAddressList? right)` | `bool` | Indicates whether two lists are equal. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfHeaderEncoding](ImfHeaderEncoding.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
