---
title: VcardContentLineParameter Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardContentLineParameter Class

## Definition

Namespace: `DataStandardizer.File.vCard`

A parameter of a content line (RFC 6350 §5), with its list of values.

**Syntax**

```csharp
public sealed class VcardContentLineParameter
```

## Remarks

Values never include their enclosing double quotes. On a line read from data, RFC 6868
caret encoding is decoded only where
[VcardContentLineReader](VcardContentLineReader.md#caret-encoding) says it is, and left
as written elsewhere. Pass values to a constructor without caret encoding.

The constructors check only for `null` and for at least one value.

Parameter names are case-insensitive. Instances are immutable, so they may be shared
between threads.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardContentLineParameter(string name, params string[] values)` | There must be at least one value. Throws `ArgumentNullException` if `name` or `values` is `null`, or `ArgumentException` if `values` is empty or contains `null`. |
| `VcardContentLineParameter(string name, IEnumerable<string> values)` | The values are copied, in order. Throws as above. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Name` | `string Name { get; }` | The parameter name, as written. |
| `Values` | `IReadOnlyList<string> Values { get; }` | The values, in order. Always at least one. The list is read-only: it can't be cast to an array or a writable list and changed. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, and `net10.0`.

## See also

- [VcardContentLine](VcardContentLine.md)
