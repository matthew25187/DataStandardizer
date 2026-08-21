---
title: ItuE164GlobalSubscriberNumber Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ItuE164GlobalSubscriberNumber Struct

## Definition

Namespace: `DataStandardizer.Communication.E164`

The global subscriber number field of a global-service number.

```csharp
public readonly struct ItuE164GlobalSubscriberNumber : IItuE164Field
```

## Remarks

The value is constructible from a `ulong` or a digit `string`; a non-digit string
throws `ArgumentException` (constructor) or `InvalidCastException` (cast). Implicit
conversions to and from `ulong` are defined, as is an explicit conversion from
`string`. `DigitCount` reports the number of digits.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ItuE164GlobalSubscriberNumber(string value)` | Throws `ArgumentException` if `value` is not all digits. |
| `ItuE164GlobalSubscriberNumber(ulong value)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `DigitCount` | `int DigitCount { get; }` | Number of digits in the value. Implements `IItuE164Field.DigitCount`. |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `ToString()` | `string` | Override. The digit string. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator ItuE164GlobalSubscriberNumber(string)` | Throws `InvalidCastException` if not all digits. |
| Implicit | `implicit operator ItuE164GlobalSubscriberNumber(ulong)` | |
| Implicit | `implicit operator ulong(ItuE164GlobalSubscriberNumber)` | |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164Field](IItuE164Field.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
</content>
