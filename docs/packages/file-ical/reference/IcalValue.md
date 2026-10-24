---
title: IcalValue Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalValue Class

## Definition

Namespace: `DataStandardizer.File.iCal`

Parses and formats the value types of RFC 5545 §3.3 that map to .NET types: BINARY,
BOOLEAN, CAL-ADDRESS, FLOAT, INTEGER, TEXT and URI.

**Syntax**

```csharp
public static class IcalValue
```

## Remarks

The other value types have types of their own, such as
[IcalDateTime](IcalDateTime.md) and [IcalRecurrenceRule](IcalRecurrenceRule.md).

Every member takes an [IcalValueFormat](IcalValueFormat.md): `Text` for `.ics`
content lines, `Json` for jCal and `Xml` for xCal. Each member throws
`ArgumentOutOfRangeException` if `format` isn't an `IcalValueFormat` member.

```csharp
IcalValue.TryParseInteger("+7".AsSpan(), IcalValueFormat.Text, out var priority);   // 7
var text = IcalValue.FormatText("Meeting; room 2", IcalValueFormat.Text);           // "Meeting\; room 2"
```

**Parse members** return `false` for a value that isn't valid, and don't throw for
it. **Format members** that write into a span return `false`, with `charsWritten` set
to 0, if the span is too short.

**Allocation.** `TryParseText` and `TryFormatText` with a span, `EnumerateTextList`,
`TryParseBoolean`, `TryFormatBoolean`, `TryFormatInteger`, and `TryParseInteger`
in `Text` and `Xml` form don't allocate. `TryParseBinary` allocates the byte array,
and `TryParseUri` the `Uri`. The overloads that return or take a `string` allocate
it.

