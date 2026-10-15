---
title: IcalValueFormat Enum
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalValueFormat Enum

## Definition

Namespace: `DataStandardizer.File.iCal`

The lexical form in which a value is parsed or formatted. The value types hold
structured data, so one value can be written in any of these forms.

**Syntax**

```csharp
public enum IcalValueFormat
```

## Remarks

The parse and format members of the value types take an `IcalValueFormat`, such as
`IcalDateTime.Parse`, `IcalValue.TryParseInteger` and `IcalValue.TryFormatText`.
They throw `ArgumentOutOfRangeException` for a value that isn't one of the members
below.

```csharp
var start = IcalDateTime.Parse("2011-05-17T12:00:00Z", IcalValueFormat.Xml);
var text = start.ToString(IcalValueFormat.Text);   // "20110517T120000Z"
```

**`Json` form.** A value is the text of a JSON string with its JSON escaping removed,
or the text of a JSON number or literal.

- TEXT values have no iCalendar escaping, and a list is one item, since each item is
  its own JSON string.
- BOOLEAN values are `true` or `false`, in lower case.
- INTEGER and FLOAT values are JSON numbers, which have no plus sign or leading zeros
  and may have an exponent. An INTEGER may have a fraction or an exponent as long as
  the number is a whole one, such as `1.0` or `1e2`.
- DATE, TIME, DATE-TIME and UTC-OFFSET values are as in `Xml` form. DURATION, BINARY,
  URI and CAL-ADDRESS values are as in `Text` form.
- A PERIOD is the start and the end or duration joined by a solidus, as in `Xml`
  form. In a jCal document they are the two strings of an array.
- A RECUR value has no form of its own, since in a jCal document it is a JSON object.
  So [IcalRecurrenceRule](IcalRecurrenceRule.md) throws `NotSupportedException` for
  this form. [JcalReader](JcalReader.md) and [JcalWriter](JcalWriter.md) handle the
  array and the object.

This form needs no JSON library, so the value types support it on every target,
`netstandard1.1` included. Only `JcalReader` and `JcalWriter` need System.Text.Json.

**`Xml` form.** A value is the content of an xCal value element.

- TEXT values have no escaping, and a list is one item, since each item is its own
  element.
- BOOLEAN values are `true` or `false`.
- DURATION, INTEGER, FLOAT, BINARY, URI and CAL-ADDRESS values are as in `Text` form.
  Parsing accepts what their XML Schema types allow, such as white space around a
  number or inside base64 data.
- A PERIOD is the start and the end or duration joined by a solidus, as in `Text`
  form. In an xCal document they are separate elements.
- A RECUR value has no form of its own, since in an xCal document each rule part is
  an element. So `IcalRecurrenceRule` throws `NotSupportedException` for this form.
  [XcalReader](XcalReader.md) and [XcalWriter](XcalWriter.md) handle those elements.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Text` | 0 | The text form of RFC 5545, used in `.ics` content lines; for example `20130101T120000Z` or `-0500`. |
| `Json` | 1 | The jCal form of RFC 7265, the text of a JSON value, which uses extended ISO 8601; for example `2013-01-01T12:00:00Z` or `-05:00`. |
| `Xml` | 2 | The xCal form of RFC 6321, the content of a value element, which uses extended ISO 8601; for example `2013-01-01T12:00:00Z` or `-05:00`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Read and write xCal](../how-to/read-write-xcal.md)
- [Read and write jCal](../how-to/read-write-jcal.md)
- [Convert between formats](../how-to/convert-formats.md)
- [IcalValue](IcalValue.md)
- [IcalValueType](IcalValueType.md)
- [IcalDateTime](IcalDateTime.md)
- [IcalRecurrenceRule](IcalRecurrenceRule.md)
- [File.iCal API reference](index.md)
