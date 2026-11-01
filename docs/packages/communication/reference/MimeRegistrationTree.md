---
title: MimeRegistrationTree Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# MimeRegistrationTree Enum

## Definition

Namespace: `DataStandardizer.Communication.Mime`

The registration tree of a media type (RFC 6838 §3), shown by the prefix of its subtype.
Read it from [MimeMediaType.Tree](MimeMediaType.md).

**Syntax**

```csharp
public enum MimeRegistrationTree
```

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `Standards` | 0 | No prefix, such as `text/plain` (RFC 6838 §3.1). |
| `Vendor` | 1 | The prefix `vnd.`, such as `application/vnd.ms-excel` (RFC 6838 §3.2). |
| `Personal` | 2 | The prefix `prs.` (RFC 6838 §3.3). |
| `Unregistered` | 3 | The prefix `x.`, or `x-` for types made before RFC 6838 (RFC 6838 §3.4). |
