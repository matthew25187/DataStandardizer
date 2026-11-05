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
RFC 6350. Each accommodation for common non-compliant input or output is a separate
option, off by default. There is no single "lenient mode".

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
| `AcceptMissingFinalLineBreak` | `bool` | `false` | Accepts a last line with no line break. The `contentline` rule of RFC 6350 §3.3 ends with CRLF. |
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
| `Encoding` | `Encoding?` | `null` | Applies to a stream or file. `null` means strict UTF-8 with no byte order mark, as RFC 6350 §3.1 requires. Setting another encoding relaxes that rule, and the encoding is used as it is. For a reader, `Encoding.UTF8` replaces invalid data instead of rejecting it, and an encoding with a preamble has it removed from the start of the data. A writer writes the preamble of the encoding, if it has one. Must be `null` with a `TextReader` or `TextWriter`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineReader](VcardContentLineReader.md)
- [VcardContentLineWriter](VcardContentLineWriter.md)
