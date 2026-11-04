---
title: ImfHeaderField Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfHeaderField Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

One field of the header of an Internet message: either what a reader read, which is written back byte for byte, or a name and body that a writer formats. A field doesn't change. To change a field of a message, replace it in its [`ImfHeaderFieldCollection`](ImfHeaderFieldCollection.md).

**Syntax**

```csharp
public sealed class ImfHeaderField
```

## Constructors

| Constructor | Notes |
| --- | --- |
| `ImfHeaderField(ImfHeaderFieldView field)` | Creates a field by copying what a reader has just read, to be written byte for byte. **Throws** `ObjectDisposedException`: The reader of the field has been disposed. **Throws** `InvalidOperationException`: The reader has moved on from the field, or the view was not obtained from a reader. |
| `ImfHeaderField(string name, string value, ImfHeaderFieldKind kind)` | Creates a field from its name and its body, which is written as it is given, folded as its kind allows. The name and body are checked when the field is written, in the way that [`ImfHeaderWriter.WriteField(string, string, ImfHeaderFieldKind)`](ImfHeaderWriter.md) describes. **`value`:** The unfolded field body. One space is written before it if it doesn't start with a space or tab, and isn't empty. **Throws** `ArgumentNullException`: `name` or `value` is `null`. **Throws** `ArgumentOutOfRangeException`: `kind` is not an [`ImfHeaderFieldKind`](ImfHeaderFieldKind.md) member. **Throws** [`ImfFileException`](ImfFileException.md): `name` or `value` is not valid Unicode. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Name` | `string Name { get; }` | Gets the field name, without any white space before the colon. Field names are case-insensitive. |
| `RawBytes` | `ReadOnlyMemory<byte> RawBytes { get; }` | Gets the whole field exactly as it was read: its name, any white space before the colon, its folds and its final line break. It is empty for a field that was not read, which a writer formats. |
| `Value` | `string Value { get; }` | Gets the field body as a string, unfolded (RFC 5322 §2.2.3): as UTF-8 (RFC 6532), or with [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) if it was read under [`ImfOptions.AllowEightBitHeaders`](ImfOptions.md) and is not valid UTF-8. RFC 2047 encoded words are left as they are. The body of a field that was read keeps its white space, including any after the colon. The body of a field made from text or a typed value is the form it has with [`ImfHeaderEncoding.EncodedWords`](ImfHeaderEncoding.md). |
| `ValueBytes` | `ReadOnlyMemory<byte> ValueBytes { get; }` | Gets the bytes of `Value`, unfolded, as they were read. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `static FromRaw(ReadOnlyMemory<byte> rawField)` | `ImfHeaderField` | Creates a field from its raw bytes, to be written byte for byte, including its folding and its final line break. **Returns:** The field. **Throws** [`ImfFileException`](ImfFileException.md): The bytes are not one whole field, as an [`ImfHeaderReader`](ImfHeaderReader.md) would read one. |
| `static FromRaw(ReadOnlyMemory<byte> rawField, ImfOptions options)` | `ImfHeaderField` | Creates a field from its raw bytes, to be written byte for byte, including its folding and its final line break. **Returns:** The field. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has a limit that is less than 1, no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. **Throws** [`ImfFileException`](ImfFileException.md): The bytes are not one whole field, as an [`ImfHeaderReader`](ImfHeaderReader.md) would read one under the options. |
| `static FromText(string name, string text)` | `ImfHeaderField` | Creates a field that holds text, as an unstructured field such as Subject, to be written as [`ImfHeaderWriter.WriteTextField`](ImfHeaderWriter.md) writes it, with the [`ImfOptions.HeaderEncoding`](ImfOptions.md) of the writer. **Returns:** The field. **Throws** `ArgumentNullException`: `name` or `text` is `null`. **Throws** [`ImfFileException`](ImfFileException.md): `name` or `text` is not valid Unicode, or `text` holds a control character other than tab. |
| `GetDecodedValue()` | `string` | Decodes `Value` as the body of an unstructured field, such as Subject or Comments, or a field of unknown meaning, as [`ImfHeaderText.DecodeUnstructured(string)`](ImfHeaderText.md) does. **Returns:** The text. |
| `GetDecodedValue(ImfOptions options)` | `string` | Decodes `Value` as the body of an unstructured field, such as Subject or Comments, or a field of unknown meaning, as [`ImfHeaderText.DecodeUnstructured(string, ImfOptions)`](ImfHeaderText.md) does. **Returns:** The text. **Throws** `ArgumentNullException`: `options` is `null`. **Throws** `ArgumentException`: `options` has no [`ImfOptions.CharsetResolver`](ImfOptions.md), or an [`ImfOptions.HeaderFallbackCharset`](ImfOptions.md) that is needed and can't be resolved. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Edit one header without breaking a signature](../how-to/edit-a-header.md)
- [Byte-exact round trips](../concepts/round-trips.md)
- [ImfFileException](ImfFileException.md)
- [ImfHeaderEncoding](ImfHeaderEncoding.md)
- [ImfHeaderFieldCollection](ImfHeaderFieldCollection.md)
- [ImfHeaderFieldKind](ImfHeaderFieldKind.md)
- [ImfHeaderReader](ImfHeaderReader.md)
- [ImfHeaderText](ImfHeaderText.md)
- [ImfHeaderWriter](ImfHeaderWriter.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
