---
title: VcardGender Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardGender Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The structured value of the GENDER property (RFC 6350 §6.2.7): a sex and a free-form gender identity, each optional. Only vCard 4.0 has this property.

**Syntax**

```csharp
public readonly struct VcardGender : IEquatable<VcardGender>
```

## Remarks

`M` and `M;` differ: the identity is `null` in the first and empty in the second. The first semicolon ends the sex, and the identity is a `text` value, so it can hold more semicolons but not a bare comma. The letter may be in either case, since ABNF strings are case-insensitive, and is written in upper case.

**Allocation.** This type holds a copy of the identity. `TryParseParts` gives the sex and the identity's text without allocating.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `Sex` and `Identity`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardGender(VcardSex sex, string identity)` | `identity` may be `null`. |

## Properties

| Property | Notes |
| --- | --- |
| `VcardSex Sex` | See [VcardSex](VcardSex.md). |
| `string Identity` | The identity, or `null` if the value leaves out the component. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse`, `Parse`, `TryFormat`, `ToString` | As for the other values, with no version. |
| `TryParseParts(value, format, out sex, out identity)` | Parses without allocating. The identity keeps its escaping. |

See the [API reference](index.md).
