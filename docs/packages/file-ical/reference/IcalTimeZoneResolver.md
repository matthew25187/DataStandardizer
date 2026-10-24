---
title: IcalTimeZoneResolver Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTimeZoneResolver Class

## Definition

Namespace: `DataStandardizer.File.iCal`

The time zone resolvers of this package, a way to chain them, and conversions that
use them. Each resolver implements [IIcalTimeZoneResolver](IIcalTimeZoneResolver.md)
and turns a `TZID` into [IcalTimeZoneRules](IcalTimeZoneRules.md).

**Syntax**

```csharp
public static class IcalTimeZoneResolver
```

## Remarks

RFC 5545 §3.2.19 requires a VTIMEZONE in the calendar for every `TZID` used, and
§3.6.5 says that VTIMEZONE defines the time zone. `Default` is therefore `Calendar`
alone, so that nothing can contradict a definition the calendar holds.

| Resolver chain | Use it when |
| --- | --- |
| `Default` (`Calendar` alone) | The calendar follows RFC 5545. |
| `Chain(Calendar, TzData)` | The calendar uses a `TZID` with no VTIMEZONE. `TzData` fills in only the time zones the calendar leaves undefined. |
| `Chain(TzData, Calendar)` | You distrust the calendar's definitions, for example because they are truncated. This departs from RFC 5545 by preferring the TZ Database. |
| `Chain(Calendar, TzData, System)` | The calendar also uses Windows names, such as `Eastern Standard Time`, with no VTIMEZONE. |

A calendar that uses a `TZID` with no VTIMEZONE is rejected by
[IcalReader](IcalReader.md) unless
[IcalOptions.AllowUndefinedTimeZones](IcalOptions.md) is set.

**Chains.** A chain tries its resolvers in order. The first that returns `true`
wins, and the rest are not called. A resolver that returns `false` (it does not know
the `TZID`) passes the `TZID` to the next one. If every resolver returns `false`, the
chain returns `false`. A resolver that throws, such as `Calendar` finding a VTIMEZONE
that is not valid, stops the chain: the exception is not caught, and later
resolvers are not tried.

**When nothing resolves.** `Resolve` and `ToUtc` throw `IcalFileException` with the
message "The time zone '...' could not be resolved. RFC 5545 requires a VTIMEZONE
for it in the calendar." To treat such a time as floating instead, call
[TryResolve](IIcalTimeZoneResolver.md) and handle the `false` result.

**Gaps and overlaps.** `ToUtc` converts a zoned time as RFC 5545 §3.3.5 requires. A
time in a daylight saving gap is read with the offset in use before the gap, which
moves it forward by the length of the gap. A time in an overlap takes its first
occurrence, the earlier instant. For example, with `America/New_York`, 02:30 on
8 March 2026 (in the gap) becomes 07:30 UTC, and 01:30 on 1 November 2026 (in the
overlap) becomes 05:30 UTC. A leap second (second 60) is converted as the second
before it, and then moved on by a second, so `20161231T185960` in New York becomes
`20170101T000000Z`.

**Thread safety.** The resolvers here, chains included, hold no mutable state, and may be used
from several threads at once, as `IIcalTimeZoneResolver` requires.

### The Calendar resolver

`Calendar` reads the calendar's own VTIMEZONE for the `TZID`.

- It looks through [IcalCalendar.TimeZones](IcalCalendar.md), the nested components
  of type [IcalTimeZone](IcalTimeZone.md), and uses the first whose `TZID` matches,
  compared ordinally (so case matters). Nothing resolves when no calendar is given.
- Any `TZID` the calendar defines resolves, including the Windows names, such as
  `Eastern Standard Time`, that Outlook writes.
- The UTC offsets come from the onsets of each observance, given by its `DTSTART`,
  `RRULE` and `RDATE`, and from its `TZOFFSETFROM` and `TZOFFSETTO`. `DTSTART` and
  `RDATE` are read as local times in the `TZOFFSETFROM` offset, using only their
  wall-clock fields. An `RDATE` that is a DATE is read as midnight, and a PERIOD as
  its start. An `RRULE` with `UNTIL` in UTC is compared with each onset converted with
  `TZOFFSETFROM`.
- Before the earliest onset of all the observances, the `TZOFFSETFROM` of that onset
  is in use.
- The VTIMEZONE is read when it is resolved, so changes made to it afterwards do not
  affect rules already returned.
- The rules expand the onsets of each observance one calendar year at a time, when
  first needed, and keep them. Each `TryResolve` builds new rules, so for many
  conversions in the same time zone, resolve once and reuse the rules, as
  `GetOccurrences` does within one expansion. The rules may be used from several
  threads at once.
- `TryResolve` throws `IcalFileException` if the VTIMEZONE has no STANDARD or
  DAYLIGHT observance, if an observance lacks `DTSTART`, `TZOFFSETFROM` or
  `TZOFFSETTO`, or if one of their values, or an `RRULE` or `RDATE`, is not valid.

### The TzData resolver

`TzData` takes time zones from the TZ Database, through
[TzDataTimezone](../../chronology/reference/TzDataTimezone.md) in
DataStandardizer.Chronology.

- A `TZID` resolves when `TzDataTimezone.TryParse` recognises it, such as
  `America/New_York` or the alias `US/Eastern`. Windows names such as
  `Eastern Standard Time` do not resolve.
- A `TZID` that starts with a solidus is globally unique (RFC 5545 §3.8.3.1). Vendors
  give such a `TZID` by prefixing a path, as in
  `/mozilla.org/20050126_1/America/New_York`. If the whole `TZID` is not recognised,
  each trailing part of the path is tried in turn, longest first.
