---
title: IcalParameterNames Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalParameterNames Class

## Definition

Namespace: `DataStandardizer.File.iCal`

The names of the property parameters that RFC 5545 and RFC 7986 define, as `string`
constants.

**Syntax**

```csharp
public static class IcalParameterNames
```

## Remarks

Parameter names are case-insensitive. These constants are in the upper case the RFCs
use. Use them with `IcalContentLine.GetParameter`, or to create an
[IcalContentLineParameter](IcalContentLineParameter.md).

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `AlternateRepresentation` | `"ALTREP"` | An alternate text representation (RFC 5545 §3.2.1). |
| `CommonName` | `"CN"` | The common name of a calendar user (RFC 5545 §3.2.2). |
| `CalendarUserType` | `"CUTYPE"` | The type of a calendar user (RFC 5545 §3.2.3). |
| `DelegatedFrom` | `"DELEGATED-FROM"` | The calendar users that delegated participation (RFC 5545 §3.2.4). |
| `DelegatedTo` | `"DELEGATED-TO"` | The calendar users participation was delegated to (RFC 5545 §3.2.5). |
| `Directory` | `"DIR"` | A directory entry for a calendar user (RFC 5545 §3.2.6). |
| `Encoding` | `"ENCODING"` | The inline encoding of a value (RFC 5545 §3.2.7). |
| `FormatType` | `"FMTTYPE"` | The media type of a value (RFC 5545 §3.2.8). |
| `FreeBusyType` | `"FBTYPE"` | Whether a time is free or busy (RFC 5545 §3.2.9). |
| `Language` | `"LANGUAGE"` | The language of a text value (RFC 5545 §3.2.10). |
| `Member` | `"MEMBER"` | The groups a calendar user belongs to (RFC 5545 §3.2.11). |
| `ParticipationStatus` | `"PARTSTAT"` | The participation status of a calendar user (RFC 5545 §3.2.12). |
| `Range` | `"RANGE"` | The recurrence instances a recurrence identifier applies to (RFC 5545 §3.2.13). |
| `Related` | `"RELATED"` | Whether a relative trigger is from the start or the end (RFC 5545 §3.2.14). |
| `RelationshipType` | `"RELTYPE"` | The type of a relationship (RFC 5545 §3.2.15). |
| `Role` | `"ROLE"` | The participation role of a calendar user (RFC 5545 §3.2.16). |
| `Rsvp` | `"RSVP"` | Whether a reply is expected (RFC 5545 §3.2.17). |
| `SentBy` | `"SENT-BY"` | The calendar user acting on behalf of another (RFC 5545 §3.2.18). |
| `TimeZoneId` | `"TZID"` | The time zone of a DATE-TIME value (RFC 5545 §3.2.19). |
| `Value` | `"VALUE"` | The value type of a property (RFC 5545 §3.2.20). |
| `Display` | `"DISPLAY"` | How an image is to be displayed (RFC 7986 §6.1). |
| `Email` | `"EMAIL"` | The email address of a calendar user (RFC 7986 §6.2). |
| `Feature` | `"FEATURE"` | The features of a conference system (RFC 7986 §6.3). |
| `Label` | `"LABEL"` | A label for a conference system (RFC 7986 §6.4). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [The layered model](../concepts/layered-model.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalContentLineParameter](IcalContentLineParameter.md)
- [IcalComponentNames](IcalComponentNames.md)
- [IcalPropertyNames](IcalPropertyNames.md)
- [File.iCal API reference](index.md)
