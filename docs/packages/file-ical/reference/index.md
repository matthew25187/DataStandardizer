---
title: API reference
parent: File.iCal
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.File.iCal API reference

The public types of **DataStandardizer.File.iCal**. All types are in the
`DataStandardizer.File.iCal` namespace.

## Classes

| Type | Description |
| --- | --- |
| [IcalAlarm](IcalAlarm.md) | An alarm (VALARM): typed access to a reminder's action, trigger and repeat properties. |
| [IcalCalendar](IcalCalendar.md) | An iCalendar object (VCALENDAR): calendar properties, including RFC 7986's, and its events, to-dos, journals, free/busy and time zones. |
| [IcalComponent](IcalComponent.md) | The base component: a name, ordered properties and nested components, with property lookup and validation. |
| [IcalContentLine](IcalContentLine.md) | A content line (RFC 5545 §3.1): property name, parameters and value. |
| [IcalContentLineParameter](IcalContentLineParameter.md) | A parameter of a content line, with its decoded values. |
| [IcalContentLineReader](IcalContentLineReader.md) | Reads the unfolded content lines of iCalendar data, with zero-copy and async options. |
| [IcalContentLineWriter](IcalContentLineWriter.md) | Writes content lines, folded and with RFC 6868 parameter encoding. |
| [IcalDiagnostic](IcalDiagnostic.md) | A conformance problem found by `IcalComponent.Validate`. |
| [IcalEvent](IcalEvent.md) | An event (VEVENT): typed properties and expansion of its recurrence set into occurrences. |
| [IcalFreeBusy](IcalFreeBusy.md) | Free or busy time information (VFREEBUSY): typed properties and its FREEBUSY periods. |
| [IcalJournal](IcalJournal.md) | A journal entry (VJOURNAL): typed properties and expansion of its recurrence set into occurrences. |
| [IcalOccurrence](IcalOccurrence.md) | One instance of a recurrence set: its local and UTC start and end, and the component (possibly an override) that describes it. |
| [IcalOptions](IcalOptions.md) | Options for readers, writers and recurrence expansion; RFC 5545 defaults. |
| [IcalReader](IcalReader.md) | Reads .ics data into the component object model. |
| [IcalTimeZone](IcalTimeZone.md) | The VTIMEZONE component; `FromTzData` builds one from the TZ Database over a period you choose. |
| [IcalTimeZoneObservance](IcalTimeZoneObservance.md) | The STANDARD or DAYLIGHT observance of a VTIMEZONE. |
| [IcalTimeZoneRules](IcalTimeZoneRules.md) | The resolved rules of a time zone: offsets, transitions, and local-to-UTC conversion per RFC 5545 §3.3.5. |
| [IcalTodo](IcalTodo.md) | A to-do (VTODO): typed properties and expansion of its recurrence set into occurrences. |
| [IcalWriter](IcalWriter.md) | Writes the component object model as .ics data. |
| [JcalReader](JcalReader.md) | Reads jCal (RFC 7265, JSON) data into the component object model. |
| [JcalWriter](JcalWriter.md) | Writes the component object model as jCal (RFC 7265, JSON) data. |
| [XcalReader](XcalReader.md) | Reads xCal (RFC 6321, XML) data into the component object model. |
| [XcalWriter](XcalWriter.md) | Writes the component object model as xCal (RFC 6321, XML) data. |

## Static classes

| Type | Description |
| --- | --- |
| [IcalComponentNames](IcalComponentNames.md) | The names of the components that RFC 5545 defines. |
| [IcalParameterNames](IcalParameterNames.md) | The names of the property parameters that RFC 5545 and RFC 7986 define. |
| [IcalPropertyNames](IcalPropertyNames.md) | The names of the properties that RFC 5545 and RFC 7986 define. |
| [IcalTimeZoneResolver](IcalTimeZoneResolver.md) | The Calendar, TzData and System resolvers, `Chain`, and the `Resolve` and `ToUtc` extension methods. |
| [IcalValue](IcalValue.md) | Parses and formats the BINARY, BOOLEAN, CAL-ADDRESS, FLOAT, INTEGER, TEXT and URI value types. |
| [IcalValueTypeRegistry](IcalValueTypeRegistry.md) | Gives the value type of each known property, honouring a `VALUE` parameter. |

## Structs

| Type | Description |
| --- | --- |
| [IcalContentLineView](IcalContentLineView.md) | A zero-allocation view of the line an `IcalContentLineReader` has just read. |
| [IcalDate](IcalDate.md) | A DATE value (RFC 5545 §3.3.4): a calendar date with no time of day. |
| [IcalDateOrDateTime](IcalDateOrDateTime.md) | A property value that is either a DATE or a DATE-TIME, such as DTSTART. |
| [IcalDateTime](IcalDateTime.md) | A DATE-TIME value (RFC 5545 §3.3.5): floating, in UTC, or in a named time zone. |
| [IcalDuration](IcalDuration.md) | A DURATION value (RFC 5545 §3.3.6): nominal weeks or days and an exact time. |
| [IcalPeriod](IcalPeriod.md) | A PERIOD value (RFC 5545 §3.3.9): a start and an end or a positive duration. |
| [IcalRecurrenceDay](IcalRecurrenceDay.md) | A BYDAY value: a day of the week with an optional ordinal, such as `-1FR`. |
| [IcalRecurrenceRule](IcalRecurrenceRule.md) | A RECUR value (RFC 5545 §3.3.10): parses, builds, formats and expands a recurrence rule. |
| [IcalTextListEnumerator](IcalTextListEnumerator.md) | Enumerates the items of a list of TEXT values without allocating. |
| [IcalTime](IcalTime.md) | A TIME value (RFC 5545 §3.3.12): a time of day, floating or in UTC. |
| [IcalTimeZoneTransition](IcalTimeZoneTransition.md) | A change in a time zone's UTC offset: the onset of an observance. |
| [IcalUtcOffset](IcalUtcOffset.md) | A UTC-OFFSET value (RFC 5545 §3.3.14): the difference of a local time from UTC. |
| [IcalWriteProgress](IcalWriteProgress.md) | The cumulative progress a writer reports from its bulk write methods. |

## Exceptions

| Type | Description |
| --- | --- |
| [IcalFileException](IcalFileException.md) | Thrown for iCalendar data that cannot be read or written; details in `Data`. |

## Interfaces

| Type | Description |
| --- | --- |
| [IIcalTimeZoneResolver](IIcalTimeZoneResolver.md) | Resolves a `TZID` to the rules of the time zone. |

## Enumerations

| Type | Description |
| --- | --- |
| [IcalDateTimeKind](IcalDateTimeKind.md) | The forms of a DATE-TIME value: floating, UTC or zoned. |
| [IcalDiagnosticKind](IcalDiagnosticKind.md) | The kinds of conformance problem, and which options accept them. |
| [IcalRecurrenceFrequency](IcalRecurrenceFrequency.md) | The FREQ rule part of a recurrence rule, from SECONDLY to YEARLY. |
| [IcalTimeZoneObservanceKind](IcalTimeZoneObservanceKind.md) | Standard or daylight saving time (STANDARD or DAYLIGHT). |
| [IcalValueFormat](IcalValueFormat.md) | The lexical form a value is parsed or formatted in: `.ics` text, jCal or xCal. |
| [IcalValueType](IcalValueType.md) | The value types of RFC 5545 §3.3, named by the `VALUE` parameter. |
