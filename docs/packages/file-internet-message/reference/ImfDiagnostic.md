---
title: ImfDiagnostic Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfDiagnostic Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A problem in a message that does not stop it being read.

**Syntax**

```csharp
public sealed class ImfDiagnostic
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `BytePosition` | `long BytePosition { get; }` | Gets the 0-based offset, in bytes from the start of the data, at which the problem was found. For a required field that is missing, it is the end of the header section. It is -1 for a message or field that was not read. |
| `EntityPath` | `string EntityPath { get; }` | Gets the path of the entity in which the problem was found, in the form that [`ImfEntityEvent.EntityPath`](ImfEntityEvent.md) describes. |
| `FieldName` | `string FieldName { get; }` | Gets the name of the header field that the problem is with, which for a required field that is missing is the name it should have. It is empty if the problem is not with one field. |
| `Kind` | `ImfDiagnosticKind Kind { get; }` | Gets the kind of problem. |
| `Message` | `string Message { get; }` | Gets a description of the problem. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [ImfEntityEvent](ImfEntityEvent.md)
- [File.InternetMessage API reference](index.md)
