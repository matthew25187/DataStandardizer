---
title: MimeMediaType Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeMediaType Struct

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A media type and its parameters, as in a Content-Type header field (RFC 2045 §5.1),
such as `text/plain;charset=us-ascii`.

**Syntax**

```csharp
public readonly struct MimeMediaType : IEquatable<MimeMediaType>, ISpanFormattable, ISpanParsable<MimeMediaType>
```

`ISpanFormattable` and `ISpanParsable<T>` are on the `net8.0` and `net10.0` builds only.

## Remarks

**Parsing.** By default, a media type is parsed by the RFC 2045 §5.1 grammar, with no
whitespace or comments, and its type, subtype and parameter names must follow the
RFC 6838 §4.2 naming rules. [MimeMediaTypeParseOptions](MimeMediaTypeParseOptions.md)
names each accommodation.

**RFC 2231.** When parsed, a value split into sections is joined, and an extended value
is decoded, so that each parameter appears once with its whole value. When a parameter
is given both as `name` and in an RFC 2231 form such as `name*`, the RFC 2231 form is
used.

**Formatting.** A media type is written with no whitespace, and with each parameter
value as a token if it is one, or otherwise as a quoted-string. A value that isn't
US-ASCII, or that has a language, is written as an RFC 2231 extended value.

**Equality.** The type, subtype and parameter names are compared case-insensitively.
Parameter values are compared case-sensitively, except for the value of `charset`. The
order of the parameters doesn't matter.

See [Use media types](../how-to/use-media-types.md).

## Fields

| Field | Value | Standard |
| --- | --- | --- |
| `TextPlain` | `text/plain` | RFC 2046 §4.1.3 |
| `TextCalendar` | `text/calendar` | RFC 5545 §8.1 |
| `TextVcard` | `text/vcard` | RFC 6350 §10.1 |
| `TextDirectory` | `text/directory` | RFC 2425 §3 |
| `MessageRfc822` | `message/rfc822` | RFC 2046 §5.2.1 |
| `MultipartMixed` | `multipart/mixed` | RFC 2046 §5.1.3 |
| `MultipartAlternative` | `multipart/alternative` | RFC 2046 §5.1.4 |
| `MultipartRelated` | `multipart/related` | RFC 2387 |
| `ApplicationOctetStream` | `application/octet-stream` | RFC 2046 §4.5.1 |

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeMediaType(string type, string subtype)` | No parameters. Throws `ArgumentException` if a name isn't valid. |
| `MimeMediaType(string type, string subtype, IEnumerable<MimeParameter> parameters)` | With parameters. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Type` | `string` | The top-level type, such as `text`. |
| `Subtype` | `string` | The subtype, such as `plain`. |
| `Suffix` | `string` | The structured syntax suffix (RFC 6839), such as `json` in `application/ld+json`; or an empty string. |
| `Tree` | [MimeRegistrationTree](MimeRegistrationTree.md) | The registration tree, from the prefix of the subtype (RFC 6838 §3). |
| `Parameters` | `IReadOnlyList<MimeParameter>` | The parameters, in the order they're written. |

The properties throw `InvalidOperationException` on a value that isn't initialised.

## Methods

| Method | Notes |
| --- | --- |
| `bool TryGetParameter(string name, out MimeParameter parameter)` | Finds a parameter by name, compared case-insensitively. |
| `MimeMediaType WithParameter(MimeParameter parameter)` | A copy with the parameter added, or with the value of the parameter of the same name replaced. |
| `MimeMediaType WithParameter(string name, string value)` | As above. A replaced parameter keeps its position. |
| `MimeMediaType WithoutParameter(string name)` | A copy without the parameter. |
| `MimeMediaType WithoutParameters()` | A copy without any parameters, such as to compare only the type and subtype. |
| `static bool TryParse(value, out MimeMediaType result)` | Strict. `value` is a `string` or `ReadOnlySpan<char>`. |
| `static bool TryParse(value, MimeMediaTypeParseOptions options, out MimeMediaType result)` | With accommodations. |
| `static bool TryParse(value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver, out MimeMediaType result)` | With a resolver for RFC 2231 extended values. |
| `static MimeMediaType Parse(value)` | Strict. Throws `FormatException` if the value isn't valid. |
| `static MimeMediaType Parse(value, MimeMediaTypeParseOptions options)` | With accommodations. |
| `static MimeMediaType Parse(value, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver)` | With a resolver for RFC 2231 extended values. |
| `bool TryFormat(Span<char> destination, out int charsWritten)` | Writes nothing if the value isn't initialised. |
| `string ToString()` | An empty string if the value isn't initialised. |

The type also has `Equals`, `GetHashCode`, and the `==` and `!=` operators.
