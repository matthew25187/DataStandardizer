---
title: Read with zero copies
parent: File.iCal
grand_parent: Packages
nav_order: 3
---

# Read with zero copies

`IcalContentLineReader` reads the content lines of `.ics` data, the layer below the
component object model. Its `ReadContentLine` method reads the next line into the
reader's own buffer, and `CurrentLine` gives a view of it, an
`IcalContentLineView`. The view hands out spans of that buffer, so you can find the
lines and values you want without allocating a string for each one.

Use this when you need only a few properties from a large file, or want to keep
allocations down. For everything else, `IcalReader` is simpler. See
[The layered model](../concepts/layered-model.md).

## Scan the lines

```csharp
using DataStandardizer.File.iCal;

using var reader = new IcalContentLineReader("calendar.ics");
while (reader.ReadContentLine())
{
    var line = reader.CurrentLine;
    if (!line.NameEquals("DTSTART")) continue;

    var tzidIndex = line.IndexOfParameter("TZID");
    var timeZoneId = tzidIndex < 0 ? null : line.GetParameterValue(tzidIndex, 0).ToString();
    var start = IcalDateTime.Parse(line.RawValue, timeZoneId, IcalValueFormat.Text);
    Console.WriteLine($"{line.LineNumber}: {start}");
}
```

`ReadContentLine` returns `false` at the end of the data. Each line is unfolded, and
its structure (a name, any parameters and a value) is checked against RFC 5545
§3.1. Nothing else is checked: every line is given, `BEGIN` and `END` included, and
the meaning of properties and values is left to you.

This sample assumes each `DTSTART` is a DATE-TIME. If yours can be a DATE, check
`IcalValueTypeRegistry.GetValueType(line)` first, or use `TryParse`.

`IcalContentLineReader` takes the same sources and options as `IcalReader`: a file
path (.NET Standard 2.0 or later), a `Stream` or a `TextReader`. The reader and the
view are available on every target, .NET Standard 1.1 included.

## What the view gives

| Member | Gives |
| --- | --- |
| `Name` | The property name, as written. |
| `NameEquals(name)` | Whether the name matches, compared case-insensitively. Use this rather than comparing `Name` yourself. |
| `RawValue` | The value as it appears in the line once unfolded, with any TEXT escaping in place. |
| `GetTextValue()` | The value with TEXT escaping removed. |
| `RawLine` | The whole unfolded line, without its line break. |
| `ParameterCount` | The number of parameters. |
| `GetParameterName(i)` | The name of parameter `i`. |
| `IndexOfParameter(name)` | The index of the first parameter of a name, or -1. |
| `GetParameterValueCount(i)` | The number of values parameter `i` has; always at least 1. |
| `GetParameterValue(i, j)` | Value `j` of parameter `i`, with RFC 6868 caret encoding decoded. |
| `GetRawParameterValue(i, j)` | The same value as written, with caret encoding in place. |
| `IsParameterValueQuoted(i, j)` | Whether the value was in double quotes. |
| `LineNumber`, `PhysicalLineNumber` | Where the line is: its logical line, and the physical line it starts on. |

Each span member has a `Memory` variant that returns `ReadOnlyMemory<char>`, such as
`RawValueMemory`, `NameMemory`, `GetParameterValueMemory` and `GetTextValueMemory`.
Use those where a span can't go, such as across an `await` or into a field. The same
rules on how long they stay valid apply.

### Parameters

Parameters are reached by index. Quotes around a value are removed in both forms.
`GetParameterValue` decodes RFC 6868 caret encoding (`^n` becomes a line feed, `^'`
a double quote and `^^` a caret), and `GetRawParameterValue` leaves it in place:

```csharp
// ATTENDEE;CN="George Herman ^'Babe^' Ruth";DELEGATED-TO="mailto:a@example.com","mailto:b@example.com":mailto:g@example.com
var line = reader.CurrentLine;
for (var parameterIndex = 0; parameterIndex < line.ParameterCount; parameterIndex++)
{
    for (var valueIndex = 0; valueIndex < line.GetParameterValueCount(parameterIndex); valueIndex++)
    {
        Console.WriteLine($"{line.GetParameterName(parameterIndex)}: " +
            $"{line.GetRawParameterValue(parameterIndex, valueIndex)} -> " +
            $"{line.GetParameterValue(parameterIndex, valueIndex)}");
    }
}
```

```text
CN: George Herman ^'Babe^' Ruth -> George Herman "Babe" Ruth
DELEGATED-TO: mailto:a@example.com -> mailto:a@example.com
DELEGATED-TO: mailto:b@example.com -> mailto:b@example.com
```

### TEXT values

`GetTextValue()` removes TEXT escaping: `\\`, `\;` and `\,` become the escaped
character, and `\n` or `\N` a line feed. A backslash that doesn't start a valid
escape throws `IcalFileException`. Use it only for properties whose value type is
TEXT.

