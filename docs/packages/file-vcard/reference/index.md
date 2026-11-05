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
| [VcardCard](VcardCard.md) | A vCard: an open list of properties, with typed accessors. |
| [VcardContentLine](VcardContentLine.md) | A content line (RFC 6350 §3.3): group, property name, parameters and value. |
| [VcardContentLineParameter](VcardContentLineParameter.md) | A parameter of a content line, with its values. |
| [VcardContentLineReader](VcardContentLineReader.md) | Reads the unfolded content lines of vCard data, with zero-copy and async options. |
| [VcardContentLineWriter](VcardContentLineWriter.md) | Writes content lines, folding them and treating each as its card's version says, with async and progress options. |
| [VcardDiagnostic](VcardDiagnostic.md) | A conformance problem. |
| [VcardFileException](VcardFileException.md) | The exception for a problem in vCard data, with where it was found. |
| [VcardOptions](VcardOptions.md) | Options for a reader or writer; the defaults follow RFC 6350 and its extensions. |
| [VcardLegacyMediaTypes](VcardLegacyMediaTypes.md) | Maps the TYPE tokens of older versions to media types. |
| [VcardParameterNames](VcardParameterNames.md) | The names of the parameters of vCard 4.0. |
| [VcardProperty](VcardProperty.md) | A property of a card with its value parsed. |
| [VcardPropertyInfo](VcardPropertyInfo.md) | Which versions define a property, and its cardinality. |
| [VcardPropertyList](VcardPropertyList.md) | The instances of one property of a card, as a live view. |
| [VcardPropertyNames](VcardPropertyNames.md) | The names of the properties of vCard 4.0. |
| [VcardPropertyParameters](VcardPropertyParameters.md) | The group and parameters of a property being added. |
| [VcardReader](VcardReader.md) | Reads vCard data into cards, sync and async. |
| [VcardValue](VcardValue.md) | TEXT, BOOLEAN, INTEGER, FLOAT, URI and LANGUAGE-TAG values. |
| [VcardValueTypeRegistry](VcardValueTypeRegistry.md) | The value type of each property, and `VALUE` overrides. |
| [VcardWriter](VcardWriter.md) | Writes cards, sync and async, with progress. |

## Interfaces

| Type | Description |
| --- | --- |
| [IVcard40Card](IVcard40Card.md) | A card in the shape of vCard 4.0. |
| [IVcard40Property](IVcard40Property.md) | A property in the shape of vCard 4.0. |
| [IVcard40PropertyList](IVcard40PropertyList.md) | The instances of one property in the shape of vCard 4.0. |

## Structs

| Type | Description |
| --- | --- |
| [VcardContentLineView](VcardContentLineView.md) | A zero-copy view of the line a reader has just read. |
| [VcardWriteProgress](VcardWriteProgress.md) | The progress of a writer. |
| [VcardAddress](VcardAddress.md) | The structured value of ADR. |
| [VcardClientPidMap](VcardClientPidMap.md) | The structured value of CLIENTPIDMAP. |
| [VcardComponentEnumerator](VcardComponentEnumerator.md) | Enumerates the components of a structured value in one pass. |
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
| [VcardTypedValue](VcardTypedValue.md) | The value of a property that can have more than one value type. |
| [VcardUtcOffset](VcardUtcOffset.md) | A UTC-OFFSET value. |

## Enumerations

| Type | Description |
| --- | --- |
| [VcardCardinality](VcardCardinality.md) | How many instances of a property a card may have. |
| [VcardDiagnosticKind](VcardDiagnosticKind.md) | The kinds of conformance problem. |
| [VcardKind](VcardKind.md) | The kind of object a card represents. |
| [VcardLegacySyntax](VcardLegacySyntax.md) | The lexical features of vCard 2.1 that a line uses. |
| [VcardLevel](VcardLevel.md) | The level of an expertise, hobby or interest. |
| [VcardPhoneticSystem](VcardPhoneticSystem.md) | The phonetic system of a value. |
| [VcardDateAndOrTimeKind](VcardDateAndOrTimeKind.md) | What a date-and-or-time holds. |
| [VcardSex](VcardSex.md) | The sex component of GENDER. |
| [VcardValueFormat](VcardValueFormat.md) | The lexical form of a value. |
| [VcardValueType](VcardValueType.md) | The value types of RFC 6350 §4. |
| [VcardVersion](VcardVersion.md) | A version of the vCard format. |
