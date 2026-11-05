---
title: VcardOptions Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardOptions Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Options to configure the behaviour of a vCard reader. The defaults follow RFC 6350. Each
accommodation for common non-compliant input is a separate option, off by default. There
is no single "lenient mode".

**.NET:**

```csharp
public sealed record VcardOptions
```

**.NET Standard:**

```csharp
public sealed class VcardOptions
```

## Properties

Each property is `init` on .NET and `set` on .NET Standard.

| Property | Type | Default | Notes |
| --- | --- | --- | --- |
| `AcceptBareLineFeed` | `bool` | `false` | Accepts a bare line feed as a line break, both to end a line and in a fold. RFC 6350 §3.2 and §3.3 require CRLF. A bare carriage return is never a line break. |
| `AcceptByteOrderMark` | `bool` | `false` | Accepts and skips a byte order mark at the start of the data. The `vcard` rule of RFC 6350 §3.3 starts with `BEGIN:VCARD`, and RFC 3629 §6 says a protocol that mandates UTF-8 should forbid the signature. It never selects the encoding. |
| `AcceptCaretEncodingInAllVersions` | `bool` | `false` | Decodes RFC 6868 caret encoding on every line. RFC 6868 updates RFC 6350, and not RFC 2426 or vCard 2.1, where a caret is an ordinary character. |
| `AcceptMissingFinalLineBreak` | `bool` | `false` | Accepts a last line with no line break. The `contentline` rule of RFC 6350 §3.3 ends with CRLF. |
| `Encoding` | `Encoding?` | `null` | Applies to a stream or file. `null` means strict UTF-8, as RFC 6350 §3.1 requires. Setting another encoding relaxes that rule, and the encoding is used as it is: `Encoding.UTF8` replaces invalid data instead of rejecting it, and an encoding with a preamble has it removed from the start of the data. Must be `null` with a `TextReader`. |
| `MaxLineLength` | `int?` | `null` | The longest logical line allowed, in characters, not counting its line break. `null` means no limit, since RFC 6350 sets none. The reader keeps each logical line in memory, so a limit is recommended for untrusted data. It must be at least 1. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineReader](VcardContentLineReader.md)
