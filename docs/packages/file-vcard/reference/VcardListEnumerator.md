---
title: VcardListEnumerator Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardListEnumerator Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

Enumerates the items of a list of values, with their escaping in place. Get one from `VcardValue.EnumerateList` or by enumerating a [VcardComponentView](VcardComponentView.md).

**Syntax**

```csharp
public ref struct VcardListEnumerator
```

## Remarks

An empty list from `EnumerateList` yields one empty item, which a `text-list` allows (RFC 6350 §4). An empty component yields none.

## Members

| Member | Notes |
| --- | --- |
| `Current`, `MoveNext()`, `GetEnumerator()` | For use with `foreach`. |

See the [API reference](index.md).
