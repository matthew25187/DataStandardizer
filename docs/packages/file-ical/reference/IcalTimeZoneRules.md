---
title: IcalTimeZoneRules Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalTimeZoneRules Class

## Definition

Namespace: `DataStandardizer.File.iCal`

The rules of a time zone, as resolved by an
[IIcalTimeZoneResolver](IIcalTimeZoneResolver.md): the UTC offset in use at each
instant, and how a local wall-clock time maps to UTC.

**Syntax**

```csharp
public abstract class IcalTimeZoneRules
```

## Remarks

You get rules from a resolver, through `TryResolve` or the
[IcalTimeZoneResolver.Resolve](IcalTimeZoneResolver.md) extension method. The
resolvers of this package return internal derived types, described on the
[IcalTimeZoneResolver](IcalTimeZoneResolver.md) page. To supply your own rules,
derive from this class and return it from your own resolver.

**Deriving.** A derived type supplies two things: the offset at an instant
(`GetUtcOffsetCore`) and the transitions over a period (`GetTransitionsCore`). The
`OffsetTo` of each transition must be what `GetUtcOffsetCore` gives from its instant
until the next transition. The mapping of local time to UTC is built on those two.
A derived type may override `GetUtcOffsetsForLocalCore` when it has a faster or more
exact way.

**Gaps and overlaps.** `ToUtc` follows RFC 5545 §3.3.5. A time in a gap, as when
clocks go forward, is read with the offset in use before the gap, which moves it
forward by the length of the gap. A time in an overlap, as when clocks go back,
takes its first occurrence, the earlier instant. For example, in `Europe/London`,
01:30 on the day clocks go forward becomes 01:30 UTC (02:30 BST), and 01:30 on the
day clocks go back becomes 00:30 UTC (01:30 BST).

**Kinds of `DateTime`.** Times in UTC may be of kind `DateTimeKind.Utc` or
`DateTimeKind.Unspecified`. Local wall-clock times must be of kind
`DateTimeKind.Unspecified`. A time of kind `DateTimeKind.Local`, which belongs to the
time zone of the machine, is rejected by both with `ArgumentException`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `protected IcalTimeZoneRules(string timeZoneId)` | Creates the rules of a time zone. `timeZoneId` is the identifier, as given by a `TZID` parameter. Throws `ArgumentNullException` if `timeZoneId` is `null`, or `ArgumentException` if it is empty. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `TimeZoneId` | `string TimeZoneId { get; }` | The time zone identifier the rules were resolved for. For a `TZID` with a vendor prefix, such as `/mozilla.org/20050126_1/America/New_York`, this is the identifier as given, prefix included. |

## Methods

### Public

| Method | Returns | Notes |
| --- | --- | --- |
| `GetTransitions(DateTime fromUtc, DateTime toUtc)` | `IEnumerable<IcalTimeZoneTransition>` | The transitions between UTC offsets from `fromUtc` (included) to `toUtc` (excluded), in chronological order. The arguments are checked when the method is called. Throws `ArgumentException` if either is of kind `DateTimeKind.Local`, or `ArgumentOutOfRangeException` if `toUtc` precedes `fromUtc`. |
| `GetUtcOffset(DateTime utc)` | `IcalUtcOffset` | The UTC offset in use at an instant. Throws `ArgumentException` if `utc` is of kind `DateTimeKind.Local`. |
| `GetUtcOffsetsForLocal(DateTime local)` | `IReadOnlyList<IcalUtcOffset>` | Every UTC offset in use at a local wall-clock time, in chronological order of the instants at which the local clock shows it: none in a gap, one normally, and two in an overlap. Throws `ArgumentException` if `local` is of kind `DateTimeKind.Utc` or `DateTimeKind.Local`. |
| `ToLocal(DateTime utc)` | `DateTime` | Converts an instant to the local wall-clock time of the zone, of kind `DateTimeKind.Unspecified`. Throws `ArgumentException` if `utc` is of kind `DateTimeKind.Local`, or `ArgumentOutOfRangeException` if the local time falls outside the range of `DateTime`. |
| `ToUtc(DateTime local)` | `DateTime` | Converts a local wall-clock time to an instant of kind `DateTimeKind.Utc`, applying the RFC 5545 §3.3.5 gap and overlap rules. Throws `ArgumentException` if `local` is of kind `DateTimeKind.Utc` or `DateTimeKind.Local`, or `ArgumentOutOfRangeException` if the instant falls outside the range of `DateTime`. Throws `InvalidOperationException` if a derived type reports no offset for the time yet no gap around it, which means its offsets and transitions disagree. |

### Protected

| Method | Returns | Notes |
| --- | --- | --- |
| `GetTransitionsCore(DateTime fromUtc, DateTime toUtc)` | `IEnumerable<IcalTimeZoneTransition>` | Abstract. The transitions from `fromUtc` (included) to `toUtc` (excluded), in chronological order. Both are of kind `DateTimeKind.Utc`, and `toUtc` does not precede `fromUtc`. |
| `GetUtcOffsetCore(DateTime utc)` | `IcalUtcOffset` | Abstract. The offset in use at an instant of kind `DateTimeKind.Utc`. |
| `GetUtcOffsetsForLocalCore(DateTime local)` | `IReadOnlyList<IcalUtcOffset>` | Virtual. Every offset in use at a local time of kind `DateTimeKind.Unspecified`, in chronological order. The default takes the offset in use a day before the time and the transitions over the day either side of it, which covers every offset of less than a day. It keeps each offset whose instant, the time less the offset, falls while that offset is in use. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Daylight saving rules](../concepts/dst-rules.md)
- [IIcalTimeZoneResolver](IIcalTimeZoneResolver.md)
- [IcalTimeZoneResolver](IcalTimeZoneResolver.md)
- [IcalTimeZoneTransition](IcalTimeZoneTransition.md)
- [IcalUtcOffset](IcalUtcOffset.md)
- [File.iCal API reference](index.md)
