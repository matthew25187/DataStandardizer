---
title: VcardOrganization Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardOrganization Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The structured value of the ORG property (RFC 6350 §6.6.4): an organization name followed by zero or more levels of organizational unit.

**Syntax**

```csharp
public readonly struct VcardOrganization : IEquatable<VcardOrganization>
```

## Remarks

A component holds one piece of text, not a list, so in vCard 4.0 and 3.0 a comma in one has to be escaped. In vCard 2.1 a comma is ordinary text.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Allocation.** This type holds copies of its text, so parsing into it allocates. `TryParseView` checks the value and gives a [VcardStructuredValueView](VcardStructuredValueView.md) without allocating (covered by `VcardValueAllocationTests`).

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `Name`, `Units` and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardOrganization(string name, IEnumerable<string> units)` | `null` throws `ArgumentNullException`, and a control character `ArgumentException`. |

## Properties

| Property | Notes |
| --- | --- |
| `string Name` | The organization name. |
| `IReadOnlyList<string> Units` | The units, the highest level first. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse(value, format, out result)` | Parses a vCard 4.0 value. Returns `false` if it isn't valid. |
| `TryParse(value, format, version, out result)` | Parses a value by the grammar of a version. |
| `Parse(value, format)`, `Parse(value, format, version)` | As `TryParse`, and throws `FormatException` if the value isn't valid. |
| `TryFormat(destination, out charsWritten, format)`, `TryFormat(destination, out charsWritten, format, version)` | Writes the value. Returns `false` if the destination is too short. |
| `ToString(format)`, `ToString(format, version)` | Formats the value as a string. |
| `IsDefinedIn(version)` | Whether a version can write this value. |
| `TryParseView(value, format, out result)`, `TryParseView(value, format, version, out result)` | Parses a value into a view of its components, without allocating. |

See the [API reference](index.md).
