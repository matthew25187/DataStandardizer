---
title: TzDataExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods on [TzDataTimezone](TzDataTimezone.md). The metadata accessors
read the deprecated [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md)
attached to its static fields.

```csharp
public static class TzDataExtensions
```

## Remarks

`GetComment`, `GetIsoCountryCodes`, `GetLatitude` and `GetLongitude` are
deprecated: each is marked `[Obsolete]` and will be removed in version 2.0. Use
the `Comment`, `IsoCountryCodes`, `Latitude` and `Longitude` properties of
[TzDataTimezone](TzDataTimezone.md) instead. See
[Access time zone metadata](../how-to/access-timezone-metadata.md#migrating-from-the-deprecated-apis).

Each deprecated accessor locates the time zone's declaring field, reads its
`TzDataTimezoneAttribute`, and returns the requested metadata, or a default
(`null`, `0` or an empty array) for an unknown time zone. On `net8.0`/`net10.0`
`GetComment` returns `string?`; on the .NET Standard targets it returns `string`
annotated `[CanBeNull]`.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetComment()` | `TzDataTimezone` | `string?` | **Deprecated** — use `TzDataTimezone.Comment`. Timezone comment, or `null` if unavailable. Returns `string` (`[CanBeNull]`) on the .NET Standard targets. |
| `GetIsoCountryCodes()` | `TzDataTimezone` | `string[]` | **Deprecated** — use `TzDataTimezone.IsoCountryCodes`. ISO 3166-1 Alpha-2 codes for the countries the zone covers (empty if none). |
| `GetLatitude()` | `TzDataTimezone` | `double` | **Deprecated** — use `TzDataTimezone.Latitude`. Latitude of the zone's principal location (`0` if unavailable). |
| `GetLongitude()` | `TzDataTimezone` | `double` | **Deprecated** — use `TzDataTimezone.Longitude`. Longitude of the zone's principal location (`0` if unavailable). |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [Use time zones](../how-to/use-timezones.md)
- [TzDataTimezone](TzDataTimezone.md)
- [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md)
- [Chronology API reference](index.md)