**TEXT values** (RFC 5545 §3.3.11). In `Text` form, parsing removes the escaping:
`\\`, `\;` and `\,` become the escaped character, and `\n` or `\N` a line feed.
Formatting escapes `\`, `;` and `,` with a backslash and writes each line break
(CRLF, LF or a lone CR) as `\n`. A TEXT value can't hold a control character other
than a tab or a line break, or a lone surrogate, so formatting throws
`ArgumentException` for one. In `Json` and `Xml` form the text has no escaping, so it
is copied as it is, line breaks included. For a property that holds a list, such as
CATEGORIES, split the list with `EnumerateTextList` first, then parse each item.

**BOOLEAN values** (RFC 5545 §3.3.2). In `Text` form, `TRUE` or `FALSE` in any case,
formatted in upper case. In `Json` form, `true` or `false` in lower case
(RFC 7265 §3.6.2). In `Xml` form, an `xsd:boolean` (RFC 6321 §3.6.2): `true`,
`false`, `1` or `0`, with any white space around it. In `Json` and `Xml` form,
formatting writes `true` or `false`.

**INTEGER values** (RFC 5545 §3.3.8). An optional sign and one or more digits, from
-2147483648 to 2147483647. In `Json` form, a JSON number (RFC 7265 §3.6.8), which
has no plus sign or leading zeros, and may have a fraction or an exponent as long as
the number is a whole one, such as `1.0` or `1e2`. In `Xml` form, an `xsd:integer`
(RFC 6321 §3.6.8), which may have white space around it.

**FLOAT values** (RFC 5545 §3.3.7). An optional sign, one or more digits, and
optionally a decimal point and one or more digits, with no exponent. In `Xml` form, an
`xsd:float` (RFC 6321 §3.6.7), which may also have an exponent, such as `1.5E3`, a
decimal point with digits on only one side, and white space around it. `INF`, `-INF`
and `NaN` aren't valid. In `Json` form, a JSON number (RFC 7265 §3.6.7), which has no
plus sign or leading zeros, and may have an exponent. A number too large for a
`double` isn't valid in any form. Formatting writes the fewest digits that parse back
to the same number, with no exponent, in every form.

**BINARY values** (RFC 5545 §3.3.1). The base64 encoding of RFC 4648, with padding and
no line breaks. In `Xml` form, white space may appear anywhere in the value to wrap
it (RFC 6321 §3.6.1), and is ignored.

**URI and CAL-ADDRESS values** (RFC 5545 §3.3.13 and §3.3.3). An absolute URI, which
starts with a scheme and is made of printable ASCII characters. A CAL-ADDRESS is a URI
identifying a calendar user, usually a `mailto` URI. In `Xml` form, an `xsd:anyURI`
(RFC 6321 §3.6.13), which may have white space around it. Formatting writes the URI
as it was given (`Uri.OriginalString`), in every form.

## Methods

### TEXT

| Method | Returns | Notes |
| --- | --- | --- |
| `TryParseText(ReadOnlySpan<char> value, IcalValueFormat format, Span<char> destination, out int charsWritten)` | `bool` | Parses a TEXT value into `destination`, removing its escaping. `destination` must be at least as long as `value`, which is always enough. Returns `false`, with `charsWritten` set to 0, if a backslash doesn't start a valid escape. Throws `ArgumentException` if `destination` is shorter than `value`. |
| `TryParseText(ReadOnlySpan<char> value, IcalValueFormat format, out string? result)` | `bool` | Parses a TEXT value into a string. Returns `false`, with `result` set to `null`, if a backslash doesn't start a valid escape. |
| `TryFormatText(ReadOnlySpan<char> value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats text as a TEXT value. Throws `ArgumentException` if `value` holds a control character other than a tab or a line break, or a lone surrogate. In `Xml` form, also throws it for a character that XML can't hold, such as U+FFFE. |
| `FormatText(string value, IcalValueFormat format)` | `string` | Formats text as a TEXT value. Throws `ArgumentNullException` if `value` is `null`, or `ArgumentException` as `TryFormatText` does. |
| `EnumerateTextList(ReadOnlySpan<char> value, IcalValueFormat format)` | `IcalTextListEnumerator` | Splits a list of TEXT values, such as `APPOINTMENT,EDUCATION\, TRAINING`, on the commas that aren't escaped. Each item keeps its escaping. An empty value is one empty item. In `Json` and `Xml` form, the value is a single item. See [IcalTextListEnumerator](IcalTextListEnumerator.md). |

### BOOLEAN

| Method | Returns | Notes |
| --- | --- | --- |
| `TryParseBoolean(ReadOnlySpan<char> value, IcalValueFormat format, out bool result)` | `bool` | Parses a BOOLEAN value. |
| `TryFormatBoolean(bool value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats a BOOLEAN value: `TRUE` or `FALSE` in `Text` form, `true` or `false` in `Json` and `Xml` form. |

### INTEGER

| Method | Returns | Notes |
| --- | --- | --- |
| `TryParseInteger(ReadOnlySpan<char> value, IcalValueFormat format, out int result)` | `bool` | Parses an INTEGER value. Returns `false` for a value outside the range of an `int`. |
| `TryFormatInteger(int value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats an INTEGER value, with a minus sign if negative and no plus sign. The same in every form. |

### FLOAT

| Method | Returns | Notes |
| --- | --- | --- |
| `TryParseFloat(ReadOnlySpan<char> value, IcalValueFormat format, out double result)` | `bool` | Parses a FLOAT value, such as `-3.14`. Returns `false` for a value too large for a `double`. |
| `TryFormatFloat(double value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats a FLOAT value with the fewest digits that parse back to the same number, and no exponent. Throws `ArgumentOutOfRangeException` if `value` is NaN or infinite. |
| `FormatFloat(double value, IcalValueFormat format)` | `string` | As `TryFormatFloat`, returning a string. Throws `ArgumentOutOfRangeException` if `value` is NaN or infinite. |

### BINARY

| Method | Returns | Notes |
| --- | --- | --- |
| `TryParseBinary(ReadOnlySpan<char> value, IcalValueFormat format, out byte[]? result)` | `bool` | Parses a BINARY value into bytes. Returns `false`, with `result` set to `null`, if the length isn't a multiple of 4 or a character isn't in the base64 alphabet. |
| `TryFormatBinary(ReadOnlySpan<byte> value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats bytes as a BINARY value, with padding. The same in every form. |
| `FormatBinary(byte[] value, IcalValueFormat format)` | `string` | As `TryFormatBinary`, returning a string. Throws `ArgumentNullException` if `value` is `null`. |

### URI and CAL-ADDRESS

| Method | Returns | Notes |
| --- | --- | --- |
| `TryParseUri(ReadOnlySpan<char> value, IcalValueFormat format, out Uri? result)` | `bool` | Parses a URI value, such as `http://example.com/my-report.txt`. Returns `false`, with `result` set to `null`, for a value that isn't an absolute URI. A file path isn't taken as a `file` URI. |
| `TryFormatUri(Uri value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats a URI value as the URI was given. Throws `ArgumentNullException` if `value` is `null`, or `ArgumentException` if it isn't absolute. |
| `TryParseCalAddress(ReadOnlySpan<char> value, IcalValueFormat format, out Uri? result)` | `bool` | Parses a CAL-ADDRESS value, such as `mailto:jane_doe@example.com`, as `TryParseUri` does. |
| `TryFormatCalAddress(Uri value, Span<char> destination, out int charsWritten, IcalValueFormat format)` | `bool` | Formats a CAL-ADDRESS value as `TryFormatUri` does, with the same exceptions. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
Every form, `Json` included, is supported on every target. On the `netstandard2.1`,
`net8.0`, and `net10.0` builds, the `out` parameters of `TryParseText` (string
overload), `TryParseBinary`, `TryParseUri` and `TryParseCalAddress` are nullable and
annotated `[NotNullWhen(true)]`.

## See also

- [Read and write xCal](../how-to/read-write-xcal.md)
- [Read and write jCal](../how-to/read-write-jcal.md)
- [Read without allocating](../how-to/read-zero-copy.md)
- [IcalTextListEnumerator](IcalTextListEnumerator.md)
- [IcalValueFormat](IcalValueFormat.md)
- [IcalValueType](IcalValueType.md)
- [IcalValueTypeRegistry](IcalValueTypeRegistry.md)
- [File.iCal API reference](index.md)
