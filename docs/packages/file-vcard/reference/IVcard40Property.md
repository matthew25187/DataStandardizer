---
title: IVcard40Property Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard40Property Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

A property of a vCard 4.0 card, in the shape RFC 6350 gives it. `VcardProperty<TValue>` implements it explicitly.

**Syntax**

```csharp
public interface IVcard40Property<out TValue>
```

It has the members of [VcardProperty](VcardProperty.md), each giving the same value, except `Line`, so code written against it can't reach the raw content line.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [IVcard40Card](IVcard40Card.md)
- [VcardProperty](VcardProperty.md)
