---
title: IcalComponent Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalComponent Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A component of iCalendar data (RFC 5545 §3.6): a name, an ordered list of
properties, and the components nested in it. It is the base type of the typed
components, such as [IcalCalendar](IcalCalendar.md) and [IcalEvent](IcalEvent.md),
and the type a reader creates for a component it does not recognise, such as an
`X-` component.

**Syntax**

```csharp
public class IcalComponent
```

## Remarks

**Properties are content lines.** Each property is held as an
[IcalContentLine](IcalContentLine.md), so its parameters stay reachable. Properties
that have no typed accessor, including unknown and `X-` properties, are kept and
written back as they were read. Property and component names are compared
case-insensitively.

**Typed accessors.** The derived types add typed properties, such as
`IcalEvent.Summary` or `IcalEvent.DtStart`, that read from and write to
`Properties`. They all follow the same rules:

- A getter reads the first property of its name, and returns `null` if there is
  none.
- A setter replaces every property of its name with one property, in the place of
  the first one it replaces, or at the end if there was none. It keeps the
  parameters of the first property, except those the new value decides: `VALUE`
  and `TZID` for a date or time, `VALUE` for a duration, and `VALUE` and
  `ENCODING` for a URI. The `RecurrenceRules` setter and `IcalAlarm.TriggerDateTime`
  keep no parameters.
- Setting `null` removes every property of that name.
- A list accessor, such as `Categories` or `RecurrenceRules`, reads every property
  of its name. Setting an empty list removes them all, as does `null`. A TEXT list,
  such as `Categories`, is written as one property holding the whole list. An item
  that is `null` throws `ArgumentException`.
- TEXT escaping (RFC 5545 §3.3.11) is removed by the getter and added by the
  setter.
- A DATE or DATE-TIME setter writes `VALUE=DATE` for a date, and a `TZID`
  parameter for a date-time in a named time zone.
- A URI setter takes an absolute URI only, and throws `ArgumentException` for a
  relative one.
- The getter does not check the value until it is read. A value that is not valid
  for the property, such as `PRIORITY:high` or `SUMMARY:\x`, makes the getter throw
  [IcalFileException](IcalFileException.md). The exception carries the line number
  of the property, when it was read from a source. Expect this for a model built in
  code from content lines, or one read with `IcalOptions.AllowInvalidValues`
  turned on.
- Some properties have no setter and return the content lines themselves, such as
  `Attendees`, `Images` and `Conferences`. Their parameters, such as `CN` or
  `FMTTYPE`, carry most of their meaning. Change them through `Properties`.

**Validity.** The typed setters do not check the rules of RFC 5545 that span more
than one property, such as whether a component has its required properties. Use
`Validate` to check a model built in code.

**Thread safety.** A component has no locking. Don't change it while another
thread reads it.

**Deriving.** The class is not sealed and has a public constructor, but the
helpers behind the typed accessors are internal. The typed components of the
package are all sealed.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalComponent(string name)` | Creates an empty component with the given name, such as `X-ABC-PROJECT`. The name must be an iana-token or x-name (RFC 5545 §3.1): one or more letters, digits and hyphens. Throws `ArgumentNullException` if `name` is `null`, or `ArgumentException` if it is empty or holds any other character. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Components` | `IList<IcalComponent> Components { get; }` | The components nested in this one, in order. Adding or setting `null` throws `ArgumentNullException`. |
| `LineNumber` | `long LineNumber { get; }` | The 1-based number of the logical line holding the component's `BEGIN`. For a component read by [XcalReader](XcalReader.md), the line of the XML document on which its element starts. 0 if the component was not read from a source. |
| `Name` | `string Name { get; }` | The component name, such as `VEVENT`. |
| `Properties` | `IList<IcalContentLine> Properties { get; }` | The properties, in order. Adding or setting `null` throws `ArgumentNullException`. |

## Methods

### Properties by name

All names are compared case-insensitively.

| Method | Returns | Notes |
| --- | --- | --- |
| `GetProperty(string name)` | `IcalContentLine?` | The first property with the name, or `null` if there is none. Throws `ArgumentNullException` if `name` is `null`. |
| `GetProperties(string name)` | `IReadOnlyList<IcalContentLine>` | Every property with the name, in order, as a new list. Empty if there are none. Throws `ArgumentNullException` if `name` is `null`. |
| `SetProperty(IcalContentLine property)` | `void` | Replaces every property with the same name as `property` with `property`. It takes the place of the first one it replaces, or is added at the end if there was none. Throws `ArgumentNullException` if `property` is `null`. |
| `RemoveProperties(string name)` | `int` | Removes every property with the name, and returns how many were removed. Throws `ArgumentNullException` if `name` is `null`. |

### Other methods

| Method | Returns | Notes |
| --- | --- | --- |
| `Validate()` | `IReadOnlyList<IcalDiagnostic>` | Checks the component, and the components nested in it, against the rules of RFC 5545 and RFC 7986 that [IcalReader](IcalReader.md) and [IcalWriter](IcalWriter.md) enforce: required properties and components, how often each property may appear, the syntax of property values, where each component may be nested, and the `VERSION` of a calendar. Whether a `TZID` parameter names a VTIMEZONE, and whether a VEVENT needs a `DTSTART`, are checked only when this is an [IcalCalendar](IcalCalendar.md). Returns one [IcalDiagnostic](IcalDiagnostic.md) per problem, in the order of the lines they were found on, or an empty list. Never throws for a problem, whatever the options. A component path in a diagnostic starts with this component's name, such as `VCALENDAR/VEVENT[2]`. |
| `ToString()` | `string` | The name and the number of properties and components, such as `VEVENT (5 properties, 1 components)`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable reference annotations, such as the `IcalContentLine?` return of
`GetProperty`, apply to the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [The layered model](../concepts/layered-model.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalEvent](IcalEvent.md)
- [IcalTodo](IcalTodo.md)
- [IcalJournal](IcalJournal.md)
- [IcalFreeBusy](IcalFreeBusy.md)
- [IcalAlarm](IcalAlarm.md)
- [IcalTimeZone](IcalTimeZone.md)
- [IcalTimeZoneObservance](IcalTimeZoneObservance.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalDiagnostic](IcalDiagnostic.md)
- [IcalComponentNames](IcalComponentNames.md)
- [IcalPropertyNames](IcalPropertyNames.md)
- [File.iCal API reference](index.md)
