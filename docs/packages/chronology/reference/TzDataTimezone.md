---
title: TzDataTimezone Struct
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataTimezone Struct

## Definition

Namespace: `DataStandardizer.Chronology`

A TZ Database time zone. You don't construct these; instead use the predefined
static instances, grouped by region and named after the database's hierarchical
identifiers — for example `TzDataTimezone.Europe.Berlin` and
`TzDataTimezone.America.Argentina.Buenos_Aires`.

```csharp
public readonly struct TzDataTimezone : IComparable, IEquatable<TzDataTimezone>, IParsable<TzDataTimezone>
```

`IParsable<TzDataTimezone>` is implemented on `net8.0` and later only.

## Remarks

To get a time zone from its identifier string, use `Parse` or `TryParse`. They
return the predefined instance with that identifier, and reject identifiers
that are not those of a predefined instance. Identifiers are case-sensitive.
Prefer them to the explicit cast, which accepts any string. They also accept the
deprecated names in the TZ Database's `backward` file, and return the canonical
time zone each refers to: `Parse("Asia/Calcutta")` returns
`TzDataTimezone.Asia.Kolkata`.

Per-zone location metadata (latitude, longitude, ISO country codes, comment) is
read through the `Latitude`, `Longitude`, `IsoCountryCodes` and `Comment`
properties. These replace the `GetLatitude`, `GetLongitude`,
`GetIsoCountryCodes` and `GetComment` extension methods on
[TzDataExtensions](TzDataExtensions.md), which are deprecated. See
[Access time zone metadata](../how-to/access-timezone-metadata.md).

Each time zone carries its full history from the TZ Database, read through the
`ZoneLines` property as a list of [TzDataZoneLine](TzDataZoneLine.md). The
extension methods on [TzDataExtensions](TzDataExtensions.md) use it to calculate
the offset from universal time, daylight saving time, transitions and
abbreviations at any instant. See
[Calculate UTC offsets](../how-to/calculate-utc-offsets.md).

### Cast and default instances

An instance created by explicit cast from a string has no data of its own. It
finds its zone lines and metadata through the predefined instance with the same
identifier, so it returns the same values as that instance:

```csharp
var london = (TzDataTimezone)"Europe/London";
bool same = london.ZoneLines[0] == TzDataTimezone.Europe.London.ZoneLines[0];   // true
```

The cast doesn't validate the identifier. For `default(TzDataTimezone)`, and for
an instance cast from an identifier that is not that of a predefined instance,
the properties (including `ZoneLines`, `IsLink` and `Canonical`) and the
calculation methods of [TzDataExtensions](TzDataExtensions.md) throw
`InvalidOperationException`. Equality uses only the identifier, so an instance
cast from an unknown identifier is equal to another cast from the same string.
The deprecated metadata accessors on `TzDataExtensions` return `null`, `0` or an
empty array instead of throwing.

### Links and canonical time zones

The canonical time zones are those listed in the TZ Database's `zone1970.tab`,
each with its own history. Some time zones listed in `zone.tab` are instead
*links* to a canonical time zone, because their clocks have agreed with it since
1970: `Europe/Oslo` and `Europe/Stockholm` are links to `Europe/Berlin`, for
example. Each such link has its own field, such as `TzDataTimezone.Europe.Oslo`,
with its own location metadata from `zone.tab`, but shares the zone lines of
its canonical time zone. Where a link refers to another link, it is resolved to
the canonical time zone at the end of the chain.

A link's history before 1970 is that of its canonical time zone, not the local
history of the place it names: `TzDataTimezone.Europe.Oslo.ZoneLines` shows
Berlin's history, including the offsets Berlin observed before 1970.

`IsLink` tells you whether a time zone is a link, and `Canonical` returns the
canonical time zone it links to, or the time zone itself if it is canonical.
Equality is based on the identifier, so `Europe.Oslo != Europe.Berlin`, and a
link's identifier is kept unchanged. To ask whether two time zones share their
history, compare their `Canonical` time zones:

```csharp
var oslo = TzDataTimezone.Europe.Oslo;
bool same = oslo == TzDataTimezone.Europe.Berlin;                     // false
bool shared = oslo.Canonical == TzDataTimezone.Europe.Berlin.Canonical; // true
```

The deprecated names in the TZ Database's `backward` file, such as
`Asia/Calcutta` and `US/Eastern`, have no fields. `Parse` and `TryParse` return
the canonical time zone for them.

### Generated source

`TzDataTimezone` is a `partial` struct. Most of it is generated from the TZ
Database source (tzdata) by `scripts/GenerateTzDataTimezoneOfficial.ps1`, which
produces four source files:

| File | Contents |
| --- | --- |
| `TzDataTimezone.cs` | The struct, its identifier members, and a field for each time zone, nested by region. |
| `TzDataTimezone.ZoneLineData.cs` | The zone lines of each canonical time zone, which the fields of the time zone and of its links share. |
| `TzDataTimezone.RuleSets.cs` | The daylight saving rule sets referenced by the zone lines. |
| `TzDataTimezone.LinkData.cs` | The deprecated names from the `backward` file, which `Parse` and `TryParse` accept. |

