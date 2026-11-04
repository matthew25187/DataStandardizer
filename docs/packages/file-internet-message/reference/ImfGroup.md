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
| `ImfGroup(string displayName, IEnumerable<ImfMailbox> mailboxes)` | Initializes a new group. **Throws** `ArgumentNullException`: `displayName` or `mailboxes` is `null`. **Throws** `ArgumentException`: `displayName` holds a control character other than tab or an unpaired surrogate, or `mailboxes` holds a `null` element. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `DisplayName` | `string DisplayName { get; }` | The display name, decoded from any RFC 2047 encoded words, or `null` if it has none. |
| `Mailboxes` | `IReadOnlyList<ImfMailbox> Mailboxes { get; }` | Gets the mailboxes of the group, in order. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfGroup? other)` | `bool` | Indicates whether this group equals another. The display name is compared case-sensitively, and the mailboxes in order, as [`ImfMailbox.Equals`](ImfMailbox.md) does. |
| `Equals(ImfAddress? other)` | `bool` | Whether the other value is equal to this one. |
| `static Parse(string value)` | `ImfGroup` | Parses a group. **Returns:** The group. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid group. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfGroup` | Parses a group. **Returns:** The group. **Throws** `FormatException`: The value is not a valid group. |
| `static Parse(string value, ImfOptions options)` | `ImfGroup` | Parses a group. **Returns:** The group. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid group. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfGroup` | Parses a group. **Returns:** The group. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid group. |
| `ToString()` | `string` | Formats the group in the modern form, such as `Friends: Ed <c@a.test>, joe@where.test;` or `Undisclosed recipients:;`. An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8. A word right next to a period is the exception: the phrase is written as new encoded words, as only the obsolete form could keep it. Use [`ImfAddress.ToString`](ImfAddress.md) to choose. **Returns:** The group. |
| `static TryParse(string? value, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`, including for `null`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`. |
| `static TryParse(string? value, ImfOptions options, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfGroup? result)` | `bool` | Parses a group. **Returns:** `true` if the value is a valid group; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfAddress](ImfAddress.md)
- [ImfMailbox](ImfMailbox.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
