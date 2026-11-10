---
title: VcardName Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardName Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The structured value of the N property (RFC 6350 §6.2.2, RFC 9554 §2.2): the components of a name, each a list of text.

**Syntax**

```csharp
public readonly struct VcardName : IEquatable<VcardName>
```

## Remarks

| Version | Components | Items | Standard |
| --- | --- | --- | --- |
| 4.0 | exactly 5, or exactly 7 with RFC 9554's secondary surname and generation | a comma separates them | RFC 6350 §6.2.2, RFC 9554 §2.2 |
| 3.0 | 1 to 5 | a comma separates them | RFC 2426 §4 |
| 2.1 | 1 to 5 | one; a comma is ordinary text | vCard 2.1 §2.9 |

This type writes five components unless one of the two RFC 9554 components has a value, so seven with both empty comes back as five. An empty component has no items, and the empty items of a list such as `a,,b` are kept. RFC 9554's rules on what else a writer puts in the older components are `WithBackwardCompatibleComponents`, which [VcardCard](VcardCard.md) applies, and those on reading them are `FamilyNamesExcludingSecondarySurnames` and `HonorificSuffixesExcludingGenerations`.

**Versions.** Every parse and format method has an overload taking a [VcardVersion](VcardVersion.md). The overload without one uses vCard 4.0. `IsDefinedIn` says whether a version can write the value, and formatting one it can't throws `InvalidOperationException`.

**Forms.** Only `VcardValueFormat.Text` has a form of its own. In jCard (RFC 7095 §3.3.1.3) the value is a JSON array with an element for each component, and in xCard (RFC 6351 §5) a tree of XML elements, one for each component, so `Json` and `Xml` throw `NotSupportedException`. `JcardReader`, `JcardWriter`, `XcardReader` and `XcardWriter` map them to and from the text form.

**Allocation.** This type holds copies of its text, so parsing into it allocates. `TryParseView` checks the value and gives a [VcardStructuredValueView](VcardStructuredValueView.md) without allocating (covered by `VcardValueAllocationTests`).

**Uninitialised instance.** A default instance throws `InvalidOperationException` from its component properties and `IsDefinedIn`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardName(familyNames, givenNames, additionalNames, honorificPrefixes, honorificSuffixes)`, and with `secondarySurnames, generations` | Each is an `IEnumerable<string>`; give an empty list for a component with no items. `null` throws `ArgumentNullException`, and a control character `ArgumentException`. |

## Properties

| Property | Notes |
| --- | --- |
| `FamilyNames`, `GivenNames`, `AdditionalNames`, `HonorificPrefixes`, `HonorificSuffixes`, `SecondarySurnames`, `Generations` | Read-only lists. |
| `FamilyNamesExcludingSecondarySurnames`, `HonorificSuffixesExcludingGenerations` | The older component without any value that is also in the new one, which RFC 9554 §2.2 says a reader should ignore. |

## Fields

| Field | Notes |
| --- | --- |
| `FamilyNamesComponent` to `GenerationComponent` | The positions, for use with a view. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse(value, format, out result)` | Parses a vCard 4.0 value. Returns `false` if it isn't valid. |
| `TryParse(value, format, version, out result)` | Parses a value by the grammar of a version. |
| `Parse(value, format)`, `Parse(value, format, version)` | As `TryParse`, and throws `FormatException` if the value isn't valid. |
| `TryFormat(destination, out charsWritten, format)`, `TryFormat(destination, out charsWritten, format, version)` | Writes the value. Returns `false` if the destination is too short. |
| `ToString(format)`, `ToString(format, version)` | Formats the value as a string. |
| `IsDefinedIn(version)` | Whether a version can write this value. |
| `WithBackwardCompatibleComponents()` | This name with its secondary surnames also in the family names and its generations also in the honorific suffixes, as RFC 9554 §2.2 says to write them. |
| `TryParseView(value, format, out result)`, `TryParseView(value, format, version, out result)` | Parses a value into a view of its components, without allocating. |

See the [API reference](index.md).
