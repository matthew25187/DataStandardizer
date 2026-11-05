---
title: VcardValueTypeRegistry Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardValueTypeRegistry Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Gives the value type of each property defined by RFC 6350 §6, and honours a `VALUE` parameter that overrides it.

**Syntax**

```csharp
public static class VcardValueTypeRegistry
```

## Remarks

Unknown and experimental (`X-`) properties are `Unknown` whatever their `VALUE` parameter says. Names are compared case-insensitively. A known type is returned even if the property doesn't allow it, which `IsAllowedValueType` checks. N, ADR, ORG, GENDER and CLIENTPIDMAP are `Text` here; their structure has a type of its own, such as [VcardName](VcardName.md).

This covers the properties of RFC 6350. The extension properties and those of older versions are added with the issues that implement them.

## Methods

| Method | Notes |
| --- | --- |
| `IsKnownProperty(propertyName)` | Whether the property's value type is known. |
| `GetDefaultValueType(propertyName)` | The type with no `VALUE` parameter. |
| `GetValueType(propertyName, valueParameter)`, `GetValueType(VcardContentLine)`, `GetValueType(VcardContentLineView)` | The type, taking any `VALUE` parameter into account. The view overload doesn't allocate. |
| `IsAllowedValueType(propertyName, valueType)` | Whether the property allows the type. |
| `TryParseValueTypeName(name, out valueType)`, `GetValueTypeName(valueType)` | Between a type and its name, such as `date-and-or-time`. |

See the [API reference](index.md).
