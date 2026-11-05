---
title: VcardComponentEnumerator Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardComponentEnumerator Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

Enumerates the components of a structured value in one pass. Get one by enumerating a [VcardStructuredValueView](VcardStructuredValueView.md).

**Syntax**

```csharp
public ref struct VcardComponentEnumerator
```

## Remarks

It gives the components the value was written with, including empty ones. It allocates nothing, and unlike repeated calls to `GetComponent`, it doesn't scan from the start of the value for each one.

## Members

| Member | Notes |
| --- | --- |
| `Current`, `MoveNext()`, `GetEnumerator()` | For use with `foreach`. `Current` is a [VcardComponentView](VcardComponentView.md). |

See the [API reference](index.md).