The zone line data, rule sets and link data are private. They are reached
through `ZoneLines` and the methods of [TzDataExtensions](TzDataExtensions.md).
The properties and parsing methods are hand-written, in
`TzDataTimezone.Members.cs`.

The generated files need no manual edits. The script's comment-based help
(`Get-Help ./scripts/GenerateTzDataTimezoneOfficial.ps1 -Full`) lists the tzdata
files it requires: `zone1970.tab`, `zone.tab`, `iso3166.tab`, the main-format
region files (`africa`, `antarctica`, `asia`, `australasia`, `europe`,
`northamerica` and `southamerica`), `etcetera` and `backward`. The TZ Database
release a build is based on is given in the remarks on the type. See
[Data currency and versioning](../../../concepts/data-currency-and-versioning.md).

## Fields

The time zone instances are grouped by region:

- [Africa](TzDataTimezone.Africa.md) — 19 time zones and 33 links
- [America](TzDataTimezone.America.md) — 121 time zones and 23 links
- [Antarctica](TzDataTimezone.Antarctica.md) — 8 time zones and 3 links
- [Arctic](TzDataTimezone.Arctic.md) — 1 link
- [Asia](TzDataTimezone.Asia.md) — 74 time zones and 8 links
- [Atlantic](TzDataTimezone.Atlantic.md) — 8 time zones and 2 links
- [Australia](TzDataTimezone.Australia.md) — 11 time zones
- [Europe](TzDataTimezone.Europe.md) — 38 time zones and 20 links
- [Indian](TzDataTimezone.Indian.md) — 3 time zones and 8 links
- [Pacific](TzDataTimezone.Pacific.md) — 30 time zones and 8 links

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Canonical` | `TzDataTimezone Canonical { get; }` | The canonical time zone that a link refers to, or this time zone if it is canonical. |
| `Comment` | `string? Comment { get; }` | The zone's comment from `zone1970.tab` (`zone.tab` for a link), or `null` if it has none. Declared as `string` (`[CanBeNull]`) on the .NET Standard targets. |
| `IsLink` | `bool IsLink { get; }` | `true` if the time zone is a link to a canonical time zone; otherwise, `false`. |
| `IsoCountryCodes` | `IReadOnlyList<string> IsoCountryCodes { get; }` | ISO 3166-1 Alpha-2 codes for the countries the zone covers, in `zone1970.tab` order (`zone.tab` for a link). |
| `Latitude` | `double Latitude { get; }` | Latitude of the zone's principal location, in decimal degrees. |
| `Longitude` | `double Longitude { get; }` | Longitude of the zone's principal location, in decimal degrees. |
| `ZoneLines` | `IReadOnlyList<TzDataZoneLine> ZoneLines { get; }` | The zone's full history of offsets from universal time, as [TzDataZoneLine](TzDataZoneLine.md) instances in chronological order. Every line but the last has an `Until`. For a link, those of its canonical time zone. |

## Methods

### Static

| Method | Returns | Notes |
| --- | --- | --- |
| `Parse(string s)` | `TzDataTimezone` | Returns the predefined instance with identifier `s`, or the canonical instance for a deprecated name. Throws `ArgumentNullException` if `s` is `null`, and `FormatException` if it is not a known identifier. |
| `Parse(string s, IFormatProvider? provider)` | `TzDataTimezone` | `IParsable<TzDataTimezone>` implementation; `net8.0` and later only. `provider` is ignored. |
| `TryParse(string? s, out TzDataTimezone result)` | `bool` | Returns `false`, with `result` set to `default`, if `s` is `null` or not a known identifier. |
| `TryParse(string? s, IFormatProvider? provider, out TzDataTimezone result)` | `bool` | `IParsable<TzDataTimezone>` implementation; `net8.0` and later only. `provider` is ignored. |

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CompareTo(object obj)` | `int` | |
| `Equals(TzDataTimezone other)` | `bool` | |
| `Equals(object obj)` | `bool` | Override. |
| `GetHashCode()` | `int` | Override. |
| `ToString()` | `string` | Override. Returns the identifier. |
| `ToString(IFormatProvider provider)` | `string` | |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator TzDataTimezone(string)` | Wraps an identifier string without validating it. Prefer `Parse` or `TryParse`. |
| Implicit | `implicit operator string(TzDataTimezone)` | Unwraps to the identifier string. |
| Equality | `operator ==`, `!=` `(TzDataTimezone, TzDataTimezone)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Use time zones](../how-to/use-timezones.md)
- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [Calculate UTC offsets](../how-to/calculate-utc-offsets.md)
- [TzDataExtensions](TzDataExtensions.md)
- [TzDataZoneLine](TzDataZoneLine.md)
- [Chronology API reference](index.md)
