---
title: VcardDiagnostic Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardDiagnostic Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A conformance problem found by `VcardCard.Validate`, or by a reader or writer.

**Syntax**

```csharp
public sealed class VcardDiagnostic
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Kind` | `VcardDiagnosticKind` | The kind of problem. |
| `Message` | `string` | A description of the problem. |
| `PropertyName` | `string?` | The property at fault, if it is one property. |
| `Group` | `string?` | The group of that property, if it has one. |
| `LineNumber` | `long` | The line of the property at fault, or the `BEGIN` line of the card. 0 if the card wasn't read from a source. |
| `CardIndex` | `int` | The 0-based index of the card in its source, or -1. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardDiagnosticKind](VcardDiagnosticKind.md)
- [VcardCard](VcardCard.md)
