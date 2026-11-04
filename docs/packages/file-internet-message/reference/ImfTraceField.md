---
title: ImfTraceField Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfTraceField Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

A Return-Path or Received field (RFC 5322 §3.6.7). These are strictly informational, and any formal interpretation of them is left to RFC 5321.

**Syntax**

```csharp
public sealed class ImfTraceField
```

## Remarks

A property has `null` or nothing for a field that is not valid, which [`ImfMessage.Validate`](ImfMessage.md) reports. The field stays as it was in `Field`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Field` | `ImfHeaderField Field { get; }` | Gets the field, which holds what it was read with. |
| `Kind` | `ImfTraceFieldKind Kind { get; }` | Gets the kind of field. |
| `ReceivedDate` | `ImfDateTime? ReceivedDate { get; }` | Gets the date and time of a Received field, which is `null` if it has none, as the obsolete syntax allows (RFC 5322 §4.5.7), and for a Return-Path field. |
| `ReceivedPairs` | `IReadOnlyList<KeyValuePair<string, string>> ReceivedPairs { get; }` | Gets the name-value pairs of a Received field, in order, such as `from` and `x.y.test`; and none for a Return-Path field. RFC 5322 §3.6.7 has only a list of tokens, and RFC 5321 §4.4 has each clause be a name and a value. So each odd token is taken as a name and the next as its value, and a last token with none has an empty value. Comments are not kept. An address in angle brackets keeps them, as in `<mary@example.net>`. |
| `ReturnPath` | `ImfMailbox? ReturnPath { get; }` | Gets the mailbox of a Return-Path field, which is `null` if the path is empty, as in `<>`, and for a Received field. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfMessage](ImfMessage.md)
- [File.InternetMessage API reference](index.md)
