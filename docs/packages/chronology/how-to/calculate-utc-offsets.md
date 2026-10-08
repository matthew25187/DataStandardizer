---
title: Calculate UTC offsets
parent: Chronology
grand_parent: Packages
nav_order: 3
---

# Calculate UTC offsets

Each time zone carries its full history from the TZ Database: the offsets from
universal time it has observed, and the daylight saving rules that change them.
The extension methods on [TzDataExtensions](../reference/TzDataExtensions.md)
use that history to tell you the offset in effect at any instant, whether
daylight saving time applies, when the offset changes, and the abbreviation by
which the local time is known.

The examples on this page use the following namespace:

```csharp
using DataStandardizer.Chronology;
```

## Pass instants in universal time

Every method takes the instant as a `DateTime` in universal time or as a
`DateTimeOffset`:

- A `DateTime` of kind `Utc` is used as it is.
- A `DateTime` of kind `Unspecified` is treated as universal time, so make sure
  that it is one.
- A `DateTime` of kind `Local` throws `ArgumentException`. Convert it with
  `ToUniversalTime()` first, or pass a `DateTimeOffset`.
- A `DateTimeOffset` is converted to universal time through its `UtcDateTime`.

The methods answer questions about an instant. They don't convert a local wall
clock time in the time zone to an instant, which can be ambiguous or invalid
around a transition.

## Get the offset at an instant

`GetUtcOffset` returns the total offset from universal time, including any
daylight saving:

```csharp
var newYork = TzDataTimezone.America.New_York;
var instant = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

TimeSpan offset = newYork.GetUtcOffset(instant);   // -04:00:00
```

Apply the offset to get the local time as a `DateTimeOffset`:

```csharp
var local = new DateTimeOffset(instant).ToOffset(offset);   // 2026-07-01 08:00:00 -04:00
```

The offset is split into the standard offset and the daylight saving added to
it:

```csharp
TimeSpan standard = newYork.GetStandardOffset(instant);   // -05:00:00
TimeSpan savings = newYork.GetDaylightSavings(instant);   // 01:00:00
```

Historical offsets are kept to the second. Before New York adopted standard time
in 1883 it kept local mean time:

```csharp
var earlier = new DateTime(1880, 1, 1, 0, 0, 0, DateTimeKind.Utc);
TimeSpan lmt = newYork.GetUtcOffset(earlier);   // -04:56:02
```

## Find out whether daylight saving time applies

```csharp
bool isDaylight = newYork.IsDaylightSavingTime(instant);   // true
```

The TZ Database decides which time is daylight saving time, and it is not always
the summer time. Europe/Dublin observes Irish Standard Time (`IST`, +01:00) in
summer and saves a negative hour in winter, so its winter time is daylight
saving time:

```csharp
var dublin = TzDataTimezone.Europe.Dublin;
var january = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
var july = new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

dublin.IsDaylightSavingTime(january);   // true
dublin.GetDaylightSavings(january);     // -01:00:00
dublin.IsDaylightSavingTime(july);      // false
dublin.GetUtcOffset(july);              // 01:00:00
```

Check `IsDaylightSavingTime` rather than comparing the daylight saving amount
with zero.

## Get everything at once

`GetOffsetInfo` returns a [TzDataOffsetInfo](../reference/TzDataOffsetInfo.md)
with the offset, its parts, the abbreviation, and the period over which they
stay in effect:

```csharp
TzDataOffsetInfo info = newYork.GetOffsetInfo(instant);

Console.WriteLine(info.UtcOffset);              // -04:00:00
Console.WriteLine(info.StandardOffset);         // -05:00:00
Console.WriteLine(info.DaylightSavings);        // 01:00:00
Console.WriteLine(info.IsDaylightSavingTime);   // True
Console.WriteLine(info.Abbreviation);           // EDT
Console.WriteLine($"{info.ValidFromUtc:u}");    // 2026-03-08 07:00:00Z
Console.WriteLine($"{info.ValidUntilUtc:u}");   // 2026-11-01 06:00:00Z
```

`ValidFromUtc` is `null` where the offset has applied since the beginning of the
zone's history, and `ValidUntilUtc` is `null` where it applies indefinitely, as
for a zone that no longer changes its clocks.

## List the transitions over a period

`GetTransitions` returns each [TzDataTransition](../reference/TzDataTransition.md)
from the start of a period, inclusive, to its end, exclusive, in chronological
order. Each one gives the instant of the change and the offsets before and
after it:

```csharp
var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
var to = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

foreach (var transition in newYork.GetTransitions(from, to))
{
    Console.WriteLine($"{transition.InstantUtc:u} {transition.Before.Abbreviation} -> {transition.After.Abbreviation}");
}
```

Output:

```text
2026-03-08 07:00:00Z EST -> EDT
2026-11-01 06:00:00Z EDT -> EST
```

A transition is reported wherever the offset, the standard offset, daylight
saving time or the abbreviation changes. The instant of a transition takes the
new offset, and the instant before it the old one.

