---
title: IcalPropertyNames Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalPropertyNames Class

## Definition

Namespace: `DataStandardizer.File.iCal`

The names of the properties that RFC 5545 and RFC 7986 define, as `string`
constants.

**Syntax**

```csharp
public static class IcalPropertyNames
```

## Remarks

Property names are case-insensitive. These constants are in the upper case the RFCs
use. Use them with `IcalComponent.GetProperty`, `IcalComponent.GetProperties` and
`IcalComponent.RemoveProperties`, or to create an
[IcalContentLine](IcalContentLine.md). [IcalValueTypeRegistry](IcalValueTypeRegistry.md)
gives the value type of each of these properties.

## Fields

| Field | Value | Meaning |
| --- | --- | --- |
| `CalendarScale` | `"CALSCALE"` | The calendar scale (RFC 5545 §3.7.1). |
| `Method` | `"METHOD"` | The iTIP method (RFC 5545 §3.7.2). |
| `ProductId` | `"PRODID"` | The identifier of the product that created the calendar (RFC 5545 §3.7.3). |
| `Version` | `"VERSION"` | The iCalendar version (RFC 5545 §3.7.4). |
| `Attachment` | `"ATTACH"` | An attachment (RFC 5545 §3.8.1.1). |
| `Categories` | `"CATEGORIES"` | Categories (RFC 5545 §3.8.1.2). |
| `Classification` | `"CLASS"` | The access classification (RFC 5545 §3.8.1.3). |
| `Comment` | `"COMMENT"` | A comment (RFC 5545 §3.8.1.4). |
| `Description` | `"DESCRIPTION"` | A description (RFC 5545 §3.8.1.5). |
| `Geo` | `"GEO"` | The geographic position (RFC 5545 §3.8.1.6). |
| `Location` | `"LOCATION"` | The location (RFC 5545 §3.8.1.7). |
| `PercentComplete` | `"PERCENT-COMPLETE"` | The percentage of a to-do that is complete (RFC 5545 §3.8.1.8). |
| `Priority` | `"PRIORITY"` | The priority (RFC 5545 §3.8.1.9). |
| `Resources` | `"RESOURCES"` | Resources (RFC 5545 §3.8.1.10). |
| `Status` | `"STATUS"` | The status (RFC 5545 §3.8.1.11). |
| `Summary` | `"SUMMARY"` | The summary (RFC 5545 §3.8.1.12). |
| `Completed` | `"COMPLETED"` | The date and time a to-do was completed (RFC 5545 §3.8.2.1). |
| `DtEnd` | `"DTEND"` | The end (RFC 5545 §3.8.2.2). |
| `Due` | `"DUE"` | The due date (RFC 5545 §3.8.2.3). |
| `DtStart` | `"DTSTART"` | The start (RFC 5545 §3.8.2.4). |
| `Duration` | `"DURATION"` | The duration (RFC 5545 §3.8.2.5). |
| `FreeBusy` | `"FREEBUSY"` | Free or busy time (RFC 5545 §3.8.2.6). |
| `Transparency` | `"TRANSP"` | The time transparency (RFC 5545 §3.8.2.7). |
| `TimeZoneId` | `"TZID"` | The time zone identifier of a VTIMEZONE (RFC 5545 §3.8.3.1). |
| `TimeZoneName` | `"TZNAME"` | The customary name of a time zone observance (RFC 5545 §3.8.3.2). |
| `TimeZoneOffsetFrom` | `"TZOFFSETFROM"` | The UTC offset in use before an observance begins (RFC 5545 §3.8.3.3). |
| `TimeZoneOffsetTo` | `"TZOFFSETTO"` | The UTC offset in use during an observance (RFC 5545 §3.8.3.4). |
| `TimeZoneUrl` | `"TZURL"` | The location of a published time zone definition (RFC 5545 §3.8.3.5). |
| `Attendee` | `"ATTENDEE"` | An attendee (RFC 5545 §3.8.4.1). |
| `Contact` | `"CONTACT"` | Contact information (RFC 5545 §3.8.4.2). |
| `Organizer` | `"ORGANIZER"` | The organizer (RFC 5545 §3.8.4.3). |
| `RecurrenceId` | `"RECURRENCE-ID"` | The recurrence instance a component overrides (RFC 5545 §3.8.4.4). |
| `RelatedTo` | `"RELATED-TO"` | A relationship to another component (RFC 5545 §3.8.4.5). |
| `Url` | `"URL"` | The location of a more dynamic rendition (RFC 5545 §3.8.4.6). |
| `Uid` | `"UID"` | The persistent, globally unique identifier (RFC 5545 §3.8.4.7). |
| `ExceptionDates` | `"EXDATE"` | Exception dates (RFC 5545 §3.8.5.1). |
| `RecurrenceDates` | `"RDATE"` | Recurrence dates (RFC 5545 §3.8.5.2). |
| `RecurrenceRule` | `"RRULE"` | A recurrence rule (RFC 5545 §3.8.5.3). |
| `Action` | `"ACTION"` | The action of an alarm (RFC 5545 §3.8.6.1). |
| `Repeat` | `"REPEAT"` | The number of times an alarm repeats (RFC 5545 §3.8.6.2). |
| `Trigger` | `"TRIGGER"` | When an alarm triggers (RFC 5545 §3.8.6.3). |
| `Created` | `"CREATED"` | The date and time the information was created (RFC 5545 §3.8.7.1). |
| `DtStamp` | `"DTSTAMP"` | The date and time the iCalendar object was created or last revised (RFC 5545 §3.8.7.2). |
| `LastModified` | `"LAST-MODIFIED"` | The date and time the information was last revised (RFC 5545 §3.8.7.3). |
| `Sequence` | `"SEQUENCE"` | The revision sequence number (RFC 5545 §3.8.7.4). |
| `RequestStatus` | `"REQUEST-STATUS"` | A request status (RFC 5545 §3.8.8.3). |
| `Name` | `"NAME"` | The name of a calendar (RFC 7986 §5.1). |
| `RefreshInterval` | `"REFRESH-INTERVAL"` | The suggested minimum interval between polls for changes (RFC 7986 §5.7). |
| `Source` | `"SOURCE"` | The location from which a calendar can be refreshed (RFC 7986 §5.8). |
| `Color` | `"COLOR"` | A color (RFC 7986 §5.9). |
| `Image` | `"IMAGE"` | An image (RFC 7986 §5.10). |
| `Conference` | `"CONFERENCE"` | A conference system (RFC 7986 §5.11). |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [The layered model](../concepts/layered-model.md)
- [IcalComponent](IcalComponent.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalValueTypeRegistry](IcalValueTypeRegistry.md)
- [IcalComponentNames](IcalComponentNames.md)
- [IcalParameterNames](IcalParameterNames.md)
- [File.iCal API reference](index.md)
