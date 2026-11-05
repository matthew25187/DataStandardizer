---
title: API reference
parent: File.vCard
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.File.vCard API reference

The public types of **DataStandardizer.File.vCard**. All types are in the
`DataStandardizer.File.vCard` namespace.

## Classes

| Type | Description |
| --- | --- |
| [VcardContentLine](VcardContentLine.md) | A content line (RFC 6350 §3.3): group, property name, parameters and value. |
| [VcardContentLineParameter](VcardContentLineParameter.md) | A parameter of a content line, with its values. |
| [VcardContentLineReader](VcardContentLineReader.md) | Reads the unfolded content lines of vCard data, with zero-copy and async options. |
| [VcardContentLineWriter](VcardContentLineWriter.md) | Writes content lines, folding them and treating each as its card's version says, with async and progress options. |
| [VcardFileException](VcardFileException.md) | The exception for a problem in vCard data, with where it was found. |
| [VcardOptions](VcardOptions.md) | Options for a reader or writer; RFC 6350 defaults. |
| [VcardLegacyMediaTypes](VcardLegacyMediaTypes.md) | Maps the TYPE tokens of older versions to media types. |
| [VcardValue](VcardValue.md) | TEXT, BOOLEAN, INTEGER, FLOAT, URI and LANGUAGE-TAG values. |
| [VcardValueTypeRegistry](VcardValueTypeRegistry.md) | The value type of each property, and `VALUE` overrides. |

## Structs

| Type | Description |
| --- | --- |
| [VcardContentLineView](VcardContentLineView.md) | A zero-copy view of the line a reader has just read. |
| [VcardWriteProgress](VcardWriteProgress.md) | The progress of a writer. |
| [VcardAddress](VcardAddress.md) | The structured value of ADR. |
| [VcardClientPidMap](VcardClientPidMap.md) | The structured value of CLIENTPIDMAP. |
| [VcardComponentView](VcardComponentView.md) | A zero-copy view of one component of a structured value. |
| [VcardDate](VcardDate.md) | A DATE value, which may be reduced or truncated. |
| [VcardDateAndOrTime](VcardDateAndOrTime.md) | A DATE-AND-OR-TIME value. |
| [VcardDateTime](VcardDateTime.md) | A DATE-TIME value. |
| [VcardGender](VcardGender.md) | The structured value of GENDER. |
| [VcardGeo](VcardGeo.md) | The value of GEO, in the form of each version. |
| [VcardListEnumerator](VcardListEnumerator.md) | Enumerates the items of a list of values. |
| [VcardName](VcardName.md) | The structured value of N. |
| [VcardOrganization](VcardOrganization.md) | The structured value of ORG. |
| [VcardPid](VcardPid.md) | A value of the PID parameter. |
| [VcardStructuredValueView](VcardStructuredValueView.md) | A zero-copy view of a structured value. |
| [VcardTime](VcardTime.md) | A TIME value, which may be reduced or truncated. |
| [VcardTimestamp](VcardTimestamp.md) | A TIMESTAMP value. |
| [VcardUtcOffset](VcardUtcOffset.md) | A UTC-OFFSET value. |

## Enumerations

| Type | Description |
| --- | --- |
| [VcardLegacySyntax](VcardLegacySyntax.md) | The lexical features of vCard 2.1 that a line uses. |
| [VcardDateAndOrTimeKind](VcardDateAndOrTimeKind.md) | What a date-and-or-time holds. |
| [VcardSex](VcardSex.md) | The sex component of GENDER. |
| [VcardValueFormat](VcardValueFormat.md) | The lexical form of a value. |
| [VcardValueType](VcardValueType.md) | The value types of RFC 6350 §4. |
| [VcardVersion](VcardVersion.md) | A version of the vCard format. |
