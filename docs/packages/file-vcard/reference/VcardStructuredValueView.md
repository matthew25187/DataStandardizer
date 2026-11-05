---
title: VcardStructuredValueView Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardStructuredValueView Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A zero-copy view of a structured value such as N, ADR or ORG, split into its components.

**Syntax**

```csharp
public readonly ref struct VcardStructuredValueView
```

## Remarks

A type that keeps a value can't hold a span, so [VcardName](VcardName.md) and the like copy their text. This view is how to read one without allocating: `TryParseView` checks the whole value and copies nothing. A default instance has no components.

## Members

| Member | Notes |
| --- | --- |
| `int ComponentCount` | The number of components the value was written with. |
| `GetComponent(int index)` | A [VcardComponentView](VcardComponentView.md). It is empty if the value has fewer components. A negative index throws `ArgumentOutOfRangeException`. Each call scans from the start of the value, so enumerate the view to read them all. |
| `GetEnumerator()` | A `VcardComponentEnumerator`, which reads the components in one pass with `foreach`. |
| `UnescapeItem(item, destination)` | Removes the escaping of an item, and returns the number of characters written. |

See the [API reference](index.md).