Rules that the TZ Database gives as continuing indefinitely are applied to any
future year, so there is no fixed upper limit:

```csharp
var london = TzDataTimezone.Europe.London;
var transitions = london.GetTransitions(
    new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    new DateTime(2101, 1, 1, 0, 0, 0, DateTimeKind.Utc));
// 2100-03-28 01:00:00Z GMT -> BST
// 2100-10-31 01:00:00Z BST -> GMT
```

Future transitions are predictions. They change when a government changes its
rules and the TZ Database, and then this package, are updated.

## Format abbreviations

`GetAbbreviation` returns the abbreviation in effect at an instant. It is the
same as `TzDataOffsetInfo.Abbreviation`:

```csharp
newYork.GetAbbreviation(instant);   // EDT
```

`GetStandardAbbreviation` and `GetDaylightAbbreviation` return the
abbreviations for standard and daylight saving time in the zone line and year in
effect at the instant, whichever of the two is in effect at that instant:

```csharp
newYork.GetStandardAbbreviation(instant);   // EST
newYork.GetDaylightAbbreviation(instant);   // EDT

london.GetStandardAbbreviation(july);       // GMT
london.GetDaylightAbbreviation(july);       // BST

dublin.GetStandardAbbreviation(july);       // IST
dublin.GetDaylightAbbreviation(july);       // GMT
```

`GetDaylightAbbreviation` returns `null` where no daylight saving time is
observed in the year:

```csharp
var tokyo = TzDataTimezone.Asia.Tokyo;
tokyo.GetStandardAbbreviation(instant);   // JST
tokyo.GetDaylightAbbreviation(instant);   // null
```

These are the TZ Database's own abbreviations. Many zones have no
abbreviation in common use, and the TZ Database gives their offset as a number
instead:

```csharp
TzDataTimezone.Asia.Dubai.GetAbbreviation(instant);       // +04
TzDataTimezone.Asia.Kathmandu.GetAbbreviation(instant);   // +0545
TzDataTimezone.Asia.Kolkata.GetAbbreviation(instant);     // IST
```

Abbreviations aren't unique: `IST` is used in both India and Ireland. Don't use
them to identify a time zone.

Historical abbreviations are kept as well. New York observed War Time from 1942
to 1945:

```csharp
newYork.GetAbbreviation(new DateTime(1943, 1, 1, 0, 0, 0, DateTimeKind.Utc));   // EWT
```

## Look at the underlying zone lines

The calculations are based on the zone's zone lines, which you can read
directly. `GetZoneLine` returns the [TzDataZoneLine](../reference/TzDataZoneLine.md)
in effect at an instant, and `ZoneLines` returns all of them:

```csharp
TzDataZoneLine line = newYork.GetZoneLine(instant);

Console.WriteLine(line.StandardOffset);       // -05:00:00
Console.WriteLine(line.RuleKind);             // RuleSet
Console.WriteLine(line.Format);               // E%sT
Console.WriteLine(line.Until is null);        // True, as the line applies indefinitely
Console.WriteLine(newYork.ZoneLines.Count);   // 6
```

The [TzDataRule](../reference/TzDataRule.md) instances in `Rules` give the
daylight saving rules. `GetTransitionDate` returns the local date of a rule's
transition in a given year. This example also needs `using System.Linq;`:

```csharp
foreach (var rule in line.Rules.Where(r => r.ToYear == int.MaxValue))
{
    Console.WriteLine($"{rule.GetTransitionDate(2026):yyyy-MM-dd} {rule.AtTime} save {rule.Save}");
}
```

Output:

```text
2026-03-08 02:00:00 save 01:00:00
2026-11-01 02:00:00 save 00:00:00
```

Most callers don't need the zone lines. Use the methods above for offsets and
abbreviations.

## Limitations

- **History before 1970.** The TZ Database records the history of each zone's
  principal location, the place it is named after, and guarantees it only from
  1970. Before then, other places in the same zone may have kept different time.
  A link, such as `Europe/Oslo`, has the history of its canonical time zone,
  `Europe/Berlin`. See
  [Work with links and deprecated names](use-timezones.md#work-with-links-and-deprecated-names).
- **Abbreviations.** Only the TZ Database's abbreviations are available, and
  many of them are numeric, such as `+04`. Long names such as "Eastern Standard
  Time", and localized names, come from the Unicode CLDR and are out of scope.
- **Leap seconds.** Leap seconds are ignored, as they are by `DateTime`.
- **Universal time.** `DateTime` arguments must be in universal time. See
  [Pass instants in universal time](#pass-instants-in-universal-time).
- **Invalid instances.** The methods throw `InvalidOperationException` for
  `default(TzDataTimezone)` and for an instance cast from an identifier that
  isn't known. Get time zones from the predefined fields, or with `Parse` or
  `TryParse`.

## Next steps

- [TzDataExtensions reference](../reference/TzDataExtensions.md)
- [Use time zones](use-timezones.md)
