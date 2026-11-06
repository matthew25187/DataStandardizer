---
title: CsvMapperCache Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvMapperCache Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The process-wide cache of the mappers that readers and writers build from
[CsvFieldMappingAttribute](CsvFieldMappingAttribute.md) attributes on record line
types, and of the type converters those mappings name.

**Syntax**

```csharp
public static class CsvMapperCache
```

## Remarks

A reader or writer builds the mapper for a record line type from its attributes
the first time it reads or writes a line of that type, then caches it here for
every other reader and writer to use. Each entry is worked out from a type alone,
so clearing the cache never changes how a line is read or written: the next
reader or writer to need an entry builds it again.

Mappers registered with
[CsvFileIoBase&lt;TRecordLine&gt;.RegisterMapper](CsvFileIoBase.md) aren't held
here. Each reader or writer keeps its own.

**Thread safety.** All members are safe to call from several threads at once,
including while readers and writers are in use.

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Clear()` | `void` | Removes every cached mapper and type converter. Use it to release the memory they hold, or the types they refer to, for example before unloading a collectible `AssemblyLoadContext` that defines your record line types. A reader or writer that is already open keeps the mapper it has resolved. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Performance](../how-to/performance.md)
- [CsvFileIoBase](CsvFileIoBase.md)
- [CsvFieldMappingAttribute](CsvFieldMappingAttribute.md)
- [File.CSV API reference](index.md)
