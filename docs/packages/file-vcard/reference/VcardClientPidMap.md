---
title: VcardClientPidMap Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardClientPidMap Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The structured value of the CLIENTPIDMAP property (RFC 6350 §6.7.7): a PID source identifier and the URI that gives it a global meaning. Only vCard 4.0 has this property.

**Syntax**

```csharp
public readonly struct VcardClientPidMap : IEquatable<VcardClientPidMap>
```

## Remarks

The identifier must be strictly positive (§6.7.7).

**Allocation.** This type holds the URI. `TryParseParts` gives the identifier and the URI's text without allocating, checking its characters and scheme (RFC 3986) but leaving `System.Uri` to go further.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `SourceId` and `Uri`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardClientPidMap(long sourceId, Uri uri)` | The URI must be absolute. |

## Properties

| Property | Notes |
| --- | --- |
| `long SourceId`, `Uri Uri` | The parts. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse`, `Parse`, `TryFormat`, `ToString` | As for the other values, with no version. |
| `TryParseParts(value, format, out sourceId, out uri)` | Parses without allocating. |

See the [API reference](index.md).
