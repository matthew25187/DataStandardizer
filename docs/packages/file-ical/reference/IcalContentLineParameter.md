---
title: IcalContentLineParameter Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalContentLineParameter Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A parameter of a content line (RFC 5545 §3.2), with its list of values.

**Syntax**

```csharp
public sealed class IcalContentLineParameter
```

## Remarks

Values are held decoded: without RFC 6868 caret encoding and without enclosing double
quotes. Pass them to the constructor the same way. When the parameter is written,
`IcalContentLineWriter` caret encodes each value (`^` as `^^`, a double quote as `^'`,
and a line feed, carriage return or CRLF as `^n`) and quotes a value that holds a `:`,
`;` or `,`.

The constructors check only for `null` and for at least one value. The name and the
values are checked when the line is written: `IcalContentLineWriter` throws
`IcalFileException` for a name that is not one or more letters, digits or hyphens, or
for a value holding a control character other than tab, carriage return or line
feed, or a lone surrogate.

Parameter names are case-insensitive. `IcalContentLine.GetParameter` compares them
that way. For well-known names, see [IcalParameterNames](IcalParameterNames.md).

Instances are immutable, so they may be shared between threads.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalContentLineParameter(string name, params string[] values)` | The values, without caret encoding. There must be at least one. Throws `ArgumentNullException` if `name` or `values` is `null`, or `ArgumentException` if `values` is empty or contains `null`. |
| `IcalContentLineParameter(string name, IEnumerable<string> values)` | The values are copied, in order. Throws as above. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Name` | `string Name { get; }` | The parameter name, as written. |
| `Values` | `IReadOnlyList<string> Values { get; }` | The values, in order, with RFC 6868 caret encoding decoded and enclosing double quotes removed. Always at least one. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [IcalContentLine](IcalContentLine.md)
- [IcalParameterNames](IcalParameterNames.md)
- [IcalContentLineWriter](IcalContentLineWriter.md)
- [File.iCal API reference](index.md)