A property that holds a list of TEXT values, such as `CATEGORIES`, must be split on
the unescaped commas before the escaping is removed. Split `RawValue` with
`IcalValue.EnumerateTextList`, then unescape each item with `IcalValue.TryParseText`:

```csharp
Span<char> buffer = stackalloc char[256];
foreach (var item in IcalValue.EnumerateTextList(line.RawValue, IcalValueFormat.Text))
{
    if (item.Length <= buffer.Length
        && IcalValue.TryParseText(item, IcalValueFormat.Text, buffer, out var length))
    {
        Console.WriteLine(buffer.Slice(0, length).ToString());
    }
}
```

The destination must be at least as long as the item.

Decoded values, from `GetParameterValue` and `GetTextValue`, are decoded the first
time you ask and kept until the reader moves on, so asking again costs nothing. A
value with nothing to decode is a slice of the line itself.

## Parse values from spans

Every value type parses from a `ReadOnlySpan<char>`, so you can parse straight from
the view:

| Value type | Parse with |
| --- | --- |
| DATE | `IcalDate.Parse` and `TryParse` |
| DATE-TIME | `IcalDateTime.Parse` and `TryParse`, with an optional time zone ID from `TZID` |
| TIME | `IcalTime.Parse` and `TryParse` |
| DURATION | `IcalDuration.Parse` and `TryParse` |
| PERIOD | `IcalPeriod.Parse` and `TryParse`, with an optional time zone ID |
| RECUR | `IcalRecurrenceRule.Parse` and `TryParse` |
| UTC-OFFSET | `IcalUtcOffset.Parse` and `TryParse` |
| BOOLEAN, INTEGER, FLOAT | `IcalValue.TryParseBoolean`, `TryParseInteger` and `TryParseFloat` |
| BINARY, URI, CAL-ADDRESS | `IcalValue.TryParseBinary`, `TryParseUri` and `TryParseCalAddress` |
| TEXT | `IcalValue.TryParseText` and `EnumerateTextList` |

Pass `IcalValueFormat.Text` for a value from an `.ics` line.

To find a line's value type, call `IcalValueTypeRegistry.GetValueType(line)`. It
uses the line's `VALUE` parameter, or else the property's default type. It returns
`IcalValueType.Unknown` for a property it doesn't know, such as an `X-` property,
even one with a `VALUE` parameter.

## Keep a line

To keep a line after the reader moves on, copy it with `ToContentLine()`. The
`IcalContentLine` it returns doesn't depend on the reader:

```csharp
var attendees = new List<IcalContentLine>();
using var reader = new IcalContentLineReader("calendar.ics");
while (reader.ReadContentLine())
{
    var line = reader.CurrentLine;
    if (line.NameEquals("ATTENDEE"))
    {
        attendees.Add(line.ToContentLine());
    }
}
```

`ReadLine()` reads the next line and returns such a copy directly, or `null` at the
end of the data.

## How long a view and its spans are valid

A view, and every span or memory taken from it, is valid until the reader reads
another line or is disposed. That means until the next call to any of these:

- `ReadContentLine`, `ReadLine` or `ReadLines` (as it is enumerated)
- `ReadContentLineAsync`, `ReadLineAsync` or `ReadLinesAsync`. An asynchronous read
  ends the view as soon as it is called, not when its task completes.
- `Dispose` or `DisposeAsync`

After that, the view checks itself. Each view carries a version stamp of the line it
was taken for, and every member compares it with the reader's current line:

- **The reader has moved on:** every member of the old view throws
  `InvalidOperationException` ("The reader has moved on from the content line of
  this view.").
- **The reader has been disposed:** every member throws `ObjectDisposedException`.

Spans and memory taken out of a view can't be checked in the same way. Using one
after the reader has moved on gives undefined content: it may still show the old
text, or part of a later line, since the buffer is reused and returned to a shared
pool. So copy anything you need to keep, with `ToString()` or `ToContentLine()`,
before the next read.

`ReadLine` and `ReadContentLine` both make their line current, so `CurrentLine`
works after either. `CurrentLine` throws `InvalidOperationException` before the
first read, at the end of the data, and after a read that threw.

## Errors

A line that doesn't follow the content-line syntax throws `IcalFileException` after
the line has been read, so reading can go on with the next line. Its `Data` holds
the `LineNumber`, the `PhysicalLineNumber` and, once it has been read, the
`PropertyName`.

A bare line feed, or a line longer than `IcalOptions.MaxLineLength`, also throws
`IcalFileException`. Then where the line ends is unknown, so later reads throw
`InvalidOperationException`. See
[Read non-compliant feeds](read-non-compliant-feeds.md).

## Related

- [Read and write .ics files](read-write-ics-files.md)
- [Read asynchronously](read-asynchronously.md)
- [The layered model](../concepts/layered-model.md)
- [IcalContentLineReader](../reference/IcalContentLineReader.md),
  [IcalContentLineView](../reference/IcalContentLineView.md),
  [IcalContentLine](../reference/IcalContentLine.md) and
  [IcalValue](../reference/IcalValue.md)
