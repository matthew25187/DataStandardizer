---
title: ItuE164SharedCodeAttribute Class
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ItuE164SharedCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Communication.E164`

Annotates an assigned-code enum member with the E.164 country code under which the
code is shared.

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class ItuE164SharedCodeAttribute : Attribute
```

## Remarks

This attribute is applied to members of the generated assigned-code enumerations
(for example `ItuE164AssignedIdentificationCodesForNetworks`) to record their
governing country code.

## Constructors

| Constructor | Notes |
| --- | --- |
| `ItuE164SharedCodeAttribute(ushort countryCode)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CountryCode` | `ushort CountryCode { get; }` | The shared E.164 country code. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [ItuE164AssignedIdentificationCodesForNetworks](ItuE164AssignedIdentificationCodesForNetworks.md)
- [Communication API reference](index.md)
</content>
