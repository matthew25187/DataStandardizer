---
title: DST gaps and overlaps
parent: File.iCal
grand_parent: Packages
nav_order: 13
---

# DST gaps and overlaps

When clocks change for daylight saving time, some local times don't happen and
others happen twice. A zoned iCalendar date-time is a local wall-clock time, so
it can fall in either. RFC 5545 says how to read such a time, and the package
applies that rule everywhere it converts local time to UTC.

All public types here are in the `DataStandardizer.File.iCal` namespace.

## Gaps and overlaps

- **Gap:** when clocks go forward, a period of local time is skipped. In
  America/New_York on 8 March 2026, clocks go from 02:00 EST to 03:00 EDT, so
  02:30 never happens. A local time in a gap has no instant.
- **Overlap:** when clocks go back, a period of local time happens twice. In
  America/New_York on 1 November 2026, clocks go from 02:00 EDT back to 01:00
  EST, so 01:30 happens once in EDT and again in EST. A local time in an overlap
  has two instants.

## The RFC 5545 rule

RFC 5545 §3.3.5 says:

> If, based on the definition of the referenced time zone, the local time
> described occurs more than once (when changing from daylight to standard time),
> the DATE-TIME value refers to the first occurrence of the referenced time.
> Thus, TZID=America/New_York:20071104T013000 indicates November 4, 2007 at 1:30
> A.M. EDT (UTC-04:00). If the local time described does not occur (when
> changing from standard to daylight time), the DATE-TIME value is interpreted
> using the UTC offset before the gap in local times. Thus,
> TZID=America/New_York:20070311T023000 indicates March 11, 2007 at 3:30 A.M. EDT
> (UTC-04:00), one hour after 1:30 A.M. EST (UTC-05:00).

So:

- **A time in a gap** is read with the UTC offset in use before the gap. This
  moves it forward by the length of the gap.
- **A time in an overlap** takes its first occurrence, the earlier instant.

## Worked examples

These results come from `IcalTimeZoneResolver.ToUtc` with
`Chain(Calendar, TzData, System)`:

| Zone | Local time | Case | UTC | Local time it shows |
| --- | --- | --- | --- | --- |
| America/New_York | 2026-03-08 02:30 | Gap (02:00 EST to 03:00 EDT) | `20260308T073000Z` | 03:30 EDT |
| America/New_York | 2026-11-01 01:30 | Overlap (02:00 EDT to 01:00 EST) | `20261101T053000Z` | 01:30 EDT, the first |
| Europe/London | 2026-03-29 01:30 | Gap (01:00 GMT to 02:00 BST) | `20260329T013000Z` | 02:30 BST |
| Europe/London | 2026-10-25 01:30 | Overlap (02:00 BST to 01:00 GMT) | `20261025T003000Z` | 01:30 BST, the first |

```csharp
var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

var inGap = IcalDateTime.Parse("20260308T023000", "America/New_York", IcalValueFormat.Text);
Console.WriteLine(resolver.ToUtc(inGap, null));       // 20260308T073000Z

var inOverlap = IcalDateTime.Parse("20261101T013000", "America/New_York", IcalValueFormat.Text);
Console.WriteLine(resolver.ToUtc(inOverlap, null));   // 20261101T053000Z
```

## Where the rule is applied

### `IcalTimeZoneRules`

Every resolver returns an `IcalTimeZoneRules`, and its conversions follow the
rule:

- `GetUtcOffsetsForLocal(local)` returns every offset in use at a local time, in
  chronological order: none in a gap, one normally, and two in an overlap.
- `ToUtc(local)` converts a local time to UTC. In a gap it takes the offset
  before the gap. In an overlap it takes the first offset.
- `ToLocal(utc)` converts an instant back to local time.

```csharp
var rules = IcalTimeZoneResolver.TzData.Resolve("America/New_York", null);

rules.GetUtcOffsetsForLocal(new DateTime(2026, 3, 8, 2, 30, 0));    // none
rules.GetUtcOffsetsForLocal(new DateTime(2026, 11, 1, 1, 30, 0));   // -0400, -0500

var utc = rules.ToUtc(new DateTime(2026, 3, 8, 2, 30, 0));          // 2026-03-08 07:30 UTC
var local = rules.ToLocal(utc);                                      // 2026-03-08 03:30
```

Local times must be of kind `DateTimeKind.Unspecified`. A time of kind `Local`,
which belongs to the machine's time zone, is rejected.

