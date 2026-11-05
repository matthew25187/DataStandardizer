---
title: VcardAddress Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardAddress Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The structured value of the ADR property (RFC 6350 §6.3.1, RFC 9554 §2.1): the components of a delivery address, each a list of text.

**Syntax**

```csharp
public readonly struct VcardAddress : IEquatable<VcardAddress>
```

## Remarks

| Version | Components | Items | Standard |
| --- | --- | --- | --- |
| 4.0 | exactly 7, or exactly 18 with RFC 9554's eleven | a comma separates them | RFC 6350 §6.3.1, RFC 9554 §2.1 |
| 3.0 | 1 to 7 | one; a comma has to be escaped | RFC 2426 §4 |
| 2.1 | 1 to 7 | one; a comma is ordinary text | vCard 2.1 §2.9 |

This type writes seven components unless one of the eleven RFC 9554 components has a value. RFC 9554's rule on combining them into the street component is `WithBackwardCompatibleComponents`, which [VcardCard](VcardCard.md) applies, and its rule on reading is `HasDetailedComponents`.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` has a form of its own. In jCard (RFC 7095 §3.3.1.3) the value is a JSON array with an element for each component, and in xCard (RFC 6351 §5) a tree of XML elements, one for each component, so `Json` and `Xml` throw `NotSupportedException`. `JcardReader`, `JcardWriter`, `XcardReader` and `XcardWriter` map them to and from the text form.

**Allocation.** This type holds copies of its text, so parsing into it allocates. `TryParseView` checks the value and gives a [VcardStructuredValueView](VcardStructuredValueView.md) without allocating (covered by `VcardValueAllocationTests`).

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `GetComponent`, its component properties and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardAddress(postOfficeBox, extendedAddress, streetAddress, locality, region, postalCode, countryName)` | The components of RFC 6350. |
| `VcardAddress(IReadOnlyList<IEnumerable<string>> components)` | All eighteen, in the order of RFC 9554 §2.1. |

## Properties

| Property | Notes |
| --- | --- |
| `PostOfficeBox`, `ExtendedAddress`, `StreetAddress`, `Locality`, `Region`, `PostalCode`, `CountryName`, `Room`, `Apartment`, `Floor`, `StreetNumber`, `StreetName`, `Building`, `Block`, `Subdistrict`, `District`, `Landmark`, `Direction` | Read-only lists. |

`HasDetailedComponents` is `true` if one of the components of RFC 9554 has a value, in which case RFC 9554 §2.1 says a reader should ignore `StreetAddress`.

## Fields

| Field | Notes |
| --- | --- |
| `PostOfficeBoxComponent` to `DirectionComponent` | The positions, for use with `GetComponent` or a view. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse(value, format, out result)` | Parses a vCard 4.0 value. Returns `false` if it isn't valid. |
| `TryParse(value, format, version, out result)` | Parses a value by the grammar of a version. |
| `Parse(value, format)`, `Parse(value, format, version)` | As `TryParse`, and throws `FormatException` if the value isn't valid. |
| `TryFormat(destination, out charsWritten, format)`, `TryFormat(destination, out charsWritten, format, version)` | Writes the value. Returns `false` if the destination is too short. |
| `ToString(format)`, `ToString(format, version)` | Formats the value as a string. |
| `IsDefinedIn(version)` | Whether a version can write this value. |
| `WithBackwardCompatibleComponents()` | This address with its street address made from the components of RFC 9554, if it has none, as RFC 9554 §2.1 says to write it. The parts are joined with a space: the street number, street name, direction, building, block, floor, apartment, room, landmark, subdistrict and district. |
| `TryParseView(value, format, out result)`, `TryParseView(value, format, version, out result)` | Parses a value into a view of its components, without allocating. |
| `GetComponent(index)` | The items of the component at a position. |

See the [API reference](index.md).
