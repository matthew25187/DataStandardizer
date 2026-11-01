---
title: IMimeCharsetResolver Interface
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# IMimeCharsetResolver Interface

## Definition

Namespace: `DataStandardizer.Communication.Mime`

Gets the `Encoding` for a character set. Implement this to add character sets that
`MimeCharsetResolver.Default` doesn't resolve, and pass it to the
[MimeCharsetResolver](MimeCharsetResolver.md) constructor.

**Syntax**

```csharp
public interface IMimeCharsetResolver
```

## Methods

| Method | Notes |
| --- | --- |
| `bool TryGetEncoding(MimeCharset charset, out Encoding encoding)` | `false` if the character set isn't resolved. On the `net8.0` and `net10.0` builds, the parameter is `[NotNullWhen(true)] out Encoding? encoding`. |
