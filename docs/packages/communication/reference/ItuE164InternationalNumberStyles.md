---
title: ItuE164InternationalNumberStyles Enum
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ItuE164InternationalNumberStyles Enum

## Definition

Namespace: `DataStandardizer.Communication.E164`

Controls what a parse method tolerates around the digits of the input string.
No style allows anything between the digits. This is a flags
enumeration; its members can be combined with the bitwise OR operator.
`Parse` and `TryParse` throw `ArgumentOutOfRangeException` for a value with
any other bit set.

```csharp
[Flags]
public enum ItuE164InternationalNumberStyles : uint
```

## Fields

| Field | Value | Description |
| --- | --- | --- |
| `None` | `0` | Strict parsing: digits only, with no extra symbols or whitespace. |
| `AllowInternationalPrefixSymbol` | `1` | Permit a leading international prefix symbol (`+`). |
| `AllowLeadingWhite` | `2` | Permit leading whitespace. |
| `AllowTrailingWhite` | `4` | Permit trailing whitespace. |
| `InternationalNumber` | `1` | Alias for `AllowInternationalPrefixSymbol`. |
| `Any` | `7` | `AllowInternationalPrefixSymbol`, `AllowLeadingWhite`, and `AllowTrailingWhite` combined. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [Use E.164 international numbers](../how-to/use-international-numbers.md)
- [ItuE164InternationalNumber](ItuE164InternationalNumber.md)
- [Communication API reference](index.md)
