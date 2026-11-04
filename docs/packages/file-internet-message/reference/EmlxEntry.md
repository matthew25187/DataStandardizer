---
title: EmlxEntry Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxEntry Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The content of an EMLX file: one message and its metadata.

**Syntax**

```csharp
public sealed class EmlxEntry
```

## Constructors

| Constructor | Notes |
| --- | --- |
| `EmlxEntry(ImfMessage message, EmlxMetadata metadata)` | Creates an entry. **Throws** `ArgumentNullException`: `message` or `metadata` is `null`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found in reading this entry that did not stop it being read. The list is read-only. A problem with the layout of the file gives a position in the file, and a problem in the message gives a position in the message. |
| `Message` | `ImfMessage Message { get; }` | Gets the message. |
| `Metadata` | `EmlxMetadata Metadata { get; }` | Gets the metadata. |
| `MissingParts` | `IReadOnlyList<EmlxMissingPart> MissingParts { get; }` | Gets the parts that the file stores elsewhere, and whose content wasn't supplied. The list is read-only. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [File.InternetMessage API reference](index.md)
