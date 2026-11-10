---
title: VcardClientPidMap Struct
parent: File.vCard
grand_parent: Packages
nav_exclude: true
---

# VcardClientPidMap Struct

## Definition

Namespace: `DataStandardizer.File.vCard`

The structured value of the CLIENTPIDMAP property (RFC 6350 §6.7.7): a PID source identifier and the URI that gives it a global meaning. Only vCard 4.0 has this property.

**Syntax**

```csharp
public readonly struct VcardClientPidMap : IEquatable<VcardClientPidMap>
```

## Remarks

The identifier must be strictly positive (§6.7.7).

**Allocation.** This type holds the URI. `TryParseParts` gives the identifier and the URI's text as it appears in the value, with any escaping, checking its characters and scheme (RFC 3986) but leaving `System.Uri` to go further. It doesn't allocate unless the text has an escape.

**Forms.** Only `VcardValueFormat.Text` has a form of its own. In jCard (RFC 7095 §3.3.1.3) the value is a JSON array with an element for each component, and in xCard (RFC 6351 §5) a tree of XML elements, one for each component, so `Json` and `Xml` throw `NotSupportedException`. `JcardReader`, `JcardWriter`, `XcardReader` and `XcardWriter` map them to and from the text form.

**Uninitialised instance.** A default instance throws `InvalidOperationException` from `SourceId` and `Uri`. `TryFormat` writes nothing and returns `true`, `ToString()` gives the type name, and two default instances are equal.

## Constructors

| Constructor | Notes |
| --- | --- |
| `VcardClientPidMap(long sourceId, Uri uri)` | The URI must be absolute, and must not have been given with characters a URI value can't hold, such as a space. |

## Properties

| Property | Notes |
| --- | --- |
| `long SourceId`, `Uri Uri` | The parts. |

## Methods

| Method | Notes |
| --- | --- |
| `TryParse`, `Parse`, `TryFormat`, `ToString` | As for the other values, with no version. |
| `TryParseParts(value, format, out sourceId, out uri)` | Parses without allocating, unless the URI has an escape. |

See the [API reference](index.md).
