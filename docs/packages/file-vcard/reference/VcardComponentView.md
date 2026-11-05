---
title: VcardComponentView Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardComponentView Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

A zero-copy view of one component of a structured value. Enumerate it for its items.

**Syntax**

```csharp
public readonly ref struct VcardComponentView
```

## Remarks

An empty component has no items.

## Members

| Member | Notes |
| --- | --- |
| `ReadOnlySpan<char> RawValue` | The text, with its escaping in place. |
| `bool IsEmpty` | Whether it has no text. |
| `GetEnumerator()` | A [VcardListEnumerator](VcardListEnumerator.md) of its items. |

See the [API reference](index.md).
