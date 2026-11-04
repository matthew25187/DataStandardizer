---
title: ImfMailbox Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMailbox Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A mailbox (RFC 5322 §3.4): an optional display name, and an address made of a local part and a domain.

**Syntax**

```csharp
public sealed class ImfMailbox : ImfAddress, IEquatable<ImfMailbox>
```

## Remarks

The local part and domain hold what the address stands for, whatever form it was written in. `"jdoe"@example.org` and `jdoe@example . org` have the same local part and domain as `jdoe@example.org`, and the source route of the obsolete form `<@node.test:mary@example.net>` is dropped.

`ToString` writes the modern form. The obsolete syntax alone can put a control character in a quoted-string or domain-literal, or a bracket or backslash in a domain-literal (RFC 5322 §4.1 and §4.4). Such a character is parsed, but has no modern form, so it is left out when written.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfMailbox(string localPart, string domain)` | Initializes a new mailbox with no display name. **Throws** `ArgumentNullException`: `localPart` or `domain` is `null`. **Throws** `ArgumentException`: `localPart` holds a control character other than tab or an unpaired surrogate, or `domain` is not a valid domain (RFC 5322 §3.4.1). |
| `ImfMailbox(string displayName, string localPart, string domain)` | Initializes a new mailbox. **Throws** `ArgumentNullException`: `displayName`, `localPart` or `domain` is `null`. **Throws** `ArgumentException`: `displayName` or `localPart` holds a control character other than tab or an unpaired surrogate, or `domain` is not a valid domain (RFC 5322 §3.4.1). |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Address` | `string Address { get; }` | Gets the addr-spec in the modern form, with no display name or angle brackets, such as `mary@example.net`. |
| `DisplayName` | `string DisplayName { get; }` | The display name, decoded from any RFC 2047 encoded words, or `null` if it has none. |
| `Domain` | `string Domain { get; }` | Gets the domain: a dot-atom, or a domain-literal with its brackets. |
| `LocalPart` | `string LocalPart { get; }` | Gets the local part, unquoted. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfMailbox? other)` | `bool` | Indicates whether this mailbox equals another. The display name and local part are compared case-sensitively and the domain case-insensitively. Display names are compared as text, so `=?UTF-8?Q?a?=` and `a` are equal. Use `HasSameAddress` to ignore the display name. |
| `Equals(ImfAddress? other)` | `bool` | Whether the other value is equal to this one. |
| `HasSameAddress(ImfMailbox? other)` | `bool` | Indicates whether this mailbox has the same address as another, whatever their display names. The local part is compared case-sensitively and the domain case-insensitively. **Returns:** `true` if the addresses are the same; `false` if not, or if `other` is `null`. |
| `static Parse(string value)` | `ImfMailbox` | Parses a mailbox. **Returns:** The mailbox. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid mailbox. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfMailbox` | Parses a mailbox. **Returns:** The mailbox. **Throws** `FormatException`: The value is not a valid mailbox. |
| `static Parse(string value, ImfOptions options)` | `ImfMailbox` | Parses a mailbox. **Returns:** The mailbox. **Throws** `ArgumentNullException`: `value` or `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid mailbox. |
| `static Parse(ReadOnlySpan<byte> value, ImfOptions options)` | `ImfMailbox` | Parses a mailbox. **Returns:** The mailbox. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** `FormatException`: The value is not a valid mailbox. |
| `ToString()` | `string` | Formats the mailbox in the modern form, such as `Mary Smith <mary@example.net>` or `mary@example.net`. An encoded word stays as it was read, and any other text is written as it is, which may be UTF-8. A word right next to a period is the exception: the phrase is written as new encoded words, as only the obsolete form could keep it. Use [`ImfAddress.ToString`](ImfAddress.md) to choose. **Returns:** The mailbox. |
| `static TryParse(string? value, out ImfMailbox? result)` | `bool` | Parses a mailbox. **Returns:** `true` if the value is a valid mailbox; otherwise `false`, including for `null`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfMailbox? result)` | `bool` | Parses a mailbox. **Returns:** `true` if the value is a valid mailbox; otherwise `false`. |
| `static TryParse(string? value, ImfOptions options, out ImfMailbox? result)` | `bool` | Parses a mailbox. **Returns:** `true` if the value is a valid mailbox; otherwise `false`, including for `null`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |
| `static TryParse(ReadOnlySpan<byte> value, ImfOptions options, out ImfMailbox? result)` | `bool` | Parses a mailbox. **Returns:** `true` if the value is a valid mailbox; otherwise `false`. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or a [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfAddress](ImfAddress.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