- An empty `TZID` does not resolve.
- It ignores the calendar, so it is not in `Default`. Chain it after `Calendar` to
  fill in the time zones that a non-compliant calendar leaves undefined.
- The data is fixed when DataStandardizer.Chronology is built, and always holds every
  time zone of that TZ Database release.

### The System resolver

`System` takes time zones from the operating system, through
`TimeZoneInfo.FindSystemTimeZoneById`. It is available on .NET Standard 2.0 and
later only.

- A `TZID` resolves when the system knows it. From .NET 6 on, that includes both TZ
  Database names, such as `America/New_York`, and Windows names, such as
  `Eastern Standard Time`, on every operating system that has ICU. So this resolver
  can read a `TZID` that Outlook wrote without its VTIMEZONE, which `TzData` cannot.
  .NET Framework knows only the Windows names.
- Vendor prefixes are stripped as by `TzData`.
- A `TZID` the system does not know, or that could name a file outside its time zone
  data, does not resolve, so that the next resolver of a chain is tried. A `TZID` is
  refused before it reaches `TimeZoneInfo` if it is empty or longer than 255
  characters, starts or ends with `/`, contains `..`, or contains a control
  character, `\` or `:`.
- The system's data is kept current by updates to the operating system, or to ICU,
  so it may hold rules newer than those of `TzData`. The results also agree with
  whatever else the application computes with `TimeZoneInfo`.

> **Caution.** The data depends on the machine. As the `TimeZoneInfo` documentation
> warns, not every time zone is present in the Windows registry, time zones can be
> added to or removed from it, and it need not hold their history. On Linux and
> macOS, and on Windows with ICU, which time zones are present and how much history
> they hold also varies from one machine to another. `TzData`, by contrast, always
> holds every time zone of the TZ Database release it was built from.
>
> `System` is therefore not in `Default`. Place it last in a chain, after `TzData`,
> as in `Chain(Calendar, TzData, System)`, so that the same calendar gives the same
> times on every machine wherever it can. Never rank it above `TzData`.

- The transitions of a time zone are found by sampling its offset a day apart and
  narrowing each change down to the tick. Two changes of offset less than a day apart
  that cancel each other out are not seen. Finding the transitions over a long period
  takes time in proportion to its length.
- A transition has no [Name](IcalTimeZoneTransition.md), as `TimeZoneInfo` gives no
  customary abbreviation. Its kind comes from `TimeZoneInfo.IsDaylightSavingTime`.
- Local times are converted with `TimeZoneInfo.IsInvalidTime`,
  `IsAmbiguousTime` and `GetAmbiguousTimeOffsets`, with the same RFC 5545 §3.3.5 gap
  and overlap rules as the other resolvers.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Calendar` | `static IIcalTimeZoneResolver Calendar { get; }` | Resolves through the calendar's own VTIMEZONE. See The Calendar resolver. |
| `Default` | `static IIcalTimeZoneResolver Default { get; }` | The default resolver, which is `Calendar` alone, as RFC 5545 requires. Used by the `GetOccurrences` overloads that take no resolver. |
| `System` | `static IIcalTimeZoneResolver System { get; }` | *(netstandard2.0+/.NET)* Resolves through `TimeZoneInfo`. Machine-dependent; place it last in a chain. See The System resolver. |
| `TzData` | `static IIcalTimeZoneResolver TzData { get; }` | Resolves through the TZ Database. See The TzData resolver. |

## Methods

### Static

| Method | Returns | Notes |
| --- | --- | --- |
| `Chain(params IIcalTimeZoneResolver[] resolvers)` | `IIcalTimeZoneResolver` | Combines resolvers into one that tries each in turn, in the order given, so that the first to resolve a time zone wins. The array is copied, so later changes to it do not affect the chain. Throws `ArgumentNullException` if `resolvers` is `null`, or `ArgumentException` if it is empty or contains `null`. |

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `Resolve(string timeZoneId, IcalCalendar? calendar)` | `IIcalTimeZoneResolver` | `IcalTimeZoneRules` | Resolves a time zone identifier, throwing if the resolver does not know it. `calendar` is the calendar the identifier appears in, or `null`. Throws `ArgumentNullException` if the resolver or `timeZoneId` is `null`, or `IcalFileException` if the resolver does not know the time zone or found a definition of it that is not valid. |
| `ToUtc(IcalDateTime value, IcalCalendar? calendar)` | `IIcalTimeZoneResolver` | `IcalDateTime` | Converts a date-time to UTC. A value already in UTC is returned as it is. A zoned value has its `TZID` resolved with `Resolve` and is converted with the RFC 5545 §3.3.5 gap and overlap rules. Throws `ArgumentNullException` if the resolver is `null`; `ArgumentException` if `value` is floating, so it has no single instant; `IcalFileException` if the resolver does not know the time zone or found a definition of it that is not valid; or `ArgumentOutOfRangeException` if the result falls outside the range of `IcalDateTime`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `System` property is available on `netstandard2.0`, `netstandard2.1`, and the
.NET targets only; on `netstandard1.1` use `Calendar` and `TzData`. The nullable
reference annotations apply to the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [IIcalTimeZoneResolver](IIcalTimeZoneResolver.md)
- [IcalTimeZoneRules](IcalTimeZoneRules.md)
- [IcalTimeZone](IcalTimeZone.md)
- [IcalOptions](IcalOptions.md)
- [TzDataTimezone](../../chronology/reference/TzDataTimezone.md)
- [File.iCal API reference](index.md)
