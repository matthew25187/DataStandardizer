---
title: VcardValueType Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardValueType Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The value types of RFC 6350 §4, named by the `VALUE` parameter.

**Syntax**

```csharp
public enum VcardValueType
```

## Remarks

The members are `Unknown`, `Text`, `Uri`, `Date`, `Time`, `DateTime`, `DateAndOrTime`, `Timestamp`, `Boolean`, `Integer`, `Float`, `UtcOffset` and `LanguageTag`. `Unknown` is the value of an unknown or experimental (`X-`) property, kept as raw text so that it round-trips without loss.

See the [API reference](index.md).
