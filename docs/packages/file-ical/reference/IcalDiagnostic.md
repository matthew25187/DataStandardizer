---
title: IcalDiagnostic Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalDiagnostic Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A conformance problem found by `IcalComponent.Validate`: its kind, a description, and
where it was found.

**Syntax**

```csharp
public sealed class IcalDiagnostic
```

## Remarks

`IcalComponent.Validate` checks a component and the components nested in it against
RFC 5545 and RFC 7986, and returns every problem found as an `IcalDiagnostic`, in the
order of the lines they were found on. It throws nothing for a problem, whatever the
options. Use it for a model built in code, or one read with accommodations turned on.

The readers and writers of the component model run the same checks. The first
problem their options do not accept becomes an [IcalFileException](IcalFileException.md)
whose `Data` holds this diagnostic's `ComponentPath`, `Kind` (as `DiagnosticKind`),
`LineNumber` (when not 0) and `PropertyName` (when not `null`), and whose message is
`Message`.

Instances are immutable, so they may be shared between threads.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalDiagnostic(IcalDiagnosticKind kind, string message, string componentPath, string? propertyName, long lineNumber)` | `propertyName` is `null` when the problem is not with one property; `lineNumber` is 0 when the model was not read from a source. `kind` and `lineNumber` are not checked. Throws `ArgumentNullException` if `message` or `componentPath` is `null`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ComponentPath` | `string ComponentPath { get; }` | The path of the component the problem was found in, such as `VCALENDAR/VEVENT[3]/VALARM[1]`. The path starts with the component that was validated, which has no index. Each component under it has a 1-based index among the components of the same name in its parent. |
| `Kind` | `IcalDiagnosticKind Kind { get; }` | The kind of problem. See [IcalDiagnosticKind](IcalDiagnosticKind.md). |
| `LineNumber` | `long LineNumber { get; }` | The 1-based number of the logical line the problem was found on: the line of the property at fault, or the `BEGIN` line of the component. For a model read by `XcalReader`, the line of the XML document on which the element starts. 0 if the model was not read from a source, which includes a model read by `JcalReader`. |
| `Message` | `string Message { get; }` | A description of the problem. |
| `PropertyName` | `string? PropertyName { get; }` | The name of the property at fault, or `null` if the problem is not with one property. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | `ComponentPath (line LineNumber): Message`, or `ComponentPath: Message` when `LineNumber` is 0. Formatted with the invariant culture. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable annotations on `propertyName` and `PropertyName` apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Standards compliance](../concepts/standards-compliance.md)
- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [IcalDiagnosticKind](IcalDiagnosticKind.md)
- [IcalComponent](IcalComponent.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
