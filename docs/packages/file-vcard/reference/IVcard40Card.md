---
title: IVcard40Card Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard40Card Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

A card in the shape of vCard 4.0 (RFC 6350). Get one from `VcardCard.AsVersion40()`. See [The card object model](../card-model.md).

**Syntax**

```csharp
public interface IVcard40Card
```

`VcardCard` implements this explicitly, so a view is the same object, not a copy. The interface exposes only what vCard 4.0 defines, so code written against it is checked by the compiler against that version. It doesn't inherit from the interface of another version, and it doesn't expose `VcardCard.Properties`.

## Members

It has the members of [VcardCard](VcardCard.md) that vCard 4.0 defines, each giving the same value, with these differences:

| Member | Type |
| --- | --- |
| Properties with at most one instance, such as `Name` | `IVcard40Property<TValue>?` |
| Properties with any number of instances, such as `Telephones` | `IVcard40PropertyList<TValue>` |
| `ExtensionProperties` | `IReadOnlyList<IVcard40Property<string>>`, how unknown and `X-` properties are reached |
| `AddExtensionProperty` | Returns `IVcard40Property<string>` |

It also has `Kind`, `KindText`, the `Set` methods, `RemoveProperties` and `Validate`. It leaves out `Properties`, `SourceVersion`, `GetProperties`, `GetPreferred(string)`, `GetAlternatives(string)` and `ResolveMembers`; the lists have `GetPreferred()` and `GetAlternatives()` of their own.

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [IVcard40Property](IVcard40Property.md)
- [IVcard40PropertyList](IVcard40PropertyList.md)
- [VcardCard](VcardCard.md)
- [The card object model](../card-model.md)
