---
title: ImfMessageId Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfMessageId Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A message identifier (RFC 5322 §3.6.4), as in a Message-ID, In-Reply-To or References header field, such as `<1234@local.machine.example>`.

**Syntax**

```csharp
public sealed class ImfMessageId : IEquatable<ImfMessageId>
```

## Remarks

The two parts hold what the identifier stands for, whatever form it was written in: `<"a" . b@c . d>` has the same parts as `<a.b@c.d>`. Both parts are compared case-sensitively.

`ToString` writes the modern form. An identifier read from the obsolete syntax (RFC 5322 §4.5.4) may have a left part that is not a dot-atom, or a right part with white space in its brackets. Neither has a modern form, and changing them would make it a different identifier, so those parts are written as a quoted-string or domain-literal. Any control character in them is left out.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfMessageId(string idLeft, string idRight)` | Initializes a new message identifier. **`idRight`:** The part after the `@`, as a dot-atom or a literal with its brackets and no white space. **Throws** `ArgumentNullException`: `idLeft` or `idRight` is `null`. **Throws** `ArgumentException`: `idLeft` or `idRight` is not valid (RFC 5322 §3.6.4). |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `IdLeft` | `string IdLeft { get; }` | Gets the part before the `@`, unquoted. |
| `IdRight` | `string IdRight { get; }` | Gets the part after the `@`: a dot-atom, or a literal with its brackets. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Equals(ImfMessageId? other)` | `bool` | Indicates whether this message identifier equals another: both parts are the same, compared case-sensitively. |
| `static Parse(string value)` | `ImfMessageId` | Parses a message identifier. **Returns:** The message identifier. **`value`:** The identifier, with its angle brackets, such as `<1234@local.machine.example>`. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid message identifier. |
| `static Parse(ReadOnlySpan<byte> value)` | `ImfMessageId` | Parses a message identifier. **Returns:** The message identifier. **`value`:** The identifier as UTF-8 (RFC 6532), with its angle brackets, such as `<1234@local.machine.example>`. **Throws** `FormatException`: The value is not a valid message identifier. |
| `static ParseCidUri(string value)` | `ImfMessageId` | Converts a `cid:` URI (RFC 2392 §2) to the identifier in the Content-ID field of the entity that it refers to. **Returns:** The identifier, such as `<foo4%foo1@bar.net>`. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not valid, as `TryParseCidUri` describes. |
| `static ParseList(string value)` | `IReadOnlyList<ImfMessageId>` | Parses the message identifiers of an In-Reply-To or References header field. **Returns:** The message identifiers, in order. There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which also allows phrases between them. Phrases have no meaning, so they are not kept. **`value`:** The identifiers, such as `<1234@local.machine.example> <3456@example.net>`. **Throws** `ArgumentNullException`: `value` is `null`. **Throws** `FormatException`: The value is not a valid list of message identifiers. |
| `static ParseList(ReadOnlySpan<byte> value)` | `IReadOnlyList<ImfMessageId>` | Parses the message identifiers of an In-Reply-To or References header field. **Returns:** The message identifiers, in order. There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which also allows phrases between them. Phrases have no meaning, so they are not kept. **`value`:** The identifiers as UTF-8 (RFC 6532), such as `<1234@local.machine.example> <3456@example.net>`. **Throws** `FormatException`: The value is not a valid list of message identifiers. |
| `ToCidUri()` | `string` | Converts the identifier in a Content-ID field to the `cid:` URI that refers to the entity (RFC 2392 §2), such as `cid:foo4%25foo1@bar.net`. Any character that a URL can't hold, or that has a special meaning in one, is written as a `%` escape (RFC 1738 §2.2). **Returns:** The URI. **Throws** `InvalidOperationException`: The identifier isn't US-ASCII, as only RFC 6532 allows, or holds a control character other than tab, as only the obsolete syntax allows, so it has no `cid:` URI that gives the same identifier back. |
| `ToString()` | `string` | Formats the message identifier with its angle brackets, such as `<1234@local.machine.example>`. **Returns:** The message identifier. |
| `static TryParse(string? value, out ImfMessageId? result)` | `bool` | Parses a message identifier. **Returns:** `true` if the value is a valid message identifier; otherwise `false`, including for `null`. **`value`:** The identifier, with its angle brackets, such as `<1234@local.machine.example>`. |
| `static TryParse(ReadOnlySpan<byte> value, out ImfMessageId? result)` | `bool` | Parses a message identifier. **Returns:** `true` if the value is a valid message identifier; otherwise `false`. **`value`:** The identifier as UTF-8 (RFC 6532), with its angle brackets, such as `<1234@local.machine.example>`. |
| `static TryParseCidUri(string? value, out ImfMessageId? result)` | `bool` | Converts a `cid:` URI (RFC 2392 §2) to the identifier in the Content-ID field of the entity that it refers to. As RFC 2392 §2 says, this removes the scheme, converts each `%` escape to its US-ASCII character, and puts the rest in angle brackets. The example there shows the escape as still in the Content-ID field, which disagrees with what it says, so this does what it says. An escape of an octet above 127 isn't valid, nor is a character that RFC 1738 §2.2 says must be escaped, such as `~` or `#`. The scheme is compared case-insensitively. **Returns:** `true` if the value is valid; otherwise `false`, including for `null`. **`result`:** The identifier, such as `<foo4%foo1@bar.net>`, if the value is valid. |
| `static TryParseList(string? value, out IReadOnlyList<ImfMessageId>? result)` | `bool` | Parses the message identifiers of an In-Reply-To or References header field. **Returns:** `true` if the value is a valid list of message identifiers; otherwise `false`, including for `null`. **`value`:** The identifiers, such as `<1234@local.machine.example> <3456@example.net>`. **`result`:** The message identifiers, in order, if the value is valid. There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which also allows phrases between them. Phrases have no meaning, so they are not kept. |
| `static TryParseList(ReadOnlySpan<byte> value, out IReadOnlyList<ImfMessageId>? result)` | `bool` | Parses the message identifiers of an In-Reply-To or References header field. **Returns:** `true` if the value is a valid list of message identifiers; otherwise `false`. **`value`:** The identifiers as UTF-8 (RFC 6532), such as `<1234@local.machine.example> <3456@example.net>`. **`result`:** The message identifiers, in order, if the value is valid. There may be none, as the obsolete syntax allows (RFC 5322 §4.5.4), which also allows phrases between them. Phrases have no meaning, so they are not kept. |

## Operators

| Operator | Returns | Notes |
| --- | --- | --- |
| `operator !=(ImfMessageId? left, ImfMessageId? right)` | `bool` | Indicates whether two message identifiers differ. |
| `operator ==(ImfMessageId? left, ImfMessageId? right)` | `bool` | Indicates whether two message identifiers are equal. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [File.InternetMessage API reference](index.md)
