---
title: VcardContentLine Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardContentLine Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A content line (RFC 6350 §3.3): an optional group, a property name, its parameters and
its value. This is the allocating form of a line, returned by
`VcardContentLineReader.ReadLine`. It stays valid after the reader moves on.

**Syntax**

```csharp
public sealed class VcardContentLine
```

## Remarks

The constructors check only for `null`. They don't check that the group, name or value
follow the content line syntax.

Groups, property names and parameter names are case-insensitive. They are kept as
written.

Instances are immutable, so they may be shared between threads.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardContentLine(string name, string value)` | A line with no group and no parameters. |
| `VcardContentLine(string name, IEnumerable<VcardContentLineParameter> parameters, string value)` | A line with no group. The parameters are copied, in order. |
| `VcardContentLine(string? group, string name, IEnumerable<VcardContentLineParameter> parameters, string value)` | `group` has no `.`, and may be `null` for none. |

The value is given as it appears in the line, with any TEXT escaping in place. Each
constructor throws `ArgumentNullException` for a `null` name, parameters or value, and
`ArgumentException` if the parameters contain `null`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Group` | `string? Group { get; }` | The group, without its `.`; or `null` if the line has none. |
| `LegacySyntax` | `VcardLegacySyntax LegacySyntax { get; }` | The lexical features of vCard 2.1 that the line uses. |
| `LineNumber` | `long LineNumber { get; }` | The 1-based number of the logical line this was read from; or 0 if it was not read from a source. |
| `Name` | `string Name { get; }` | The property name. |
| `Parameters` | `IReadOnlyList<VcardContentLineParameter> Parameters { get; }` | The parameters, in order. The list is read-only: it can't be cast to an array or a writable list and changed. |
| `PhysicalLineNumber` | `long PhysicalLineNumber { get; }` | The 1-based number of the physical line on which this line starts; or 0 if it was not read from a source. |
| `Value` | `string Value { get; }` | The value, unfolded, with any TEXT escaping in place. |

## Methods

| Method | Signature | Notes |
| --- | --- | --- |
| `GetDecodedText` | `string GetDecodedText()` | The text of the value, by its `ENCODING` and `CHARSET`. See [Legacy syntax and character sets](../legacy-syntax.md). Throws `VcardFileException` if either can't be resolved or the value isn't valid in them. |
| `GetDecodedText` | `string GetDecodedText(out bool usedCharsetFallback)` | The same, and whether `AllowUnknownCharsetAsLatin1` was needed. |
| `GetDecodedValue` | `byte[] GetDecodedValue()` | The octets of the value, by its `ENCODING`. Throws `VcardFileException` if it can't be resolved or the value isn't valid in it. |
| `TryGetCharset` | `bool TryGetCharset(out MimeCharset charset)` | The `CHARSET`, if it applies to the line and its name is valid. |
| `TryGetTransferEncoding` | `bool TryGetTransferEncoding(out MimeTransferEncoding encoding)` | The `ENCODING`, if it applies to the line and its name is valid. `b` gives `base64`. |
| `GetParameter` | `VcardContentLineParameter? GetParameter(string name)` | The first parameter with the name, compared case-insensitively; or `null`. |
| `GetTextValue` | `string GetTextValue()` | The value with RFC 6350 §3.4 TEXT escaping removed: `\\`, `\;` and `\,` become the escaped character, and `\n` or `\N` a line feed. Any other backslash throws `VcardFileException`, unless `AcceptLegacySyntax` is set; then it gives the next character. A vCard 2.1 line returns its value unchanged. Use it only for a single TEXT value: split a list on unescaped commas, or a compound value on unescaped semicolons, before unescaping. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineReader](VcardContentLineReader.md)
- [VcardContentLineView](VcardContentLineView.md)
- [VcardContentLineParameter](VcardContentLineParameter.md)
- [VcardContentLineWriter](VcardContentLineWriter.md)
