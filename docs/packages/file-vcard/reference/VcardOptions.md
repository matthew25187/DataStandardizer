---
title: VcardOptions Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardOptions Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Options to configure the behaviour of a vCard reader or writer. The defaults follow
RFC 6350, and a vCard 2.1 card follows its own specification. Each accommodation for
common non-compliant input or output is a separate option, off by default. There is no
single "lenient mode".

**.NET:**

```csharp
public sealed record VcardOptions
```

**.NET Standard:**

```csharp
public sealed class VcardOptions
```

## Properties

Each property is `init` on .NET and `set` on .NET Standard. A reader ignores the options
that are only for writing, and a writer ignores those that are only for reading.

### Reading

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `AcceptBareLineFeed` | `bool` | `false` | Accepts a bare line feed as a line break, both to end a line and in a fold. RFC 6350 §3.2 and §3.3 require CRLF. A bare carriage return is never a line break. |
| `AcceptByteOrderMark` | `bool` | `false` | Accepts and skips a byte order mark at the start of the data. The `vcard` rule of RFC 6350 §3.3 starts with `BEGIN:VCARD`, and RFC 3629 §6 says a protocol that mandates UTF-8 should forbid the signature. It never selects the encoding. |
| `AcceptCaretEncodingInAllVersions` | `bool` | `false` | Decodes RFC 6868 caret encoding on every line. RFC 6868 updates RFC 6350, and not RFC 2426 or vCard 2.1, where a caret is an ordinary character. |
| `AcceptEightBitWithoutEncoding` | `bool` | `false` | Decodes a value in a 2.1 card with characters outside US-ASCII and no `ENCODING=8BIT`. vCard 2.1 §2.1.5 makes 7-bit the default. The value must still be valid in its `CHARSET`. |
| `AcceptLegacySyntax` | `bool` | `false` | Gives every line the lexical forms of vCard 2.1, which a 2.1 card has anyway: parameters with no name, soft line breaks, `BASE64` lines, `ENCODING` and `CHARSET`. It also accepts a parameter with no name that vCard 2.1 §2.9 doesn't allow, and an escape sequence that RFC 6350 §3.4 doesn't have. See [Legacy syntax and character sets](../legacy-syntax.md). |
| `AcceptMissingFinalLineBreak` | `bool` | `false` | Accepts a last line with no line break. The `contentline` rule of RFC 6350 §3.3 ends with CRLF. |
| `AcceptRfc6350FoldsInVersion21` | `bool` | `false` | Unfolds a 2.1 card as RFC 6350 §3.2 does, removing one white space character. vCard 2.1 §2.1.3 keeps it. |
| `AllowUnknownCharsetAsLatin1` | `bool` | `false` | Decodes a value in a character set that can't be resolved as ISO-8859-1. `GetDecodedText(out bool)` says it did. |
| `CharsetResolver` | `IMimeCharsetResolver?` | `null` | Resolves the character sets that the built-in encodings and the runtime don't. |
| `MaxLineLength` | `int?` | `null` | The longest logical line allowed, in characters, not counting its line break. `null` means no limit, since RFC 6350 sets none. The reader keeps each logical line in memory, so a limit is recommended for untrusted data. A line with more folds than the limit is rejected too. It must be at least 1. |

### Writing

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `AllowLossyVersionConversion` | `bool` | `false` | Writes a parameter value that needs RFC 6868 caret encoding outside a 4.0 card without those characters: a double quote is left out, and a line break becomes one space. RFC 6868 updates RFC 6350 only, and the `param-value` rule of RFC 2426 §4 can't hold them. The default throws `VcardFileException`. |
| `FoldLength` | `int` | `75` | The length, in octets of the UTF-8 encoding and not counting the line break, at which lines are folded. RFC 6350 §3.2 says lines should be folded to 75, so a longer length relaxes that. A fold never splits a character. The minimum is 4. The lines of a 2.1 card are never folded. |
| `FoldWhitespace` | `char` | `' '` | The white space at the start of each continuation line: a space or a horizontal tab, the two that RFC 6350 §3.2 allows. |
| `LineBreak` | `string` | `"\r\n"` | The line break at the end of each physical line. RFC 6350 §3.2 and §3.3 require CRLF. A bare line feed, `"\n"`, relaxes that, and no other value is accepted. |

### Reading and writing

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `Encoding` | `Encoding?` | `null` | Applies to a stream or file. `null` means strict UTF-8 with no byte order mark, as RFC 6350 §3.1 requires. Setting another encoding relaxes that rule, and the encoding is used as it is. For a reader, `Encoding.UTF8` replaces invalid data instead of rejecting it, and an encoding with a preamble has it removed from the start of the data. A writer writes the preamble of the encoding, if it has one. To read raw 8-bit vCard 2.1 values, set ISO-8859-1. Must be `null` with a `TextReader` or `TextWriter`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineReader](VcardContentLineReader.md)
- [VcardContentLineWriter](VcardContentLineWriter.md)
