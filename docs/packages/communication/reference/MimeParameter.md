---
title: MimeParameter Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeParameter Struct

## Definition

Namespace: `DataStandardizer.Communication.Mime`

A media type parameter (RFC 2045 §5.1): a name, its decoded value, and the language of
the value, if any (RFC 2231).

**Syntax**

```csharp
public readonly struct MimeParameter : IEquatable<MimeParameter>
```

## Remarks

**Formatting.** A value of printable US-ASCII characters, spaces and horizontal tabs,
with no language, is written as a token when it is one, and otherwise as a
quoted-string. Any other value, or a value with a language, is written as an RFC 2231
extended value in UTF-8, such as `title*=UTF-8'en'%C2%A3%20rates`.

**Equality.** Names and languages are compared case-insensitively. Values are compared
case-sensitively, except for the value of `charset` (RFC 2046 §4.1.2). The character set
that an extended value was written in isn't compared, as it doesn't change the value.

See [Use RFC 2231 parameter values](../how-to/use-rfc-2231-parameters.md).

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeParameter(string name, string value)` | Throws `ArgumentException` if the name isn't valid, or the value has an unpaired surrogate. |
| `MimeParameter(string name, string value, string language)` | With the language of the value, which is written as an RFC 2231 extended value. `language` must be an RFC 1766 tag (RFC 2231 §4), so `es-419` is rejected. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Name` | `string` | The name. |
| `Value` | `string` | The value, with any quoting removed, its sections joined and any RFC 2231 encoding decoded. |
| `Charset` | `string` | The character set an extended value was written in, when parsed; or an empty string. The value is already decoded from it. |
| `Language` | `string` | The language of the value; or an empty string. It's a `string` because RFC 2231 §4 calls for RFC 1766 tags, not BCP 47 tags. |

The properties throw `InvalidOperationException` on a value that isn't initialised.

## Methods

| Method | Notes |
| --- | --- |
| `bool TryFormat(Span<char> destination, out int charsWritten)` | Writes `name=value`, or `name*=UTF-8'language'value` for an extended value. Throws `InvalidOperationException` if the value isn't initialised. |
| `string ToString()` | As `TryFormat`; an empty string if the value isn't initialised. |
| `IReadOnlyList<string> FormatSections(int maxSectionLength)` | The parameter as RFC 2231 sections of at most the given length, or as `ToString` writes it if it fits. A section is longer only when its name and a single character of the value don't fit. Throws `ArgumentOutOfRangeException` if the length is less than 1. |

The type also has `Equals`, `GetHashCode`, and the `==` and `!=` operators.
