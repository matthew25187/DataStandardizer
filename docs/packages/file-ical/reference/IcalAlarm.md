---
title: IcalAlarm Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalAlarm Class

## Definition

Namespace: `DataStandardizer.File.iCal`

An alarm (RFC 5545 §3.6.6): the VALARM component, nested in an
[IcalEvent](IcalEvent.md) or [IcalTodo](IcalTodo.md). It gives typed access to the
properties of a reminder.

**Syntax**

```csharp
public sealed class IcalAlarm : IcalComponent
```

## Remarks

The typed properties follow the rules described under *Typed accessors* on
[IcalComponent](IcalComponent.md): a getter reads the first property of its name, a
setter replaces every property of its name in the place of the first, setting
`null` removes them, and a getter throws [IcalFileException](IcalFileException.md)
if the stored value is not valid.

**Triggers.** A `TRIGGER` (RFC 5545 §3.8.6.3) is either relative, a DURATION, or
absolute, a DATE-TIME in UTC with `VALUE=DATE-TIME`. `TriggerDuration` and
`TriggerDateTime` each read only their own kind, and return `null` for the other:

- Setting `TriggerDuration` replaces an absolute trigger. It keeps the parameters
  of an existing relative trigger, such as `RELATED=END`, which makes the duration
  relative to the end of the event or to-do instead of its start.
- Setting `TriggerDateTime` removes any trigger and adds a new `TRIGGER` with the
  `VALUE=DATE-TIME` parameter that RFC 5545 requires, at the end of `Properties`.
  It keeps no other parameters.
- Setting either to `null` removes the trigger, whichever kind it is.

To nest an alarm, add it to the `Components` of the event or to-do. Read the alarms
back through `IcalEvent.Alarms` or `IcalTodo.Alarms`.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalAlarm()` | Creates an empty VALARM component, with no properties. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Action` | `string? Action { get; set; }` | `ACTION` (RFC 5545 §3.8.6.1), such as `AUDIO`, `DISPLAY` or `EMAIL`. TEXT. Throws `IcalFileException` on get if the value has a backslash that does not start a valid escape. `null` removes it. |
| `Attachments` | `IReadOnlyList<IcalContentLine> Attachments { get; }` | `ATTACH` (RFC 5545 §3.8.1.1), as the content lines themselves. Change them through `Properties`. |
| `Attendees` | `IReadOnlyList<IcalContentLine> Attendees { get; }` | `ATTENDEE` (RFC 5545 §3.8.4.1), as the content lines themselves: the recipients of an `EMAIL` alarm. Change them through `Properties`. |
| `Description` | `string? Description { get; set; }` | `DESCRIPTION` (RFC 5545 §3.8.1.5). TEXT. `null` removes it. |
| `Duration` | `IcalDuration? Duration { get; set; }` | `DURATION` (RFC 5545 §3.8.2.5), the delay between repeats. Throws `IcalFileException` on get if the value is not a valid DURATION. `null` removes it. |
| `Repeat` | `int? Repeat { get; set; }` | `REPEAT` (RFC 5545 §3.8.6.2), the number of times the alarm repeats after it is first triggered. Throws `IcalFileException` on get if the value is not a valid INTEGER. `null` removes it. |
| `Summary` | `string? Summary { get; set; }` | `SUMMARY` (RFC 5545 §3.8.1.12), the subject of an `EMAIL` alarm. TEXT. `null` removes it. |
| `TriggerDateTime` | `IcalDateTime? TriggerDateTime { get; set; }` | An absolute `TRIGGER` (RFC 5545 §3.8.6.3), a DATE-TIME. `null` if there is no trigger, or it is relative. Throws `IcalFileException` on get if the value is not a valid DATE-TIME. See *Triggers*. |
| `TriggerDuration` | `IcalDuration? TriggerDuration { get; set; }` | A relative `TRIGGER` (RFC 5545 §3.8.6.3), a DURATION. `null` if there is no trigger, or it is absolute. Throws `IcalFileException` on get if the value is not a valid DURATION. See *Triggers*. |

TEXT properties remove TEXT escaping on get and add it on set.

Inherited members `Components`, `LineNumber`, `Name`, `Properties`, `GetProperty`,
`GetProperties`, `SetProperty`, `RemoveProperties`, `Validate` and `ToString` are
documented on [IcalComponent](IcalComponent.md).

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations apply to the `netstandard2.1`, `net8.0`, and
`net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalComponent](IcalComponent.md)
- [IcalEvent](IcalEvent.md)
- [IcalTodo](IcalTodo.md)
- [IcalDuration](IcalDuration.md)
- [IcalDateTime](IcalDateTime.md)
- [File.iCal API reference](index.md)
