---
title: VcardValueFormat Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardValueFormat Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The lexical form in which a value is parsed or formatted.

**Syntax**

```csharp
public enum VcardValueFormat
```

## Remarks

`Text` is the form used in `.vcf` content lines. `Json` is the jCard form of RFC 7095 and `Xml` the xCard form of RFC 6351. The value types throw `NotSupportedException` for those two until jCard and xCard are implemented.

See the [API reference](index.md).
