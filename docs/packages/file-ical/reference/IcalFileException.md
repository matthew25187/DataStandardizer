---
title: IcalFileException Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalFileException Class

## Definition

Namespace: `DataStandardizer.File.iCal`

The exception thrown for iCalendar data that cannot be read or written: a content
line that does not follow RFC 5545 §3.1, a `BEGIN` or `END` that is not matched, an
unsupported `VERSION`, a calendar that breaks a rule the options do not accept, a
property that cannot be written, a value a typed accessor cannot parse, an xCal or
jCal document that is not valid, or a time zone that cannot be resolved.

**Syntax**

```csharp
public sealed class IcalFileException : Exception
```

## Remarks

Details of where the problem was found are held in the exception's `Data`
dictionary, under the keys of the nested static class `IcalFileException.DataItemName`.
Each item is present only when it applies, so check with `Data.Contains` before
reading it. Some exceptions carry no items at all: data that holds no `VCALENDAR`, an
xCal document with no `icalendar` root element, and the time zone errors of
`IcalTimeZoneResolver` and the VTIMEZONE rules.

```csharp
catch (IcalFileException exception)
{
    if (exception.Data.Contains(IcalFileException.DataItemName.LineNumber))
    {
        var line = (long)exception.Data[IcalFileException.DataItemName.LineNumber]!;
    }
}
```

**When reading can continue.** For [IcalContentLineReader](IcalContentLineReader.md), a
content line syntax error consumes the line, so reading can continue; a bare line feed
that is not accepted, or a line longer than `IcalOptions.MaxLineLength`, cannot be
recovered from. For [IcalReader](IcalReader.md), a conformance problem (one with
`DiagnosticKind`, found after `END:VCALENDAR`) discards that calendar and reading can
continue; any other `IcalFileException`, including an unsupported `VERSION`, leaves the
reader unable to continue. For the writers, a conformance problem means nothing of
that calendar was written, and a content line error means nothing of that line was
written.

**Where each item comes from.**

| Thrown by | Items present |
| --- | --- |
| `IcalContentLineReader`, and content line errors from `IcalReader` | `LineNumber`, `PhysicalLineNumber`, and `PropertyName` once the name has been read. |
| `IcalReader`: `BEGIN`/`END` errors and a property outside a calendar | `LineNumber`, `PhysicalLineNumber`, `PropertyName`, and `ComponentPath` when inside a component. |
| `IcalReader`, `XcalReader`, `JcalReader`: unsupported `VERSION` | `DiagnosticKind` = `UnsupportedVersion`, `PropertyName`, `ComponentPath`, and the location items that reader gives: `LineNumber` and `PhysicalLineNumber` for .ics, `LineNumber` and `LinePosition` for xCal, `JsonPath` and `BytePosition` for jCal. |
| Any reader or writer of the component model: a conformance problem | `ComponentPath`, `DiagnosticKind`, and `LineNumber` and `PropertyName` when the diagnostic has them. |
| `IcalContentLineWriter`, and content line errors from `IcalWriter` | `LineNumber` (the number the line would have had in the output), and `PropertyName` once the property name is valid. |
| `IcalContentLine.GetTextValue`, `IcalContentLineView.GetTextValue`, and the typed accessors of the component model | `LineNumber`, `PhysicalLineNumber`, `PropertyName`. The items are always present: for a property read from xCal, `LineNumber` is the line of the XML document and `PhysicalLineNumber` is 0; for one read from jCal or built in code, both are 0. |
| `XcalReader` | `LineNumber` and `LinePosition` in the XML document where known, and `PropertyName` where it applies. For XML that is not well-formed, the `XmlException` is the inner exception. |
| `JcalReader` | `JsonPath` and `BytePosition`, and `PropertyName` where it applies. For JSON that is not valid, `BytePosition`, `LineNumber` and `LinePosition` in the JSON document, with the `JsonException` as the inner exception. |
| `JcalWriter` | `LineNumber` of the property's source line, if it has one, and `PropertyName`. |

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalFileException()` | |
| `IcalFileException(string message)` | |
| `IcalFileException(string message, Exception innerException)` | |

## Fields

The constants of the nested static class `IcalFileException.DataItemName`, which are
the keys of the items in `Data`.

| Field | Value | Meaning |
| --- | --- | --- |
| `BytePosition` | `"BytePosition"` | A `long`: the 0-based offset, in bytes from the start of the data, of the JSON token at which the problem was found. A byte order mark at the start is counted. For data that is not valid JSON, the offset of the end of the last token read. `JcalReader` only. |
| `ComponentPath` | `"ComponentPath"` | A `string`: the path of the component in which the problem was found, such as `VCALENDAR/VEVENT[3]`. Each component under the calendar has a 1-based index among the components of the same name in its parent. Readers and writers of the component model only. See `IcalDiagnostic.ComponentPath`. |
| `DiagnosticKind` | `"DiagnosticKind"` | An [IcalDiagnosticKind](IcalDiagnosticKind.md): the kind of conformance problem. Present only for a problem that `IcalComponent.Validate` also reports. Any of the six values can appear. `InvalidComponentNesting` and `UnsupportedVersion` always throw; the other four throw only when their `IcalOptions` accommodation is off. |
| `JsonPath` | `"JsonPath"` | A `string`: the path of the JSON value at which the problem was found, such as `$[2][0][1][3]` or `$[1][3][1]['tzid']`. `JcalReader` only, once the value has been read; a JSON syntax error has `BytePosition` instead. |
| `LineNumber` | `"LineNumber"` | A `long`: the 1-based number of the logical (unfolded) content line. For `XcalReader`, the line of the XML document. For JSON that is not valid, the line of the JSON document. For a writer's content line error, the number the line would have had in the output. |
| `LinePosition` | `"LinePosition"` | A `long`: the 1-based position on the line of the XML document, where known (`XcalReader`); or, for JSON that is not valid, the 1-based position in bytes on the line of the JSON document (`JcalReader`). |
| `PhysicalLineNumber` | `"PhysicalLineNumber"` | A `long`: the 1-based number of the physical line on which the problem was found in .ics data. Not added by the xCal or jCal readers, but a typed accessor adds it, as 0, for a property that was not read from .ics data. |
| `PropertyName` | `"PropertyName"` | A `string`: the name of the property on the content line. Present only once the name has been read. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`JcalReader` and `JcalWriter`, and so the `BytePosition` and `JsonPath` items, are
available on `netstandard2.0`, `netstandard2.1`, and the .NET targets only; the
constants themselves are on every target.

## See also

- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [IcalDiagnosticKind](IcalDiagnosticKind.md)
- [IcalDiagnostic](IcalDiagnostic.md)
- [IcalOptions](IcalOptions.md)
- [IcalReader](IcalReader.md)
- [IcalWriter](IcalWriter.md)
- [File.iCal API reference](index.md)
