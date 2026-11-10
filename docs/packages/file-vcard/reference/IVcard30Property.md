---
title: IVcard30Property Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard30Property&lt;TValue&gt; Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

A property of a vCard 3.0 card, in the shape RFC 2426 gives it. See [IVcard30Card](IVcard30Card.md).

**Syntax**

```csharp
public interface IVcard30Property<out TValue>
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Name` | `string` | The name in vCard 3.0. |
| `Value` | `TValue` | The value. |
| `IsPreferred` | `bool` | Whether its `TYPE` has `pref` in vCard 3.0, which the model holds as the lowest `PREF` among the instances of the property. It is `false` on a type that can't say so in vCard 3.0, such as `URL` or `PHOTO`. |
| `Types` | `IReadOnlyList<string>` | The `TYPE` values as written, other than `pref`. |
| `Group` | `string?` | The group, without its `.`. |
| `Language` | `string?` | The `LANGUAGE` as written. RFC 2426 §4 cites RFC 1766, so it isn't always a tag that `Bcp47LanguageTag` holds. |
| `ExtensionParameters` | `IReadOnlyList<VcardContentLineParameter>` | The parameters no version defines, in order. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