`IcalTimeZoneResolver.ToUtc(value, calendar)` uses these rules for an
`IcalDateTime`. A leap second, `:60`, is converted as the second before it and
then moved on by a second.

The rules are the same whichever resolver found the time zone: `Calendar`,
`TzData` or `System`.

### `DataStandardizer.Chronology`

The `TzData` resolver gets its local-time conversion from
`DataStandardizer.Chronology`, which you can also use directly:

- `TzDataExtensions.GetOffsetInfosForLocal(timezone, local)` returns the offsets
  in use at a local time: none in a gap, one normally, two in an overlap.
- `DateTimeExtensions.ToDateTimeOffset(local, timezone, resolution)` places a
  local time in a zone. `TzDataLocalTimeResolution.Rfc5545` applies the RFC 5545
  rule. `TzDataLocalTimeResolution.Strict` throws `ArgumentException` for a time
  in a gap or an overlap.

```csharp
using DataStandardizer.Chronology;

var newYork = TzDataTimezone.Parse("America/New_York");
var placed = new DateTime(2026, 3, 8, 2, 30, 0)
    .ToDateTimeOffset(newYork, TzDataLocalTimeResolution.Rfc5545);   // 2026-03-08T03:30:00-04:00
```

### Recurrence expansion

Recurrence rules are applied in local wall-clock time, and each occurrence is
converted to UTC afterwards:

1. `IcalRecurrenceRule.GetOccurrences` generates the occurrences of the `RRULE`
   as local times, with the time zone of `DTSTART`. It knows nothing of time
   zones, so a daily event at 02:30 gives 02:30 every day, even on the day that
   has no 02:30.
2. `GetOccurrences` on `IcalEvent`, `IcalTodo` and `IcalJournal` adds `RDATE`,
   removes `EXDATE` and applies overrides, then resolves each instance to UTC
   with the resolver, following the RFC 5545 rule.

So a daily event keeps its local time, and its time in UTC moves by an hour
across a change. For a daily event at 02:30 in America/New_York starting on
6 March 2026:

```text
DTSTART;TZID=America/New_York:20260306T023000
RRULE:FREQ=DAILY;COUNT=4
```

```csharp
foreach (var occurrence in calendarEvent.GetOccurrences(calendar, null, null))
{
    Console.WriteLine($"{occurrence.Start}  {occurrence.StartUtc}");
}
```

| `Start` (local) | `StartUtc` | Note |
| --- | --- | --- |
| `20260306T023000` | `20260306T073000Z` | EST |
| `20260307T023000` | `20260307T073000Z` | EST |
| `20260308T023000` | `20260308T073000Z` | In the gap: read with EST, so 03:30 EDT |
| `20260309T023000` | `20260309T063000Z` | EDT |

`IcalOccurrence.Start` keeps the local time the recurrence set gives, even where
it falls in a gap. `StartUtc` gives the instant. Across the fall-back change, a
daily event at 01:30 from 31 October 2026 gives `20261031T053000Z`, then
`20261101T053000Z` (the first 01:30, in EDT), then `20261102T063000Z`.

When a rule's `UNTIL` is in UTC and `DTSTART` is zoned, each local occurrence has
to be converted to UTC to compare it with `UNTIL`. Component expansion does this
for you. When you call the rule engine yourself, use the overload of
`IcalRecurrenceRule.GetOccurrences` that takes a `toUtc` function.

## Durations across a change

The end of an instance also depends on how its length is given. RFC 5545 has two
kinds of duration:

- **Nominal:** weeks and days, such as `P1D`. They keep the local time of day,
  so a day can be 23 or 25 hours long.
- **Exact:** hours, minutes and seconds, such as `PT24H`. They are a fixed
  length of time.

For an event at 09:00 on 7 March 2026 in America/New_York, the day before clocks
go forward:

| `DURATION` | End (local) | End (UTC) |
| --- | --- | --- |
| `P1D` | `20260308T090000` | `20260308T130000Z` |
| `PT24H` | `20260308T100000` | `20260308T140000Z` |

A `DTEND` gives every instance the exact duration it gives `DTSTART`, so the
local time of the end can change across a change.

## Related

- [Time-zone resolution](time-zone-resolution.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [The layered model](layered-model.md)
- [IcalTimeZoneResolver reference](../reference/IcalTimeZoneResolver.md)
- [API reference](../reference/index.md)
