---
title: EmlxPartResolver Delegate
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxPartResolver Delegate

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

Supplies the content of a part that a partial EMLX file stores in another file.

**Syntax**

```csharp
public delegate Stream? EmlxPartResolver(EmlxMissingPart part);
```

## Parameters

| Parameter | Meaning |
| --- | --- |
| `part` | The part. |

## Returns

A stream of the bytes of the body of the part as the message would hold them, still in its transfer encoding, which the reader reads to its end and disposes; or `null` if the content isn't available.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [File.InternetMessage API reference](index.md)
