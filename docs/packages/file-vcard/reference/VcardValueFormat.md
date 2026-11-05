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

`Text` is the form used in `.vcf` content lines. `Xml` is the xCard form of RFC 6351, for which the value types throw `NotSupportedException` until xCard is implemented.

`Json` is the jCard form of RFC 7095: the text of a JSON string with its JSON escaping removed, or of a JSON number or literal. TEXT has no escaping of vCard, and the items of a list are separate strings. BOOLEAN is `true` or `false`, and INTEGER and FLOAT are JSON numbers; an INTEGER may have a fraction, which is dropped. Dates, times and UTC offsets are in the extended format of ISO 8601 where it applies, such as `1985-04-12` and `-05:00`. A URI has no escaping of its commas. jCard represents vCard 4.0 alone, so a method that takes a `VcardVersion` throws `ArgumentException` for `Json` with any other. `VcardName`, `VcardAddress`, `VcardOrganization`, `VcardGender` and `VcardClientPidMap` throw `NotSupportedException` for `Json`, since in jCard their values are JSON arrays, which [JcardReader](JcardReader.md) and [JcardWriter](JcardWriter.md) handle. See [Read and write jCard](../how-to/read-write-jcard.md).

See the [API reference](index.md).
