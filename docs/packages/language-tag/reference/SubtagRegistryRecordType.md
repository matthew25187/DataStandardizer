---
title: SubtagRegistryRecordType Enum
parent: LanguageTag
grand_parent: Packages
nav_exclude: true
---

# SubtagRegistryRecordType Enum

## Definition

Namespace: `DataStandardizer.LanguageTag.Registry`

The value of the `Type` field of a Subtag Registry record (RFC 5646 §3.1.3).

```csharp
public enum SubtagRegistryRecordType
```

## Remarks

Returned by the `Type` property of
[SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md), and passed to
`TryGetSubtag` on [SubtagRegistry](SubtagRegistry.md). The registry's own text
for the field, such as `extlang`, is still in the record's `Fields`.

## Fields

| Field | Registry value | Description |
| --- | --- | --- |
| `Unknown` | | The default value. No record of a loaded registry has it: a record with no `Type` field, or with a value that RFC 5646 §3.1.3 doesn't define, fails the load. |
| `Language` | `language` | A Primary Language subtag. |
| `ExtendedLanguage` | `extlang` | An Extended Language subtag. |
| `Script` | `script` | A Script subtag. |
| `Region` | `region` | A Region subtag. |
| `Variant` | `variant` | A Variant subtag. |
| `Grandfathered` | `grandfathered` | A grandfathered tag. |
| `Redundant` | `redundant` | A redundant tag. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, and `net10.0`.

## See also

- [SubtagRegistry](SubtagRegistry.md)
- [SubtagRegistryTagRecordBase](SubtagRegistryTagRecordBase.md)
- [LanguageTag API reference](index.md)
