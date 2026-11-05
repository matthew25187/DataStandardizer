---
title: IVcard30PropertyList Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard30PropertyList&lt;TValue&gt; Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

The instances of one property of a vCard 3.0 card, as a live view of the card. See [IVcard30Card](IVcard30Card.md).

**Syntax**

```csharp
public interface IVcard30PropertyList<TValue> : IReadOnlyList<IVcard30Property<TValue>>
```

## Methods

| Method | Notes |
| --- | --- |
| `Add(value)`, `Add(value, parameters)` | Adds an instance after the last property of the card, adapted into canonical form. The parameters are a [Vcard30PropertyParameters](Vcard30PropertyParameters.md). |
| `Clear()` | Removes every instance, including any whose value isn't valid or that vCard 3.0 can't show. |
| `Remove(property)` | Removes an instance, and says whether it was one of this property in the card. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
