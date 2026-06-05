---
title: SubtagRegistryFieldAttribute Class
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryFieldAttribute Class

## Definition

Namespace: `DataStandardizer.LanguageTag.SubtagRegistry`

Applied to a registry-record property to map it to its underlying registry field
name (used when the field name differs from the property name, e.g.
`Preferred-Value`).

```csharp
public class SubtagRegistryFieldAttribute : Attribute
```

## Remarks

The attribute targets properties only (`[AttributeUsage(AttributeTargets.Property)]`).
The [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md) `GetPropertyValue<T>()`
/ `GetPropertyValues<T>()` helpers read it to resolve a property to its registry
field.

## Constructors

| Constructor | Notes |
| --- | --- |
| `SubtagRegistryFieldAttribute(string fieldName)` | |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `FieldName` | `string FieldName { get; }` | The underlying registry field name. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`.

## See also

- [SubtagRegistryRecordBase](SubtagRegistryRecordBase.md)
- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [LanguageTag API reference](index.md)
