---
title: CsvFileIoBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileIoBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The shared abstract base of [CsvFileReader](CsvFileReader.md) and
[CsvFileWriter](CsvFileWriter.md). It holds the mapper registration methods
common to both.

**Syntax**

```csharp
public abstract class CsvFileIoBase<TRecordLine>
    where TRecordLine : CsvFileRecordLine
```

## Remarks

You can't derive from this type: its constructor is `private protected`, so only
the reader and writer in this package derive from it. Use it as the type of a
variable or parameter that takes either a reader or a writer.

A mapper registered with `RegisterMapper` applies only to the reader or writer
you register it on, and each reader or writer has at most one. A mapper type
is matched exactly: a mapper derived from `TMapper` counts as another type. To
use the same mapper with another reader or writer, register it on that one too.
Mappers built from
[CsvFieldMappingAttribute](CsvFieldMappingAttribute.md) attributes are shared by
every reader and writer, and are held in [CsvMapperCache](CsvMapperCache.md).

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `RegisterMapper<TMapper>()` | `void` | `where TMapper : CsvFileMapperBase<TRecordLine>, new()`. Registers a `TMapper` on this reader or writer, from the next record line it reads or writes. A reader or writer has at most one registered mapper, so this replaces a mapper of another type. Does nothing if a `TMapper` is already registered. Throws `ObjectDisposedException` if the reader or writer has been disposed. |
| `UnregisterMapper<TMapper>()` | `void` | `where TMapper : CsvFileMapperBase<TRecordLine>, new()`. Removes the registered mapper if it is a `TMapper`, from the next record line. Does nothing if no mapper is registered, or the registered one is of another type. Throws `ObjectDisposedException` if the reader or writer has been disposed. |

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [Map CSV files](../how-to/map-csv-files.md)
- [CsvFileReader](CsvFileReader.md)
- [CsvFileWriter](CsvFileWriter.md)
- [CsvFileMapperBase](CsvFileMapperBase.md)
- [CsvMapperCache](CsvMapperCache.md)
- [File.CSV API reference](index.md)
