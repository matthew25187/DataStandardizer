---
title: Read with zero copies
parent: File.vCard
grand_parent: Packages
nav_order: 3
---

# Read with zero copies

`VcardContentLineReader` reads the content lines of `.vcf` data, the layer below the
card model. Its `ReadContentLine` method reads the next line into the reader's own
buffer, and `CurrentLine` gives a view of it, a `VcardContentLineView`. The view
hands out spans of that buffer, so you can find the lines and values you want
without allocating a string for each one.

Use this when you need only a few properties from a large file, or want to keep
allocations down. For everything else, `VcardReader` is simpler. See
[The layered model](../concepts/layered-model.md).

## Scan the lines

```csharp
using DataStandardizer.File.vCard;

using var reader = new VcardContentLineReader("contacts.vcf");
var isVersion21 = false;
while (reader.ReadContentLine())
{
    var line = reader.CurrentLine;
    if (line.NameEquals("VERSION"))
    {
        isVersion21 = line.RawValue.SequenceEqual("2.1".AsSpan());
        continue;
    }

    if (!line.NameEquals("EMAIL")) continue;

    var typeIndex = line.IndexOfParameter("TYPE");
    var type = typeIndex < 0 ? "" : line.GetParameterValue(typeIndex, 0).ToString();
    var email = isVersion21 ? line.GetDecodedText() : line.GetTextValue().ToString();
    Console.WriteLine($"{line.LineNumber}: {type} {email}");
}
```

A vCard 2.1 value can be in quoted-printable, such as `alice=40example.com`, and has
no TEXT escaping, so it is decoded with `GetDecodedText()`, which allocates. A value of
a later version is unescaped with `GetTextValue()`. The view doesn't give the version
of the card a line belongs to, so the example follows it from each `VERSION` line.

`ReadContentLine` returns `false` at the end of the data. Each line is unfolded, and
its structure (an optional group, a name, any parameters and a value) is checked
against RFC 6350 §3.3. Nothing else is checked: every line is given, `BEGIN`,
`VERSION` and `END` included, and the meaning of properties and values is left to
you.

`VcardContentLineReader` takes the same sources and options as `VcardReader`: a file
path (.NET Standard 2.0 or later), a `Stream` or a `TextReader`. The reader and the
view are available on every target, .NET Standard 1.1 included.

## What the view gives

| Member | Gives |
| --- | --- |
| `Name` | The property name, as written. |
| `NameEquals(name)` | Whether the name matches, compared case-insensitively. Use this rather than comparing `Name` yourself. |
| `Group`, `GroupEquals(group)` | The group, such as `item1` in `item1.EMAIL`, or an empty span. |
| `RawValue` | The value as it appears in the line once unfolded, with any TEXT escaping in place. |
| `GetTextValue()` | The value with TEXT escaping removed. |
| `RawLine` | The whole unfolded line, without its line break. |
| `ParameterCount` | The number of parameters. |
| `GetParameterName(i)` | The name of parameter `i`. For a vCard 2.1 parameter with no name, such as `HOME` in `TEL;HOME:`, the name it belongs to, here `TYPE`. |
| `IndexOfParameter(name)` | The index of the first parameter of a name, or -1. |
| `GetParameterValueCount(i)` | The number of values parameter `i` has; always at least 1. |
| `GetParameterValue(i, j)` | Value `j` of parameter `i`, with RFC 6868 caret encoding decoded in a vCard 4.0 card. |
| `GetRawParameterValue(i, j)` | The same value as written, with caret encoding in place. |
| `IsParameterValueQuoted(i, j)` | Whether the value was in double quotes. |
| `GetDecodedText()`, `GetDecodedValue()` | The value decoded by its `ENCODING` and `CHARSET`, for a vCard 2.1 or 3.0 card. These allocate. See [Handle legacy character sets](handle-legacy-character-sets.md). |
| `LineNumber`, `PhysicalLineNumber` | Where the line is: its logical line, and the physical line it starts on. |
| `ToContentLine()` | A copy of the line, as a `VcardContentLine`, that stays valid after the reader moves on. |

Each span member has a `Memory` variant that returns `ReadOnlyMemory<char>`, such as
`RawValueMemory`, `NameMemory`, `GetParameterValueMemory` and `GetTextValueMemory`.
Use those where a span can't go, such as across an `await` or into a field. The same
rules on how long they stay valid apply.

### Parameters

Parameters are reached by index. Quotes around a value are removed in both forms.
`GetParameterValue` decodes RFC 6868 caret encoding (`^n` becomes a line feed, `^'` a
double quote and `^^` a caret), and `GetRawParameterValue` leaves it in place. Caret
encoding applies only in a vCard 4.0 card, from its `VERSION:4.0` line up to and
including its `END:VCARD`, since RFC 6868 doesn't update vCard 3.0 or 2.1.

```csharp
// ADR;TYPE=work,postal;LABEL="Level 4^n1 Willis Street^nWellington":;;1 Willis Street;Wellington;;6011;New Zealand
var line = reader.CurrentLine;
for (var parameterIndex = 0; parameterIndex < line.ParameterCount; parameterIndex++)
{
    for (var valueIndex = 0; valueIndex < line.GetParameterValueCount(parameterIndex); valueIndex++)
    {
        Console.WriteLine($"{line.GetParameterName(parameterIndex).ToString()}: " +
            $"{line.GetParameterValue(parameterIndex, valueIndex).ToString()}");
    }
}
```

