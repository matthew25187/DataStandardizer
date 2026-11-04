---
title: MboxEnvelope Class
parent: File.InternetMessage
grand_parent: Packages
nav_exclude: true
---

# MboxEnvelope Class

## Definition

Namespace: `DataStandardizer.File.InternetMessage`

The separator line before a message in an mbox file (RFC 4155 Appendix A): `From`, a space, the address of the sender, a space, and the UTC date and time that the message was received, in the form of the `ctime` function with no time zone.

**Syntax**

```csharp
public sealed class MboxEnvelope
```

## Remarks

An instance is immutable. One that was read keeps its line as it was, so it is written back byte for byte. With [`ImfOptions.AllowInvalidMboxSeparator`](ImfOptions.md), a line in another form can be read, and then `Sender` or `Date` is `null` if the line doesn't give one in a form that can be understood.

## Constructors

| Constructor | Notes |
| --- | --- |
| `MboxEnvelope(ImfMailbox sender, DateTimeOffset date)` | Creates a separator line. **Throws** `ArgumentNullException`: `sender` is `null`. **Throws** `ArgumentException`: The address of `sender` isn't the `addr-spec` of US-ASCII characters that RFC 4155 Appendix A requires. **Throws** `ArgumentOutOfRangeException`: The year of `date` in UTC doesn't have four digits. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Date` | `DateTimeOffset? Date { get; }` | Gets the date and time that the message was received, or `null` if the line was read under [`ImfOptions.AllowInvalidMboxSeparator`](ImfOptions.md) and doesn't give one that can be understood, such as one with a time zone given as a name other than UTC or GMT. A leap second becomes second 59 of the same minute, as `DateTimeOffset` has no leap seconds, and `RawLine` keeps it. |
| `RawLine` | `ReadOnlyMemory<byte> RawLine { get; }` | Gets the bytes of the line, without its line break. |
| `Sender` | `ImfMailbox? Sender { get; }` | Gets the sender, or `null` if the line was read under [`ImfOptions.AllowInvalidMboxSeparator`](ImfOptions.md) and doesn't give one as an `addr-spec`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | Gets the line, without its line break. **Returns:** The line, with each byte as one character. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1` and `net10.0` builds.

## See also

- [Read, filter and rewrite an mbox file](../how-to/read-and-rewrite-mbox.md)
- [ImfOptions](ImfOptions.md)
- [File.InternetMessage API reference](index.md)
