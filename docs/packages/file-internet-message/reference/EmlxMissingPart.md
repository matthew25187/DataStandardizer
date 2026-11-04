---
title: EmlxMissingPart Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# EmlxMissingPart Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A part that a partial EMLX file stores in another file: an entity with an `X-Apple-Content-Length` field and an empty body.

**Syntax**

```csharp
public sealed class EmlxMissingPart
```

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `ContentLength` | `long? ContentLength { get; }` | Gets the value of the `X-Apple-Content-Length` field, or `null` if it isn't a number. Community documentation doesn't say whether this counts the bytes before or after the transfer encoding, so it is for information only. |
| `Entity` | `ImfEntity Entity { get; }` | Gets the entity, whose body is empty. |
| `Path` | `string Path { get; }` | Gets the number of the part, as IMAP numbers it, and as the name of its file does: `2`, or `1.2` for the second part of the first part. The body of a message with no parts is `1`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetPartFileName(string messageFileName)` | `string` | Gets the name of the file that holds this part, by the convention that open-source readers follow: `123.partial.emlx` keeps part 2 in `123.2.emlxpart`. This only works out the name, and any folders in `messageFileName` stay. Opening the file is left to the [`EmlxPartResolver`](EmlxPartResolver.md). **Returns:** The name of the file of the part. **Throws** `ArgumentNullException`: `messageFileName` is `null`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read Apple Mail EMLX files](../how-to/read-emlx.md)
- [EmlxPartResolver](EmlxPartResolver.md)
- [File.InternetMessage API reference](index.md)
