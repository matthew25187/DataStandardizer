---
title: ImfGroup Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfGroup Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A group (RFC 5322 §3.4): a display name and a list of mailboxes, which may be empty, such as `Undisclosed recipients:;`.

**Syntax**

```csharp
public sealed class ImfGroup : ImfAddress, IEquatable<ImfGroup>
```

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfGroup(string displayName, IEnumerable<ImfMailbox> mailboxes)` | Initializes a new group. **`mailboxes`:** The mailboxes, in order, which are copied. There may be none. **Throws** `ArgumentNullException`: `displayName` or `mailboxes` is `null`. **Throws** `ArgumentException`: `displayName` holds a control character other than tab or an unpaired surrogate, or `mailboxes` holds a `null` element. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `DisplayName` | `string DisplayName { get; }` | Gets the display name, or an empty string if a mailbox has none. |
| `Mailboxes` | `IReadOnlyList<ImfMailbox> Mailboxes { get; }` | Gets the mailboxes of the group, in order. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfGroup? other)` | `bool` | Indicates whether this group equals another. The display name is compared case-sensitively, and the mailboxes in order, as [`ImfMailbox.Equals(ImfMailbox)`](ImfMailbox.md) does. |
| `Equals(ImfAddress? other)` | `bool` | Whether the other value is equal to this one. |
| `static Parse(string value)` | `ImfGroup` | Parses a group. **Returns:** The group. **`value`:** The group, which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid group. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfGroup` | Parses a group. **Returns:** The group. **`value`:** The group as UTF-8 (RFC 6532), which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. **Throws** `FormatException`: The value is not a valid group. |
| `static Parse(string value, ImfOptions options)` | `ImfGroup` | Parses a group. **Returns:** The group. **`value`:** The group, which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid group. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfGroup` | Parses a group. **Returns:** The group. **`value`:** The group, which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid group. |
| `ToString()` | `string` | Formats the group in the modern form, such as `Friends: Ed <c@a.test>, joe@where.test;` or `Undisclosed recipients:;`. An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8. A word right next to a period is the exception: the phrase is written as new encoded words, as only the obsolete form could keep it. Use [`ImfAddress.ToString`](ImfAddress.md) to choose. **Returns:** The group. |
| `static TryParse(string? value, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`, including for `null`. **`value`:** The group, which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`. **`value`:** The group as UTF-8 (RFC 6532), which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. |
| `static TryParse(string? value, ImfOptions options, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`, including for `null`. **`value`:** The group, which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. **`options`:** The options, of which this uses [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) and [`ImfOptions.CharsetResolver`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`. **`value`:** The group, which may be folded, such as `Friends: Ed <c@a.test>, joe@where.test;`. Bytes that aren't valid UTF-8 are decoded with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) before any encoded word is read, if [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) is `true`. **`options`:** The options, of which this uses [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md), [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md), [`ImfOptions.CharsetResolver`](ImfOptions.md) and [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfAddress](ImfAddress.md)
- [ImfMailbox](ImfMailbox.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
