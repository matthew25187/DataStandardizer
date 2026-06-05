---
title: Iso15924Extensions Class
parent: Language
grand_parent: Packages
nav_exclude: true
---

# Iso15924Extensions Class

## Definition

Namespace: `DataStandardizer.Language`

Extension methods that read the metadata attached to the
[Iso15924Script](Iso15924Script.md) enum.

```csharp
public static class Iso15924Extensions
```

## Remarks

Each accessor reads the [Iso15924ScriptCodeAttribute](Iso15924ScriptCodeAttribute.md)
applied to the script code member and returns the requested metadata. Every method
returns `null` when the value is not a defined script code or the metadata is
absent.

## Methods

### Extension

| Method | Extends | Returns | Notes |
| --- | --- | --- | --- |
| `GetAge()` | `Iso15924Script` | `double?` | Unicode version in which the script was encoded, or `null`. |
| `GetAlias()` | `Iso15924Script` | `string?` | Script alias, or `null` when absent. |
| `GetDate()` | `Iso15924Script` | `DateOnly?` *(net6.0+)* / `DateTime?` | The script code's date. Returns `DateOnly?` on the `net8.0`/`net10.0` targets and `DateTime?` on the .NET Standard targets. |
| `GetEnglishName()` | `Iso15924Script` | `string?` | English name, or `null` when absent. |
| `GetFrenchName()` | `Iso15924Script` | `string?` | French name, or `null` when absent. |

On the `netstandard1.0` and `netstandard2.0` targets the `string?` return types are
plain `string` annotated with JetBrains `[CanBeNull]`.

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. `GetDate()`
returns `DateOnly?` on the `net8.0` and `net10.0` builds and `DateTime?` on the
`netstandard1.0` and `netstandard2.0` builds.

## See also

- [Iso15924Script](Iso15924Script.md)
- [Iso15924ScriptCodeAttribute](Iso15924ScriptCodeAttribute.md)
- [Access script metadata](../how-to/access-script-metadata.md)
- [Language API reference](index.md)
