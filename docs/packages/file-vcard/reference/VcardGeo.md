---
title: VcardGeo Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardGeo Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The value of the GEO property: a URI in vCard 4.0, and a latitude and longitude in vCard 3.0 and 2.1.

**Syntax**

```csharp
public readonly struct VcardGeo : IEquatable<VcardGeo>
```

## Remarks

| Version | Form | Standard |
| --- | --- | --- |
| 4.0 | a URI, with its comma escaped in a content line, such as `geo:37.386013\,-122.082932` | RFC 6350 §6.5.2 and its erratum 3846 |
| 3.0 | `37.386013;-122.082932` | RFC 2426 §3.4.2 |
| 2.1 | `37.24,-17.87` | vCard 2.1 §2.4.6 |

The value is held as a URI. RFC 6350 §6.5.2 says the `geo` scheme of RFC 5870 is particularly well suited but other schemes may be used, so a URI of another scheme is valid and `Latitude` and `Longitude` are `null` for it. They are `null` too for a `geo` URI whose `crs` parameter names a system other than `wgs84`, since RFC 5870 §3.4.2 gives them no meaning there. A value that says it is a `geo` URI has to follow RFC 5870 §3.3.

The digits are kept as they were written when one form becomes another, apart from a plus sign, which a `geo` URI can't have. vCard 3.0 and 2.1 can write only a plain latitude and longitude. A latitude or longitude out of range is rejected in every version.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Allocation.** Parsing allocates, since the URI is held as a string.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from its properties and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardGeo(double latitude, double longitude)` | In WGS 84. Out of range, or NaN, throws `ArgumentOutOfRangeException`. |

## Properties

| Property | Notes |
| --- | --- |
| `string UriText` | The URI, with no escaping. |
| `double? Latitude`, `double? Longitude`, `double? Altitude` | The coordinates in WGS 84, or `null`. |
| `double? Uncertainty` | The `u` parameter of RFC 5870 §3.4.3, or `null`. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse(value, format, out result)` | Parses a vCard 4.0 value. Returns `false` if it isn't valid. |
| `TryParse(value, format, version, out result)` | Parses a value by the grammar of a version. |
| `TryParse(Uri uri, out result)` | Reads a URI, such as one from `VcardCard.GeoLocations`, which has no escaping. Returns `false` if it isn't valid. |
| `Parse(value, format)`, `Parse(value, format, version)` | As `TryParse`, and throws `FormatException` if the value isn't valid. |
| `TryFormat(destination, out charsWritten, format)`, `TryFormat(destination, out charsWritten, format, version)` | Writes the value. Returns `false` if the destination is too short. |
| `ToString(format)`, `ToString(format, version)` | Formats the value as a string. |
| `IsDefinedIn(version)` | Whether a version can write this value. |

See the [API reference](index.md).
