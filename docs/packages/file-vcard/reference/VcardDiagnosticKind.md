---
title: VcardDiagnosticKind Enum
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardDiagnosticKind Enum

## Definition

Namespace: `DataStandardizer.File.vCard`

The kinds of conformance problem.

**Syntax**

```csharp
public enum VcardDiagnosticKind
```

## Fields

| Field | Value | Accepted with | Notes |
| --- | --- | --- | --- |
| `MissingRequiredProperty` | 1 | `AllowMissingRequiredProperties` | `FN`, or the `VERSION` of a card being read. |
| `CardinalityViolation` | 2 | `AllowCardinalityViolations` | More instances than RFC 6350 §6 allows, or a `MEMBER` without `KIND:group`. |
| `InvalidValue` | 3 | `AllowInvalidValues` | A value that doesn't follow its syntax, or a `VALUE` the property doesn't allow. |
| `InvalidParameter` | 4 | `AllowInvalidValues` | A parameter on a property that doesn't allow it, repeated, or not valid; or a `PID` source with no `CLIENTPIDMAP`. |
| `UnsupportedVersion` | 5 | Nothing | A version that can't be read. |
| `VersionConversion` | 6 | | Not reported yet. It arrives with reading and writing vCard 3.0. |
| `Lossy` | 7 | `AllowLossyVersionConversion` | Not reported yet. It arrives with reading and writing vCard 3.0. |
| `LegacySyntax` | 8 | `AcceptLegacySyntax`, reading only | A parameter with no name, or `ENCODING` or `CHARSET`, in a vCard 4.0 card. |
| `InvalidStructure` | 9 | `AllowVersionNotFirst`, for that case only | Lines that don't follow the `vcard` rule of RFC 6350 §3.3. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardDiagnostic](VcardDiagnostic.md)
- [VcardOptions](VcardOptions.md)
