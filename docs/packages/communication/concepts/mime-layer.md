---
title: The MIME layer
parent: Communication
grand_parent: Packages
nav_order: 11
---

# The MIME layer

The `DataStandardizer.Communication.Mime` namespace holds the building blocks that
file formats and message formats share: media types, transfer encodings, base64 and
quoted-printable, character sets, `data:` URIs, encoded words and RFC 2231 parameter
values.

## Values and codecs, not messages

The layer works on strings and spans. It holds values, such as `MimeMediaType`, and
codecs, such as `MimeBase64`. It has no stream readers or writers, and it doesn't read
or write a whole message. So these are up to the caller:

- the RFC 5322 grammar of a header field, including unfolding it and removing comments
- addresses, dates and Message-ID
- MIME-Version, Content-ID, Content-Description and Content-Disposition
- multipart bodies and their boundaries (RFC 2046 §5)

A value type takes the value of a header field, once you've taken it out of the field.
`MimeMediaType` takes `text/plain;charset=UTF-8`, not `Content-Type: text/plain;
charset=UTF-8`.

## Which standard each type implements

| Type | Standard |
| --- | --- |
| [MimeMediaType](../reference/MimeMediaType.md), [MimeParameter](../reference/MimeParameter.md) | RFC 2045 §5.1, RFC 2046, RFC 6838, RFC 6839 and RFC 2231 |
| [MimeTransferEncoding](../reference/MimeTransferEncoding.md) | RFC 2045 §6, RFC 4289 |
| [MimeBase64](../reference/MimeBase64.md) | RFC 2045 §6.8, RFC 4648 §4 |
| [MimeQuotedPrintable](../reference/MimeQuotedPrintable.md) | RFC 2045 §6.7, RFC 2047 §4.2 |
| [MimeCharset](../reference/MimeCharset.md), [MimeCharsetResolver](../reference/MimeCharsetResolver.md) | RFC 2046 §4.1.2, RFC 2978 §2.3, RFC 6657 |
| [MimeDataUri](../reference/MimeDataUri.md) | RFC 2397 |
| [MimeEncodedWord](../reference/MimeEncodedWord.md) | RFC 2047, RFC 2231 §5 |

## Strict by default

Every type does what its standard says. A parser rejects what the standard doesn't allow,
and a writer writes only what it allows. That holds even when other software commonly
accepts the input.

Real mail often breaks the rules, so each accommodation has a named flag that you opt in
to, and whose documentation names the rule it relaxes:

- [MimeMediaTypeParseOptions](../reference/MimeMediaTypeParseOptions.md)
- [MimeEncodedWordParseOptions](../reference/MimeEncodedWordParseOptions.md)
- [MimeBase64DecoderOptions](../reference/MimeBase64DecoderOptions.md), which adds a
  check rather than relaxing one
- [MimeQuotedPrintableDecoderOptions](../reference/MimeQuotedPrintableDecoderOptions.md)

There's no general lenient mode. Pass only the flags for the problems you expect to meet.

## Conventions

The value types share these conventions. `MimeParameter` is read through `MimeMediaType`,
so it has no parse methods and doesn't implement the interfaces.

- **Parsing.** `TryParse` returns `false` for an invalid value, and `Parse` throws
  `FormatException`. Each takes a `string` or a `ReadOnlySpan<char>`.
- **Formatting.** `ToString` and `TryFormat` write the value. A parsed media type,
  character set, data URI or encoded word keeps the case it was written in. A known
  transfer encoding is written in lower case.
- **Equality.** `Equals`, `==` and `!=` compare what the standard says is significant.
  This is often not the same as comparing the text. Each type's page says how it compares.
- **Default values.** A `default` value isn't initialised. Its properties throw
  `InvalidOperationException`, `ToString` returns an empty string, and `TryFormat`
  writes nothing, except on `MimeParameter`, where it throws.
- **Interfaces.** The `net8.0` and `net10.0` builds of the value types also implement
  `ISpanFormattable` and `ISpanParsable<T>`. Their `IFormatProvider` and format arguments
  are ignored.

On .NET Standard and .NET Framework, a `string` doesn't convert to `ReadOnlySpan<char>`
implicitly. Call `AsSpan()` on it, as the examples for the codecs do.

## Dependencies

The package doesn't depend on System.Text.Encoding.CodePages. See
[Resolve character sets](../how-to/resolve-character-sets.md) for how to add the code
pages.
