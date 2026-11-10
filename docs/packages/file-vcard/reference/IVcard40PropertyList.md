---
title: IVcard40PropertyList Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard40PropertyList Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

The instances of one property of a vCard 4.0 card. It is a live view of the card.

**Syntax**

```csharp
public interface IVcard40PropertyList<TValue> : IReadOnlyList<IVcard40Property<TValue>>
```

It has the members of [VcardPropertyList](VcardPropertyList.md), in terms of `IVcard40Property<TValue>`. `Remove` gives `false` for an instance that isn't from a card.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [IVcard40Card](IVcard40Card.md)
- [VcardPropertyList](VcardPropertyList.md)
