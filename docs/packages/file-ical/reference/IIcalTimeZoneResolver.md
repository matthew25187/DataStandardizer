---
title: IIcalTimeZoneResolver Interface
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IIcalTimeZoneResolver Interface

## Definition

Namespace: `DataStandardizer.File.iCal`

Resolves a time zone identifier, as given by a `TZID` parameter, to the
[IcalTimeZoneRules](IcalTimeZoneRules.md) of the time zone.

**Syntax**

```csharp
public interface IIcalTimeZoneResolver
```

## Remarks

[IcalTimeZoneResolver](IcalTimeZoneResolver.md) provides the resolvers of this
package (`Calendar`, `TzData`, `System`), `Chain`, which combines them, and the
`Resolve` and `ToUtc` extension methods. `IcalTimeZoneResolver.Default` resolves only
through the calendar's own VTIMEZONE components, as RFC 5545 requires.

Pass a resolver to `GetOccurrences` on [IcalEvent](IcalEvent.md),
[IcalTodo](IcalTodo.md) or [IcalJournal](IcalJournal.md) to choose how zoned times
are converted.

**Implementing.** Resolution is synchronous. A resolver must be safe to use from
several threads at once. Return `false`, not throw, when the resolver does not know
the identifier, so that the next resolver of a chain is tried. Throw
`IcalFileException` only when the resolver found a definition of the time zone that
is not valid; a chain does not catch it. To supply your own rules, derive from
[IcalTimeZoneRules](IcalTimeZoneRules.md).

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `TryResolve(string timeZoneId, IcalCalendar? calendar, [NotNullWhen(true)] out IcalTimeZoneRules? rules)` | `bool` | Tries to resolve `timeZoneId`. `calendar` is the calendar the identifier appears in, or `null` if there is none. Returns `true` and sets `rules` if this resolver knows the time zone; otherwise returns `false`, so that the next resolver of a chain is tried. Throws `ArgumentNullException` if `timeZoneId` is `null`, or `IcalFileException` if the resolver found a definition of the time zone that is not valid. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations and the `[NotNullWhen(true)]` attribute apply to
the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Time zone resolution](../concepts/time-zone-resolution.md)
- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [IcalTimeZoneResolver](IcalTimeZoneResolver.md)
- [IcalTimeZoneRules](IcalTimeZoneRules.md)
- [File.iCal API reference](index.md)
