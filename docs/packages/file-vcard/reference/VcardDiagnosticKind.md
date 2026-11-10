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
| `MissingRequiredProperty` | 1 | `AllowMissingRequiredProperties` | `FN`, the `VERSION` of a card being read, or a `CONTACT-URI` with a `mailto`, `http` or `https` URI in a card that has others (RFC 8605 §2.1). |
| `CardinalityViolation` | 2 | `AllowCardinalityViolations` | More instances than RFC 6350 §6 or the extension that defines the property allows, or a `MEMBER` without `KIND:group`. |
| `InvalidValue` | 3 | `AllowInvalidValues` | A value that doesn't follow its syntax, or a `VALUE` the property doesn't allow. |
| `InvalidParameter` | 4 | `AllowInvalidValues` | A parameter on a property that doesn't allow it, repeated, or not valid; a `PID` source with no `CLIENTPIDMAP`; several `CONTACT-URI` properties with no `PREF` (RFC 8605 §2.1); or a rule of RFC 9554 that spans a property or several, such as a `SOCIALPROFILE` as text with no `SERVICE-TYPE`, `GRAMGENDER` instances with the same `LANGUAGE`, or a `PHONETIC` with no related property. |
| `UnsupportedVersion` | 5 | Nothing | A version that can't be read. |
| `VersionConversion` | 6 | Always | A property of an older version has no equivalent in vCard 4.0, and is kept as one that version doesn't define. `VcardVersionConverter.Analyze` reports it. |
| `Lossy` | 7 | `AllowLossyVersionConversion` | A card holds data that the version being written can't hold. The option leaves it out. |
| `LegacySyntax` | 8 | `AcceptLegacySyntax`, reading only | A parameter with no name, `CHARSET`, or an `ENCODING` other than the `b` of vCard 3.0, in a vCard 4.0 or 3.0 card. |
| `InvalidStructure` | 9 | `AllowVersionNotFirst`, for that case only | Lines that don't follow the `vcard` rule of RFC 6350 §3.3. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardDiagnostic](VcardDiagnostic.md)
- [VcardOptions](VcardOptions.md)
