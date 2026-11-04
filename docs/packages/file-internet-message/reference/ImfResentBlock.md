---
title: ImfResentBlock Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# ImfResentBlock Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The resent fields of one resending of a message (RFC 5322 §3.6.6).

**Syntax**

```csharp
public sealed class ImfResentBlock
```

## Remarks

A property has `null` for a field that is absent or not valid, which [`ImfMessage.Validate`](ImfMessage.md) reports. An obsolete Resent-Reply-To field (RFC 5322 §4.5.6) is among the `Fields`, and has no property of its own.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Bcc` | `ImfAddressList? Bcc { get; }` | Gets the addresses of the Resent-Bcc field. It is also `null` for a field with no addresses, which RFC 5322 §3.6.6 allows, and which is among the `Fields`. |
| `Cc` | `ImfAddressList? Cc { get; }` | Gets the addresses of the Resent-Cc field. |
| `Date` | `ImfDateTime? Date { get; }` | Gets the date and time of the Resent-Date field. |
| `Fields` | `IReadOnlyList<ImfHeaderField> Fields { get; }` | Gets the fields of the block, in order. |
| `From` | `ImfAddressList? From { get; }` | Gets the addresses of the Resent-From field. |
| `MessageId` | `ImfMessageId? MessageId { get; }` | Gets the identifier of the Resent-Message-ID field. |
| `Sender` | `ImfAddress? Sender { get; }` | Gets the address of the Resent-Sender field. |
| `To` | `ImfAddressList? To { get; }` | Gets the addresses of the Resent-To field. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [ImfMessage](ImfMessage.md)
- [File.InternetMessage API reference](index.md)
