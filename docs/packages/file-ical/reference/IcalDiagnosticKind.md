---
title: IcalDiagnosticKind Enum
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDiagnosticKind Enum

## Definition

Namespace: `DataStandardizer.File.iCal`

The kinds of conformance problem that `IcalComponent.Validate` reports, as
`IcalDiagnostic.Kind`. The same value is put in the `DiagnosticKind` item of the
`Data` of an [IcalFileException](IcalFileException.md) that a reader or writer throws
for the problem.

**Syntax**

```csharp
public enum IcalDiagnosticKind
```

## Remarks

The readers and writers of the component model ([IcalReader](IcalReader.md),
[IcalWriter](IcalWriter.md), [XcalReader](XcalReader.md), [XcalWriter](XcalWriter.md),
[JcalReader](JcalReader.md) and [JcalWriter](JcalWriter.md)) throw `IcalFileException`
for the first problem whose kind the options do not accept. Four kinds can each be
accepted by an [IcalOptions](IcalOptions.md) property, all off by default. The other two
are never accepted. `IcalComponent.Validate` itself reports every kind and throws
nothing.

There is no member with the value 0, so `default(IcalDiagnosticKind)` is not a defined
kind.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `MissingRequiredProperty` | 1 | A required property or component is missing, such as the `UID` or `DTSTAMP` of a VEVENT, or the STANDARD or DAYLIGHT parts of a VTIMEZONE. Accepted with `IcalOptions.AllowMissingRequiredProperties`. |
| `CardinalityViolation` | 2 | A property appears more often than allowed, or two properties that exclude each other both appear, such as `DTEND` and `DURATION` in a VEVENT. Accepted with `IcalOptions.AllowCardinalityViolations`. |
| `UndefinedTimeZone` | 3 | A `TZID` parameter names a time zone that has no VTIMEZONE in the calendar (RFC 5545 §3.2.19). Checked only when the calendar is validated. Accepted with `IcalOptions.AllowUndefinedTimeZones`. |
| `InvalidValue` | 4 | A property value does not follow the syntax of its value type (RFC 5545 §3.3), or breaks a rule that ties it to its property. Accepted with `IcalOptions.AllowInvalidValues`. |
| `InvalidComponentNesting` | 5 | A component is inside a component where RFC 5545 does not allow it, such as a VEVENT inside a VTODO. Never accepted. |
| `UnsupportedVersion` | 6 | The `VERSION` property does not include iCalendar 2.0 (RFC 5545 §3.7.4). Never accepted. The readers throw it as soon as they read the `VERSION`, and cannot continue afterwards. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [IcalDiagnostic](IcalDiagnostic.md)
- [IcalFileException](IcalFileException.md)
- [IcalOptions](IcalOptions.md)
- [File.iCal API reference](index.md)
