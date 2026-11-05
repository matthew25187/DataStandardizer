---
title: VcardValue Class
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardValue Class

## Definition

Namespace: `DataStandardizer.File.vCard`

Parses and formats the value types of RFC 6350 §4 that map to .NET types and other packages: TEXT, BOOLEAN, INTEGER, FLOAT, URI and LANGUAGE-TAG.

**Syntax**

```csharp
public static class VcardValue
```

## Remarks

**TEXT.** The grammar differs by version. vCard 4.0 requires a comma to be escaped and lets a semicolon be escaped or not (RFC 6350 §3.4, §4). vCard 3.0 requires both to be escaped (RFC 2426 §4). vCard 2.1 has no escaping outside a compound value, so its text is copied as it is. For a list, split it with `EnumerateList` first, then parse each item.

**INTEGER** is a `long`, the range RFC 6350 §4.5 gives. **FLOAT** is a `double`, with no exponent when read or written (§4.6). **BOOLEAN** is `TRUE` or `FALSE` in any case (§4.4). **URI** is an absolute `System.Uri`, written as it was given. Characters RFC 3986 doesn't allow are rejected when parsing and when writing, including a space, which `System.Uri` accepts, and `[` or `]` outside the host. A `geo` URI may have brackets in its parameter values (RFC 5870 §3.3).

**Allocation.** `TryParseUri`, `TryParseLanguageTag` and the `string` overload of `TryParseText` allocate. So does `TryParseFloat` on netstandard1.1 and netstandard2.0. The rest parse from a span without allocating. `TryFormatText` needs a destination that doesn't overlap the value.

**LANGUAGE-TAG** is a `Bcp47LanguageTag`, which decides which tags are well-formed (RFC 5646 §2.1). The check allocates nothing, but a tag that passes is copied to the one string it holds. A tag it rejects is an invalid value.

**Forms.** Only `VcardValueFormat.Text` is implemented. `Json` and `Xml` throw `NotSupportedException` until the jCard and xCard work adds them.

## Methods

| Method | Notes |
| --- | --- |
| `TryParseText`, `TryFormatText`, `FormatText` | TEXT, with overloads for a span or a string and for a version. |
| `EnumerateList(value, format)` | Splits a list on the commas that aren't escaped. See [VcardListEnumerator](VcardListEnumerator.md). |
| `TryParseBoolean`, `TryFormatBoolean` | BOOLEAN. |
| `TryParseInteger`, `TryFormatInteger` | INTEGER. |
| `TryParseFloat`, `TryFormatFloat` | FLOAT. |
| `TryParseUri`, `TryFormatUri` | URI. |
| `TryParseLanguageTag`, `TryFormatLanguageTag` | LANGUAGE-TAG. |

See the [API reference](index.md).
