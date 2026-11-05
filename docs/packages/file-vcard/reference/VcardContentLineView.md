---
title: VcardContentLineView Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardContentLineView Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A view of the content line a [VcardContentLineReader](VcardContentLineReader.md) has just
read, without allocation. Its members return spans and memory over the reader's own
buffers.

**Syntax**

```csharp
public readonly struct VcardContentLineView
```

## Remarks

Get a view from `VcardContentLineReader.CurrentLine`.

**When a view goes stale.** Each view carries a version stamp taken from the reader. The
view goes stale as soon as the next read starts, and when the reader is disposed. A read
that is refused before it starts doesn't make it stale.

**Using a stale view.** Every member checks the stamp. Once the reader has been disposed,
every member throws `ObjectDisposedException`. Otherwise, once the reader has moved on,
every member throws `InvalidOperationException`. A `default` view, not obtained from a
reader, throws `InvalidOperationException` from every member.

**Spans and memory taken from a view.** They point into the reader's pooled buffers, and
are valid for the same period as the view, but can't check this. Using one later throws
nothing and gives undefined content. Copy anything that must outlive the line, or use
`ToContentLine`.

**Decoded values.** `GetParameterValue` and `GetTextValue` decode when first asked and
keep the result until the reader moves on, so asking again costs nothing.

## Properties

Properties with a `Memory` form return `ReadOnlyMemory<char>`, for use across an `await`.

| Property | Signature | Notes |
| --- | --- | --- |
| `Group`, `GroupMemory` | `ReadOnlySpan<char> Group { get; }` | The group, as written and without its `.`; empty if the line has none. |
| `LineNumber` | `long LineNumber { get; }` | The 1-based number of the logical line. |
| `Name`, `NameMemory` | `ReadOnlySpan<char> Name { get; }` | The property name, as written. |
| `ParameterCount` | `int ParameterCount { get; }` | The number of parameters. |
| `PhysicalLineNumber` | `long PhysicalLineNumber { get; }` | The 1-based number of the physical line on which the line starts. |
| `RawLine`, `RawLineMemory` | `ReadOnlySpan<char> RawLine { get; }` | The whole logical line, unfolded, without its line break. |
| `RawValue`, `RawValueMemory` | `ReadOnlySpan<char> RawValue { get; }` | The value, unfolded, with any TEXT escaping in place. |

## Methods

| Method | Signature | Notes |
| --- | --- | --- |
| `GetParameterName`, `GetParameterNameMemory` | `ReadOnlySpan<char> GetParameterName(int parameterIndex)` | The parameter name, as written. |
| `GetParameterValue`, `GetParameterValueMemory` | `ReadOnlySpan<char> GetParameterValue(int parameterIndex, int valueIndex)` | A value without enclosing double quotes. RFC 6868 caret encoding is decoded where [the reader](VcardContentLineReader.md#caret-encoding) says it is. |
| `GetParameterValueCount` | `int GetParameterValueCount(int parameterIndex)` | The number of values. Always at least one. |
| `GetRawParameterValue`, `GetRawParameterValueMemory` | `ReadOnlySpan<char> GetRawParameterValue(int parameterIndex, int valueIndex)` | A value without enclosing double quotes, and with any caret encoding in place. |
| `GetTextValue`, `GetTextValueMemory` | `ReadOnlySpan<char> GetTextValue()` | The value with RFC 6350 §3.4 TEXT escaping removed. Any other backslash throws `VcardFileException`. Use it only for a single TEXT value. |
| `GroupEquals` | `bool GroupEquals(ReadOnlySpan<char> group)` | Compares the group case-insensitively. |
| `IndexOfParameter` | `int IndexOfParameter(ReadOnlySpan<char> name)` | The index of the first parameter with the name, compared case-insensitively; or -1. |
| `IsParameterValueQuoted` | `bool IsParameterValueQuoted(int parameterIndex, int valueIndex)` | Whether the value was in double quotes. |
| `NameEquals` | `bool NameEquals(ReadOnlySpan<char> name)` | Compares the property name case-insensitively. |
| `ToContentLine` | `VcardContentLine ToContentLine()` | Copies the line, so that it stays valid after the reader moves on. |

The methods that take an index throw `ArgumentOutOfRangeException` for one that isn't
valid.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLineReader](VcardContentLineReader.md)
- [VcardContentLine](VcardContentLine.md)
