---
title: Time-zone resolution
parent: File.iCal
grand_parent: Packages
nav_order: 12
---

# Time-zone resolution

A zoned date-time in iCalendar, such as `DTSTART;TZID=America/New_York:20260115T090000`,
is a wall-clock time plus the name of a time zone. To turn it into an instant, the
package has to find the rules of that time zone. A time-zone resolver does this.
By default the rules come only from the calendar itself, as RFC 5545 requires.
Other sources are opt-in.

All public types here are in the `DataStandardizer.File.iCal` namespace.

## Resolvers

A resolver implements `IIcalTimeZoneResolver`. It has one method, which is
synchronous:

```csharp
bool TryResolve(string timeZoneId, IcalCalendar? calendar, out IcalTimeZoneRules? rules);
```

It returns `true` and the rules of the time zone if it knows the identifier, and
`false` otherwise. A resolver must be safe to use from several threads at once.

`IcalTimeZoneRules` gives the UTC offset in use at an instant, the transitions
between offsets over a period, and conversions between local time and UTC.

The static `IcalTimeZoneResolver` class holds the built-in resolvers:

| Resolver | Source | Notes |
| --- | --- | --- |
| `Calendar` | The calendar's own VTIMEZONE whose `TZID` matches, compared ordinally. | The default. Resolves any `TZID` the calendar defines, including Windows names. Resolves nothing when no calendar is given. |
| `TzData` | The TZ Database, through `DataStandardizer.Chronology`. | Opt-in. Ignores the calendar. Doesn't know Windows names. |
| `System` | The operating system, through `TimeZoneInfo.FindSystemTimeZoneById`. | Opt-in, and a last resort. .NET Standard 2.0 and later only. |
| `Default` | The same as `Calendar`. | |

`TzData` and `System` both accept a vendor prefix. A `TZID` that starts with a
solidus is globally unique (RFC 5545 §3.8.3.1), and vendors write one by putting
a path in front of the TZ Database name, as in
`/mozilla.org/20050126_1/America/New_York`. Each trailing part of the path is
tried in turn.

## Chains

`IcalTimeZoneResolver.Chain(...)` combines resolvers into one. It tries each in
order, and the first that resolves the identifier wins. A resolver that doesn't
know the identifier returns `false`, and the chain moves on to the next. So the
order of the chain decides which source is used when two know the same zone.

```csharp
var resolver = IcalTimeZoneResolver.Chain(
    IcalTimeZoneResolver.Calendar,
    IcalTimeZoneResolver.TzData,
    IcalTimeZoneResolver.System);
```

When nothing resolves an identifier, `Resolve` and `ToUtc` throw
`IcalFileException`, and so does occurrence expansion. To treat such a time as
floating instead, call `TryResolve` yourself and handle the result.

## Why `Calendar` is the default

RFC 5545 §3.2.19 says:

> An individual "VTIMEZONE" calendar component MUST be specified for each unique
> "TZID" parameter value specified in the iCalendar object.

and §3.6.5 says that the VTIMEZONE defines the time zone. So a compliant
calendar carries the definition of every time zone it uses, and that definition
is what its author meant. The default resolver reads only that. Nothing outside
the calendar can contradict it, and the same calendar gives the same times on
every machine.

The `Calendar` resolver takes the offsets from the observances of the
VTIMEZONE: the onsets given by each observance's `DTSTART`, `RRULE` and `RDATE`,
and its `TZOFFSETFROM` and `TZOFFSETTO`. Before the first onset, the
`TZOFFSETFROM` of that onset is in use.

This order matters. Given a calendar whose VTIMEZONE for `America/New_York`
defines a fixed offset of `+0100`, `09:00` on 15 January 2026 resolves as:

| Chain | Result |
| --- | --- |
| `Chain(Calendar, TzData)` | `20260115T080000Z`, from the calendar's definition |
| `Chain(TzData, Calendar)` | `20260115T140000Z`, from the TZ Database |

## Calendars with undefined time zones

Many calendars use a `TZID` with no VTIMEZONE. Such a calendar breaks RFC 5545,
and the reader rejects it unless `IcalOptions.AllowUndefinedTimeZones` is set.
Even with that option, `Default` can't resolve the time zone, so resolution
throws:

```text
The time zone 'America/New_York' could not be resolved.  RFC 5545 requires a VTIMEZONE
for it in the calendar.
```

To read such a calendar, set the option and chain `TzData` after `Calendar`:

```csharp
var options = new IcalOptions { AllowUndefinedTimeZones = true };
var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

using var reader = new IcalReader("feed.ics", options);
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var calendarEvent in calendar.Events)
    {
        if (calendarEvent.DtStart is { IsDate: false } start &&
            start.DateTime.Kind != IcalDateTimeKind.Floating)
        {
            Console.WriteLine(resolver.ToUtc(start.DateTime, calendar));
        }
    }
}
```

`TzData` then fills in only the time zones the calendar leaves undefined. It
never overrides one the calendar defines. `TzData` always holds every time zone
of the TZ Database release that `DataStandardizer.Chronology` was built from, so
it gives the same result on every machine.

If you distrust the calendar's own definitions, for example because they are
truncated, you can use `Chain(TzData, Calendar)` instead. This departs from
RFC 5545, because it prefers the TZ Database to the calendar.

## The `System` resolver, last

