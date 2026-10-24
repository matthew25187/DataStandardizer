---
title: IcalContentLineView Struct
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalContentLineView Struct

## Definition

Namespace: `DataStandardizer.File.iCal`

A view of the content line an [IcalContentLineReader](IcalContentLineReader.md) has
just read, without allocation. Its members return spans and memory over the reader's
own buffers.

**Syntax**

```csharp
public readonly struct IcalContentLineView
```

## Remarks

Get a view from `IcalContentLineReader.CurrentLine`, after `ReadContentLine`,
`ReadLine` or one of their async forms has returned a line.

**When a view goes stale.** Each view carries a version stamp taken from the reader.
The reader advances the stamp at the start of every read: `ReadContentLine`,
`ReadLine`, the next step of `ReadLines`, and their async forms. So the view goes
stale as soon as the next read starts, whether that read returns a line, reaches the
end of the data, throws `IcalFileException`, or is still pending. A read call that is
refused before it starts (because the reader is disposed, another read is in
progress, or the reader cannot continue) does not advance the stamp. The view also
goes stale when the reader is disposed.

**Using a stale view.** Every member checks the stamp. Once the reader has been
disposed, every member throws `ObjectDisposedException`. Otherwise, once the reader has
moved on, every member throws `InvalidOperationException`. A `default` view, not
obtained from a reader, throws `InvalidOperationException` from every member.

**Spans and memory taken from a view.** A `ReadOnlySpan<char>` or `ReadOnlyMemory<char>`
returned by the view points into the reader's pooled character block or its pooled
scratch buffer. It is valid for exactly the same period as the view, but it cannot
check this. Using one after the view has gone stale throws nothing and gives
undefined content: the next read compacts and overwrites the block in place, a longer
line makes the reader swap to a larger block and return the old one to
`ArrayPool<char>.Shared`, and disposal returns both buffers to the pool, where other
code may rent and overwrite them. Copy anything that must outlive the line (for
example with `ToString()` or `ToArray()`), or call `ToContentLine`. A span cannot be
held across an `await`; memory can, but the next read still invalidates it.

**Decoded values.** `GetParameterValue` and `GetTextValue` decode when first asked and
keep the result until the reader moves on, so asking again costs nothing. A value
with nothing to decode is returned as a slice of the line itself. Otherwise it is
decoded into a scratch buffer that is sized for the whole line and does not move
while the line is current, so a decoded value taken out earlier stays valid when
later values are decoded.

**Allocation.** No member allocates, except `ToContentLine`, and the first decode of a
line, which may rent the scratch buffer from `ArrayPool<char>.Shared`.

**Thread safety.** The stamp check is not synchronized with reads. Do not use a view
on one thread while another thread reads from the same reader.

**Names.** Property and parameter names are case-insensitive (RFC 5545 §2). The name
properties return the name as written; `NameEquals` and `IndexOfParameter` compare
ASCII letters case-insensitively and every other character exactly.

## Properties

Every property throws `ObjectDisposedException` if the reader has been disposed, or
`InvalidOperationException` if the reader has moved on to another line or the view is
`default`.

| Property | Signature | Notes |
| --- | --- | --- |
| `LineNumber` | `long LineNumber { get; }` | The 1-based number of the logical (unfolded) line. |
| `PhysicalLineNumber` | `long PhysicalLineNumber { get; }` | The 1-based number of the physical line on which the line starts. |
| `RawLine` | `ReadOnlySpan<char> RawLine { get; }` | The whole logical line, unfolded, without its line break. |
| `RawLineMemory` | `ReadOnlyMemory<char> RawLineMemory { get; }` | As `RawLine`, as memory. |
| `Name` | `ReadOnlySpan<char> Name { get; }` | The property name, as written. Use `NameEquals` to compare it. |
| `NameMemory` | `ReadOnlyMemory<char> NameMemory { get; }` | As `Name`, as memory. |
| `ParameterCount` | `int ParameterCount { get; }` | The number of parameters. |
| `RawValue` | `ReadOnlySpan<char> RawValue { get; }` | The value as it appears in the unfolded line, with any TEXT escaping in place. |
| `RawValueMemory` | `ReadOnlyMemory<char> RawValueMemory { get; }` | As `RawValue`, as memory. |

## Methods

Every method throws `ObjectDisposedException` if the reader has been disposed, or
`InvalidOperationException` if the reader has moved on to another line or the view is
`default`. Indexes are 0-based; an index out of range throws
`ArgumentOutOfRangeException`.

### Names and parameters

| Method | Returns | Notes |
| --- | --- | --- |
| `NameEquals(ReadOnlySpan<char> name)` | `bool` | Whether the property name equals `name`, compared case-insensitively. |
| `GetParameterName(int parameterIndex)` | `ReadOnlySpan<char>` | The name of a parameter, as written. |
| `GetParameterNameMemory(int parameterIndex)` | `ReadOnlyMemory<char>` | As `GetParameterName`, as memory. |
| `IndexOfParameter(ReadOnlySpan<char> name)` | `int` | The index of the first parameter with that name, compared case-insensitively, or -1 if there is none. |
| `GetParameterValueCount(int parameterIndex)` | `int` | The number of values the parameter has; always at least 1. |
| `GetRawParameterValue(int parameterIndex, int valueIndex)` | `ReadOnlySpan<char>` | A parameter value as written, without enclosing double quotes, with RFC 6868 caret encoding in place. |
| `GetRawParameterValueMemory(int parameterIndex, int valueIndex)` | `ReadOnlyMemory<char>` | As `GetRawParameterValue`, as memory. |
| `GetParameterValue(int parameterIndex, int valueIndex)` | `ReadOnlySpan<char>` | A parameter value with RFC 6868 caret encoding decoded: `^n` becomes a line feed, `^'` a double quote, and `^^` a caret. A caret followed by anything else is kept. |
| `GetParameterValueMemory(int parameterIndex, int valueIndex)` | `ReadOnlyMemory<char>` | As `GetParameterValue`, as memory. |
| `IsParameterValueQuoted(int parameterIndex, int valueIndex)` | `bool` | Whether the parameter value was enclosed in double quotes. |

### Values and copying

| Method | Returns | Notes |
| --- | --- | --- |
| `GetTextValue()` | `ReadOnlySpan<char>` | The value with RFC 5545 §3.3.11 TEXT escaping removed: `\\`, `\;` and `\,` become the escaped character, and `\n` or `\N` a line feed. Use it only for a single TEXT value; for a list of TEXT values, split `RawValue` on unescaped commas first. Throws `IcalFileException` if a backslash does not start a valid escape sequence. |
| `GetTextValueMemory()` | `ReadOnlyMemory<char>` | As `GetTextValue`, as memory. |
| `ToContentLine()` | `IcalContentLine` | Copies the line into an allocated `IcalContentLine`, which stays valid after the reader moves on. It keeps the parameters exactly as read, so `IcalContentLineWriter` writes them back unchanged. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Read zero-copy](../how-to/read-zero-copy.md)
- [The layered model](../concepts/layered-model.md)
- [IcalContentLineReader](IcalContentLineReader.md)
- [IcalContentLine](IcalContentLine.md)
- [File.iCal API reference](index.md)
