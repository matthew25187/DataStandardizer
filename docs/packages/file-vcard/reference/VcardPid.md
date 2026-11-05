---
title: VcardPid Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardPid Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A value of the PID parameter (RFC 6350 §5.5): a positive integer, or two separated by a dot such as `1.2`. Only vCard 4.0 has this parameter.

**Syntax**

```csharp
public readonly struct VcardPid : IEquatable<VcardPid>
```

## Remarks

The second integer is the source identifier, which a [VcardClientPidMap](VcardClientPidMap.md) gives a meaning to. Neither can be zero (RFC 6350 §5.5, §6.7.7). An integer too large for a `long` is rejected. Leading zeros are accepted, and not written.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

**Allocation.** `TryParse` from a span and `TryFormat` into one do not allocate (covered by `VcardValueAllocationTests`).

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `LocalId` and `SourceId`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardPid(long localId)`, `VcardPid(long localId, long sourceId)` | An identifier that isn't positive throws `ArgumentOutOfRangeException`. |

## Properties

| Property | Notes |
| --- | --- |
| `long LocalId` | The first integer. |
| `long? SourceId` | The second, or `null`. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse`, `Parse`, `TryFormat`, `ToString` | As for the other values, with no version. |

See the [API reference](index.md).