```text
TYPE: work
TYPE: postal
LABEL: Level 4
1 Willis Street
Wellington
```

### TEXT values

`GetTextValue()` removes TEXT escaping: `\\`, `\;` and `\,` become the escaped
character, and `\n` or `\N` a line feed (RFC 6350 §3.4). A backslash that doesn't
start one of these throws `VcardFileException`. A vCard 2.1 line, which has no such
escaping, is given as it is. Use it only for a property whose value is a single TEXT
value, and check the type with `VcardValueTypeRegistry.GetValueType(line)` if it can
vary.

A property that holds a list, such as `CATEGORIES` or `NICKNAME`, must be split on
the commas that aren't escaped before the escaping is removed. Split `RawValue` with
`VcardValue.EnumerateList`, then unescape each item with `VcardValue.TryParseText`:

```csharp
Span<char> stackBuffer = stackalloc char[256];
foreach (var item in VcardValue.EnumerateList(line.RawValue, VcardValueFormat.Text))
{
    // The unescaped text is never longer than the item, so an item that doesn't fit gets an array of its own.
    Span<char> buffer = item.Length <= stackBuffer.Length ? stackBuffer : new char[item.Length];
    if (VcardValue.TryParseText(item, VcardValueFormat.Text, buffer, out var length))
    {
        Console.WriteLine(buffer.Slice(0, length).ToString());
    }
}
```

`EnumerateList` splits by the rules of vCard 3.0 and 4.0, where `\,` is an escaped
comma, not a separator, and this unescapes by the rules of vCard 4.0. For a line of
a vCard 3.0 card, pass `VcardVersion.V3_0` to the overload of `TryParseText` that
takes a version. vCard 2.1 defines no property that holds a list, and has no TEXT
escaping. So a list in a vCard 2.1 extension property is split by that extension's
own syntax, not with `EnumerateList`. Each item is then given to `TryParseText` with
`VcardVersion.V2_1`, which keeps `\N` as written.

### Structured values

`N`, `ADR` and `ORG` hold components separated by semicolons, and each component can
be a list. `VcardName.TryParseView`, `VcardAddress.TryParseView` and
`VcardOrganization.TryParseView` check the value and give a
`VcardStructuredValueView` of its components, without copying anything:

```csharp
// N:Doe;Jane;Ann,Marie;Dr.;
if (VcardName.TryParseView(line.RawValue, VcardValueFormat.Text, out var name))
{
    var family = name.GetComponent(VcardName.FamilyNamesComponent);
    var given = name.GetComponent(VcardName.GivenNamesComponent);
    Console.WriteLine($"{given.RawValue.ToString()} {family.RawValue.ToString()}"); // Jane Doe
}
```

`GetComponent` scans from the start of the value each time. To read every component,
enumerate the view, which takes one pass, and enumerate a component for its items.
`UnescapeItem` removes the escaping of an item into a buffer you provide.

### Typed values

The value types parse from a span, and most of them without allocating:

```csharp
if (line.NameEquals("BDAY")
    && VcardValueTypeRegistry.GetValueType(line) == VcardValueType.DateAndOrTime
    && VcardDateAndOrTime.TryParse(line.RawValue, VcardValueFormat.Text, out var birthday))
{
    Console.WriteLine(birthday);
}
```

A value of vCard 3.0 or 2.1 is written in that version's form, such as
`BDAY:1985-04-12`. Pass the version to the overloads that take one.

## How long a view lives

A view, and every span it gives, points into the reader's buffer, which the next
read reuses.

- **The view goes stale** as soon as the next read starts, and when the reader is
  disposed. Every member of a stale view throws: `InvalidOperationException`, or
  `ObjectDisposedException` once the reader is disposed.
- **A span or memory taken from the view** can't check this. Using one after the
  reader has moved on throws nothing and gives undefined content.

So copy anything that must outlive the line: call `ToString()` on a span, or
`ToContentLine()` on the view.

```csharp
var emails = new List<string>();
var isVersion21 = false;
while (reader.ReadContentLine())
{
    var line = reader.CurrentLine;
    if (line.NameEquals("VERSION"))
    {
        isVersion21 = line.RawValue.SequenceEqual("2.1".AsSpan());
    }
    else if (line.NameEquals("EMAIL"))
    {
        emails.Add(isVersion21 ? line.GetDecodedText() : line.GetTextValue().ToString());
    }
}
```

## Allocation

`ReadContentLine` and the members of the view allocate nothing per line. The reader
allocates a fixed amount for its buffers and its `StreamReader` when it is created,
whatever the size of the data, and rents a larger buffer only for a very long line. `GetParameterValue` and `GetTextValue` decode into a scratch buffer the
first time they are asked, and keep the result until the reader moves on. The
benchmarks read 5,000 cards, about 4.9 MB, this way with about 80 KB allocated in
all.

## Related

- [The layered model](../concepts/layered-model.md)
- [The shared content-line engine](../concepts/content-line-engine.md)
- [Read asynchronously](read-asynchronously.md)
- [VcardContentLineReader](../reference/VcardContentLineReader.md),
  [VcardContentLineView](../reference/VcardContentLineView.md) and
  [VcardStructuredValueView](../reference/VcardStructuredValueView.md)
