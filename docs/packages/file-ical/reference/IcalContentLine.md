---
title: IcalContentLine Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalContentLine Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A content line (RFC 5545 §3.1): a property name, its parameters and its value. This
is the allocating, immutable form of a line. It is what `IcalContentLineReader.ReadLine`
returns and `IcalContentLineWriter.WriteLine` takes, and every property of an
[IcalComponent](IcalComponent.md) is held as one.

**Syntax**

```csharp
public sealed class IcalContentLine
```

## Remarks

**Values.** `Value` is the value as it appears in the content line once unfolded. TEXT
escaping is left in place, because whether it applies depends on the value type. Call
`GetTextValue` for a single TEXT value. When you build a line yourself, pass the value
already escaped.

**Parameters.** `Parameters` holds the decoded parameter values: RFC 6868 caret
encoding removed, and enclosing double quotes removed. A line read by
[IcalContentLineReader](IcalContentLineReader.md) also keeps its parameters exactly as
they appeared in the source, and `IcalContentLineWriter` writes that original text, so
a line read and written back keeps its quoting and caret encoding. A line you build,
or one converted from xCal or jCal, has its parameters encoded afresh when written.

**Validation.** The constructors check only for `null`. Names and characters are
checked when the line is written: `IcalContentLineWriter` throws `IcalFileException` for
a name that is not one or more letters, digits or hyphens, or for a control character
(other than tab) or lone surrogate in the value.

**Line numbers.** `LineNumber` and `PhysicalLineNumber` record where a line read from
.ics data came from. A line read by `XcalReader` has the line of the XML document as
its `LineNumber` and 0 as its `PhysicalLineNumber`. A line read by `JcalReader`, or
built in code, has 0 for both.

**Thread safety.** Instances are immutable, so they may be shared between threads.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalContentLine(string name, string value)` | A line with no parameters. `value` is as it appears in the content line, with any TEXT escaping in place. Throws `ArgumentNullException` if `name` or `value` is `null`. |
| `IcalContentLine(string name, IEnumerable<IcalContentLineParameter> parameters, string value)` | The parameters are copied, in order. Throws `ArgumentNullException` if `name`, `parameters` or `value` is `null`, or `ArgumentException` if `parameters` contains `null`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `LineNumber` | `long LineNumber { get; }` | The 1-based number of the logical (unfolded) line this was read from, or 0 if it was not read from a source. For xCal, the line of the XML document. |
| `Name` | `string Name { get; }` | The property name, as written. Property names are case-insensitive. |
| `Parameters` | `IReadOnlyList<IcalContentLineParameter> Parameters { get; }` | The parameters, in the order they appear. The list is read-only: it can't be cast to an array or a writable list and changed. |
| `PhysicalLineNumber` | `long PhysicalLineNumber { get; }` | The 1-based number of the physical line on which this line starts in its .ics source, or 0 if it was not read from .ics data. |
| `Value` | `string Value { get; }` | The value as it appears in the content line once unfolded, with TEXT escaping left in place. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetParameter(string name)` | `IcalContentLineParameter?` | The first parameter with that name, compared case-insensitively (ordinal), or `null` if there is none. Throws `ArgumentNullException` if `name` is `null`. |
| `GetTextValue()` | `string` | The value with RFC 5545 §3.3.11 TEXT escaping removed: `\\`, `\;` and `\,` become the escaped character, and `\n` or `\N` a line feed. Returns `Value` itself when it holds no backslash. Use it only for a single TEXT value; for a list of TEXT values, split `Value` on unescaped commas first. Throws `IcalFileException` if a backslash does not start a valid escape sequence; its `Data` holds `LineNumber`, `PhysicalLineNumber` (the physical line of the bad escape) and `PropertyName`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [The layered model](../concepts/layered-model.md)
- [IcalContentLineParameter](IcalContentLineParameter.md)
- [IcalContentLineReader](IcalContentLineReader.md)
- [IcalContentLineView](IcalContentLineView.md)
- [IcalContentLineWriter](IcalContentLineWriter.md)
- [IcalComponent](IcalComponent.md)
- [File.iCal API reference](index.md)