`System` resolves through the operating system's time zones. From .NET 6 on, it
knows both TZ Database names, such as `America/New_York`, and Windows names, such
as `Eastern Standard Time`, on every operating system that has ICU. .NET Framework
knows only the Windows names. Its data is kept current by operating system and
ICU updates, so it may hold rules newer than those of `TzData`, and its results
agree with whatever else your application computes with `TimeZoneInfo`.

But its data depends on the machine. As the XML documentation of
`IcalTimeZoneResolver.System` puts it:

> As the `TimeZoneInfo` documentation warns, not every time zone is present in
> the Windows registry, time zones can be added to or removed from it, and it
> need not hold their history. On Linux and macOS, and on Windows with ICU, which
> time zones are present and how much history they hold also varies from one
> machine to another. `TzData`, by contrast, always holds every time zone of the
> TZ Database release it was built from.

So `System` is not in `Default`, and should be placed last in a chain, after
`TzData`, so that the same calendar gives the same times on every machine
wherever it can:

```csharp
var resolver = IcalTimeZoneResolver.Chain(
    IcalTimeZoneResolver.Calendar,
    IcalTimeZoneResolver.TzData,
    IcalTimeZoneResolver.System);
```

This is the recommended chain for reading calendars from unknown sources.

Two more limits apply to `System`:

- `TimeZoneInfo` gives no list of transitions on every target, so `System` finds
  them by sampling the offset a day apart. Two changes of offset less than a day
  apart that cancel each other out aren't seen.
- A transition has no name, because `TimeZoneInfo` gives no customary
  abbreviation.

`System` refuses a `TZID` that could name a file outside the system's time zone
data, since a `TZID` comes from the calendar, which may not be trusted.

## Windows time-zone names

Outlook and other Microsoft software write Windows names, such as
`TZID=Eastern Standard Time`.

- **With a VTIMEZONE:** `Calendar` resolves it like any other `TZID`. This is the
  compliant case.
- **Without a VTIMEZONE:** `TzData` doesn't map Windows names to TZ Database
  names, so only `System` can resolve it.

## UTC and floating times

Only zoned times need a resolver:

- **UTC:** a value ending in `Z` is already an instant. `ToUtc` returns it as it
  is.
- **Floating:** a value with neither `Z` nor `TZID` means the same wall-clock
  time in any time zone (RFC 5545 §3.3.5). It has no single instant, so `ToUtc`
  throws `ArgumentException`. Occurrence expansion keeps floating times floating.
- **DATE:** an all-day value has no time and no time zone. Expansion keeps it a
  date.

## Converting a time

`IcalTimeZoneResolver` adds extension methods to any resolver:

- `Resolve(timeZoneId, calendar)` returns the rules, or throws
  `IcalFileException`.
- `ToUtc(value, calendar)` converts an `IcalDateTime` to UTC, resolving its time
  zone if it is zoned.

```csharp
var start = IcalDateTime.Parse("20260115T090000", "America/New_York", IcalValueFormat.Text);
var utc = resolver.ToUtc(start, calendar);   // 20260115T140000Z
```

A time in a daylight saving gap or overlap follows the rules of RFC 5545 §3.3.5.
See [DST gaps and overlaps](dst-rules.md).

## Writing calendars

A writer refuses a calendar that uses a `TZID` with no VTIMEZONE, unless
`AllowUndefinedTimeZones` is set. To write a compliant calendar, add a VTIMEZONE
for each time zone you use. `IcalTimeZone.FromTzData` builds one from the TZ
Database:

```csharp
public static IcalTimeZone FromTzData(TzDataTimezone timezone, DateTimeOffset from, DateTimeOffset to)
```

It defines the time zone over the period from `from` (included) to `to`
(excluded). Choose a period that covers the calendar's times:

```csharp
using DataStandardizer.Chronology;

var newYork = TzDataTimezone.Parse("America/New_York");
var timeZone = IcalTimeZone.FromTzData(newYork,
    new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

calendar.Components.Add(timeZone);
```

The result is exact within the period:

```text
BEGIN:VTIMEZONE
TZID:America/New_York
BEGIN:STANDARD
DTSTART:20251231T190000
TZOFFSETFROM:-0500
TZOFFSETTO:-0500
TZNAME:EST
END:STANDARD
BEGIN:DAYLIGHT
DTSTART:20260308T020000
TZOFFSETFROM:-0500
TZOFFSETTO:-0400
TZNAME:EDT
END:DAYLIGHT
BEGIN:STANDARD
DTSTART:20261101T020000
TZOFFSETFROM:-0400
TZOFFSETTO:-0500
TZNAME:EST
END:STANDARD
END:VTIMEZONE
```

- The first observance starts at `from`, with the offset then in use as both its
  `TZOFFSETFROM` and its `TZOFFSETTO`.
- Each later observance gathers the transitions that share a kind, a pair of
  offsets and a name. Its `DTSTART` is the first of them, and `RDATE` properties
  list the rest. `RRULE` isn't used.
- Before `from`, the definition gives the offset in use at `from`. From `to`
  onwards, it gives the offset after the last transition in the period.

Since the definitions come from the TZ Database, the `Calendar` resolver gives
the same offsets for the generated VTIMEZONE as `TzData` does within the period.

## Related

- [DST gaps and overlaps](dst-rules.md)
- [Standards compliance by default](standards-compliance.md)
- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [IcalTimeZoneResolver reference](../reference/IcalTimeZoneResolver.md)
- [IIcalTimeZoneResolver reference](../reference/IIcalTimeZoneResolver.md)
