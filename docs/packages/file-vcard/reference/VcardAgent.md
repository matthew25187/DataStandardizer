---
title: VcardAgent Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardAgent Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Who acts for the entity of a card: a `RELATED` property with `TYPE=agent` (RFC 6350 §6.6.6), which is the `AGENT` of older versions. Get one from `VcardCard.Agents`. See [Converting between versions](../version-conversion.md).

**Syntax**

```csharp
public sealed class VcardAgent
```

## Properties

One of these has a value.

| Property | Type | Notes |
| --- | --- | --- |
| `Uri` | `Uri?` | The URI. |
| `Card` | `VcardCard?` | The card, read from the text of the property, so a change to it isn't a change to the property. |
| `Text` | `string?` | The text of a value that isn't a card or a URI. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
