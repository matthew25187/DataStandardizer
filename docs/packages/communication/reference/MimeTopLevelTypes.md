---
title: MimeTopLevelTypes Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeTopLevelTypes Class

## Definition

Namespace: `DataStandardizer.Communication.Mime`

The registered top-level media types, as `string` constants.

**Syntax**

```csharp
public static class MimeTopLevelTypes
```

## Remarks

This isn't a copy of the IANA media type registry. Any top-level type that follows the
naming rules can be used.

## Fields

| Field | Value | Standard |
| --- | --- | --- |
| `Text` | `text` | RFC 2046 §4.1 |
| `Image` | `image` | RFC 2046 §4.2 |
| `Audio` | `audio` | RFC 2046 §4.3 |
| `Video` | `video` | RFC 2046 §4.4 |
| `Application` | `application` | RFC 2046 §4.5 |
| `Multipart` | `multipart` | RFC 2046 §5.1 |
| `Message` | `message` | RFC 2046 §5.2 |
| `Font` | `font` | RFC 8081 |
| `Model` | `model` | RFC 2077 |
| `Haptics` | `haptics` | RFC 9695 |
