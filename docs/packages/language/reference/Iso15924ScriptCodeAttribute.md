---
title: Iso15924ScriptCodeAttribute Class
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso15924ScriptCodeAttribute Class

## Definition

Namespace: `DataStandardizer.Language`

Describes an [Iso15924Script](Iso15924Script.md) code member with its metadata:
the script's age, alias, and date. English and French names are inherited from
`CodeAttributeBase`.

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class Iso15924ScriptCodeAttribute : CodeAttributeBase
```

## Remarks

This attribute is applied to each member of [Iso15924Script](Iso15924Script.md).
Its constructors are `internal`, so you read the metadata through the
[Iso15924Extensions](Iso15924Extensions.md) accessors rather than constructing the
attribute yourself.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Age` | `double? Age { get; }` | The Unicode version in which the script was encoded, or `null`. |
| `Alias` | `string? Alias { get; set; }` | The alias for the script code, or `null`. |
| `Date` | `DateOnly? Date { get; }` *(net6.0+)* / `DateTime? Date { get; }` | The script code's date. Returns a `DateOnly?` on the `net8.0`/`net10.0` targets and a `DateTime?` on the .NET Standard targets. |

Inherited from `CodeAttributeBase`: `EnglishName`, `EnglishNames`, `FrenchName`,
`FrenchNames`.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. The `Date`
property is `DateOnly?` on the `net8.0` and `net10.0` builds and `DateTime?` on the
`netstandard1.0` and `netstandard2.0` builds. On the .NET Standard targets the
`Alias` property is annotated with JetBrains `[CanBeNull]` rather than a C# nullable
reference type.

## See also

- [Iso15924Extensions](Iso15924Extensions.md)
- [Iso15924Script](Iso15924Script.md)
- [Access script metadata](../how-to/access-script-metadata.md)
- [Language API reference](index.md)
