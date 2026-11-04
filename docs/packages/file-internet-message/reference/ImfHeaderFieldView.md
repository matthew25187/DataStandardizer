---
title: ImfHeaderFieldView Struct
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderFieldView Struct

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A view of the header field an [`ImfHeaderReader`](ImfHeaderReader.md) or [`ImfEntityReader`](ImfEntityReader.md) has just read, without allocation.

**Syntax**

```csharp
public readonly struct ImfHeaderFieldView
```

## Remarks

A view is valid only until the reader reads again or is disposed. After that, every member throws: `ObjectDisposedException` once the reader is disposed, otherwise `InvalidOperationException`.

A span or memory taken out of the view is valid for the same period, but that cannot be checked: using one later gives undefined content. Copy anything that must outlive the field.

The value of a folded field is unfolded when first asked for and kept until the reader moves on, so asking again costs nothing. The value of a field with no folds is a slice of the input itself.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the 0-based offset, in bytes from the start of the data, of the first byte of the field. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `LineNumber` | `long LineNumber { get; }` | Gets the 1-based number of the physical line on which the field starts. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `Name` | `ReadOnlySpan<byte> Name { get; }` | Gets the field name, as written, without any white space before the colon. Field names are case-insensitive. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `NameMemory` | `ReadOnlyMemory<byte> NameMemory { get; }` | Gets the field name, as written, without any white space before the colon. Field names are case-insensitive. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `NeedsHeaderFallbackCharset` | `bool NeedsHeaderFallbackCharset { get; }` | Gets a flag indicating that the field body is not valid UTF-8, so `GetValue` decodes it with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md). It can be `true` only with [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md). **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `RawBytes` | `ReadOnlySpan<byte> RawBytes { get; }` | Gets the whole field exactly as it appears in the data: its name, any white space before the colon, its folds and its final line break. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `RawBytesMemory` | `ReadOnlyMemory<byte> RawBytesMemory { get; }` | Gets the whole field exactly as it appears in the data: its name, any white space before the colon, its folds and its final line break. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `Value` | `ReadOnlySpan<byte> Value { get; }` | Gets the field body, unfolded (RFC 5322 §2.2.3): every byte after the colon, without the line breaks of folds and the final line break. White space is kept, including any after the colon, so the value of `Subject: Hi` starts with a space. The syntax of the body, and the obsolete forms of RFC 5322 §4, are not checked here. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `ValueMemory` | `ReadOnlyMemory<byte> ValueMemory { get; }` | Gets the field body, unfolded (RFC 5322 §2.2.3): every byte after the colon, without the line breaks of folds and the final line break. White space is kept, including any after the colon, so the value of `Subject: Hi` starts with a space. The syntax of the body, and the obsolete forms of RFC 5322 §4, are not checked here. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetDecodedValue()` | `string` | Decodes `Value` as the body of an unstructured field, such as Subject or Comments, or a field of unknown meaning: as `GetValue` does, and then without the white space at either end, and with each RFC 2047 encoded word decoded and the white space between adjacent encoded words removed (RFC 2047 §6.2). <br><br> This is what [`ImfHeaderText.DecodeUnstructured(string, ImfOptions)`](ImfHeaderText.md) does, with the options of the reader. A word that can't be decoded stays as ordinary text, as [`ImfOptions.AllowMalformedEncodedWords`](ImfOptions.md) describes. <br><br> The result can hold anything that the words encode, including control characters and line breaks. A structured field has encoded words only in some places (RFC 2047 §5), so parse one with its type, such as [`ImfAddressList`](ImfAddressList.md), and get its comments with [`ImfHeaderText.GetComments(string, ImfOptions)`](ImfHeaderText.md). **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. -or- The options of the reader have no [`ImfOptions.CharsetResolver`](ImfOptions.md). |
| `GetName()` | `string` | Decodes `Name` into a string. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |
| `GetValue()` | `string` | Decodes `Value` into a string: as UTF-8 (RFC 6532), or with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) if `NeedsHeaderFallbackCharset` is `true`. RFC 2047 encoded words are left as they are. **Throws** `ObjectDisposedException`: The reader has been disposed. **Throws** `InvalidOperationException`: The reader has moved on to another field, or the view was not obtained from a reader. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Edit one header without breaking a signature](../how-to/edit-a-header.md)
- [The layers](../concepts/layers.md)
- [ImfAddressList](ImfAddressList.md)
- [ImfEntityReader](ImfEntityReader.md)
- [ImfHeaderReader](ImfHeaderReader.md)
- [ImfHeaderText](ImfHeaderText.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
