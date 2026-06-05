---
title: TzDataExtensions Class
parent: Chronology
grand_parent: Packages
nav_exclude: true
---

# TzDataExtensions Class

## Definition

Namespace: `DataStandardizer.Chronology`

Extension methods that read the [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md)
metadata attached to the static fields of [TzDataTimezone](TzDataTimezone.md).

```csharp
public static class TzDataExtensions
```

## Remarks

Each accessor locates the time zone's declaring field, reads its
`TzDataTimezoneAttribute`, and returns the requested metadata (or a default when
unavailable). On `net8.0`/`net10.0` `GetComment` returns `string?`; on the
.NET Standard targets it returns `string` annotated `[CanBeNull]`.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetComment()` | `TzDataTimezone` | `string?` | Timezone comment, or `null` if unavailable. Returns `string` (`[CanBeNull]`) on the .NET Standard targets. |
| `GetIsoCountryCodes()` | `TzDataTimezone` | `string[]` | ISO 3166-1 Alpha-2 codes for the countries the zone covers (empty if none). |
| `GetLatitude()` | `TzDataTimezone` | `double` | Latitude of the zone's principal location (`0` if unavailable). |
| `GetLongitude()` | `TzDataTimezone` | `double` | Longitude of the zone's principal location (`0` if unavailable). |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [Access time zone metadata](../how-to/access-timezone-metadata.md)
- [Use time zones](../how-to/use-timezones.md)
- [TzDataTimezone](TzDataTimezone.md)
- [TzDataTimezoneAttribute](TzDataTimezoneAttribute.md)
- [Chronology API reference](index.md)
