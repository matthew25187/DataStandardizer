---
title: VcardLegacyMediaTypes Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardLegacyMediaTypes Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Maps the `TYPE` tokens that vCard 3.0 and 2.1 use for inline binary values, such as `JPEG`, to the media types an RFC 2397 `data:` URI holds in vCard 4.0.

**Syntax**

```csharp
public static class VcardLegacyMediaTypes
```

## Remarks

Use it with `MimeDataUri` from `DataStandardizer.Communication.Mime`, which takes the place of a vCard-specific `data:` URI type.

| Token | Media type |
| --- | --- |
| `GIF`, `CGM`, `WMF`, `BMP`, `TIFF`, `JPEG`, `PNG` | `image/` and the name in lower case |
| `PDF` | `application/pdf` |
| `PS` | `application/postscript` |
| `QTIME` | `video/quicktime` |
| `MPEG`, `MPEG2` | `video/mpeg` |
| `AVI` | `video/vnd.avi` |
| `WAVE` | `audio/vnd.wave`; `audio/wav` maps back to it too |
| `BASIC`, `PCM` | `audio/basic` |
| `X509` | `application/pkix-cert` |
| `PGP` | `application/pgp-keys` |

A token is mapped where the IANA registry or RFC 2361 gives a media type. The other formats of vCard 2.1 §2.9 (`MET`, `PMB`, `DIB`, `PICT` and `AIFF`) have none, so they have no mapping. vCard 2.1 writes only the tokens of its §2.9, so it has `PCM` for `audio/basic` where vCard 3.0 has `BASIC`, and no token for `image/png`.

## Methods

| Method | Notes |
| --- | --- |
| `TryGetMediaType(token, out mediaType)` | The media type a token stands for, compared case-insensitively. |
| `TryGetToken(mediaType, version, out token)` | The token a version writes. Parameters are ignored. vCard 4.0 has none, since it uses media types. |

See the [API reference](index.md).
