---
title: IVcard21Property Interface
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# IVcard21Property&lt;TValue&gt; Interface

## Definition

Namespace: `DataStandardizer.File.vCard`

A property of a vCard 2.1 card, in the shape the specification of 1996 gives it. See [IVcard21Card](IVcard21Card.md).

**Syntax**

```csharp
public interface IVcard21Property<out TValue>
```

## Properties

| Property | Type | Notes |
| --- | --- | --- |
| `Name` | `string` | The name in vCard 2.1. |
| `Value` | `TValue` | The value. |
| `IsPreferred` | `bool` | Whether it has `PREF` in vCard 2.1, which the model holds as the lowest `PREF` among the instances of the property. The grammar of §2.9 allows `PREF` on any property. |
| `Types` | `IReadOnlyList<string>` | The `TYPE` values vCard 2.1 can write, as bare tokens: each `knowntype` of §2.9 in upper case, such as `WORK`, and each that starts with `X-`. It leaves out `PREF`, and the token of a value's format, which the value gives. |
| `Group` | `string?` | The group, without its `.`. |
| `Language` | `string?` | The `LANGUAGE` as written. vCard 2.1 §2.1.7 cites RFC 1766, so it isn't always a tag that `Bcp47LanguageTag` holds. |
| `ExtensionParameters` | `IReadOnlyList<VcardContentLineParameter>` | The parameters whose names start with `X-`, in order. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.
