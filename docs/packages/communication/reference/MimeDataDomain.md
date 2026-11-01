---
title: MimeDataDomain Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeDataDomain Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

The kind of data a transport must carry for a body (RFC 2045 §2.7–§2.9). Read it from
[MimeTransferEncoding.Domain](MimeTransferEncoding.md).

**Syntax**

```csharp
public enum MimeDataDomain
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Unknown` | 0 | Not known, as for an extension transfer encoding, whose output RFC 2045 and RFC 4289 don't constrain. |
| `SevenBit` | 1 | Short lines of US-ASCII characters, with no NULs, and CR and LF only as a CRLF line break (RFC 2045 §2.7). |
| `EightBit` | 2 | Short lines that may contain octets above 127, with no NULs, and CR and LF only as a CRLF line break (RFC 2045 §2.8). |
| `Binary` | 3 | Any sequence of octets (RFC 2045 §2.9). |
