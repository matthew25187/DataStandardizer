---
title: MboxEntry Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MboxEntry Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

One message of an mbox file, with its separator line.

**Syntax**

```csharp
public sealed class MboxEntry
```

## Constructors

| Constructor | Notes |
| --- | --- |
| `MboxEntry(MboxEnvelope envelope, ImfMessage message)` | Creates an entry. **Throws** `ArgumentNullException`: `envelope` or `message` is `null`. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Diagnostics` | `IReadOnlyList<ImfDiagnostic> Diagnostics { get; }` | Gets the problems found in reading this entry that did not stop it being read, as the options allow. The list is read-only. A problem with the format of the file gives a position in the file, and a problem in the message gives a position in the message as it reads after any quotes have been removed. |
| `Envelope` | `MboxEnvelope Envelope { get; }` | Gets the separator line. |
| `Message` | `ImfMessage Message { get; }` | Gets the message. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md)
- [File.InternetMessage API reference](index.md)
