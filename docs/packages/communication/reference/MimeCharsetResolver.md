---
title: MimeCharsetResolver Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeCharsetResolver Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Gets the `Encoding` for a character set, from the encodings built into the package, then the
encodings the runtime provides, and then, optionally, a resolver the caller supplies.

**Syntax**

```csharp
public sealed class MimeCharsetResolver : IMimeCharsetResolver
```

## Remarks

See [Resolve character sets](../how-to/resolve-character-sets.md) for the order in which a
character set is resolved, how the built-in encodings behave, and how to register
`CodePagesEncodingProvider`. `Encoding.Default` is never used.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MimeCharsetResolver(IMimeCharsetResolver fallback)` | Resolves the character sets that `Default` doesn't with `fallback`. Throws `ArgumentNullException` if it's `null`. |

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Default` | `MimeCharsetResolver` | Static. Resolves the built-in encodings and those the runtime provides. |

## Methods

| Method | Notes |
| --- | --- |
| `bool TryGetEncoding(MimeCharset charset, out Encoding encoding)` | `false` if the character set isn't resolved. Throws `ArgumentException` if `charset` isn't initialised. |
