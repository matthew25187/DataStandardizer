---
title: IcalValueTypeRegistry Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalValueTypeRegistry Class

## Definition

Namespace: `DataStandardizer.File.iCal`

Gives the value type of each property defined by RFC 5545 and RFC 7986, and honours a
`VALUE` parameter that overrides it.

**Syntax**

```csharp
public static class IcalValueTypeRegistry
```

## Remarks

Unknown, experimental (`X-`) and IANA-registered properties have the value type
`IcalValueType.Unknown` whatever their `VALUE` parameter says. So their values are
kept as raw text and round-trip without loss. Property names and value type names are
compared case-insensitively, for ASCII letters.

A `VALUE` parameter that names a type RFC 5545 defines gives that type, even if the
property doesn't allow it. Use `IsAllowedValueType` to check. The members that take a
span don't allocate.

**Known properties.** The default type is the type of a value with no `VALUE`
parameter. RFC 7986 gives REFRESH-INTERVAL, SOURCE, IMAGE and CONFERENCE no default,
since they need a `VALUE` parameter; the registry gives their first allowed type as
the default.

| Property | Default type | Other allowed types |
| --- | --- | --- |
| `CALSCALE`, `METHOD`, `PRODID`, `VERSION` (RFC 5545 §3.7) | `Text` | |
| `ATTACH` | `Uri` | `Binary` |
| `CATEGORIES`, `CLASS`, `COMMENT`, `DESCRIPTION`, `LOCATION`, `RESOURCES`, `STATUS`, `SUMMARY` | `Text` | |
| `GEO` | `Float` | |
| `PERCENT-COMPLETE`, `PRIORITY` | `Integer` | |
| `COMPLETED` | `DateTime` | |
| `DTEND`, `DUE`, `DTSTART` | `DateTime` | `Date` |
| `DURATION` | `Duration` | |
| `FREEBUSY` | `Period` | |
| `TRANSP` | `Text` | |
| `TZID`, `TZNAME` | `Text` | |
| `TZOFFSETFROM`, `TZOFFSETTO` | `UtcOffset` | |
| `TZURL` | `Uri` | |
| `ATTENDEE`, `ORGANIZER` | `CalAddress` | |
| `CONTACT`, `RELATED-TO`, `UID` | `Text` | |
| `RECURRENCE-ID` | `DateTime` | `Date` |
| `URL` | `Uri` | |
| `EXDATE` | `DateTime` | `Date` |
| `RDATE` | `DateTime` | `Date`, `Period` |
| `RRULE` | `Recur` | |
| `ACTION` | `Text` | |
| `REPEAT` | `Integer` | |
| `TRIGGER` | `Duration` | `DateTime` |
| `CREATED`, `DTSTAMP`, `LAST-MODIFIED` | `DateTime` | |
| `SEQUENCE` | `Integer` | |
| `REQUEST-STATUS` | `Text` | |
| `NAME`, `COLOR` (RFC 7986) | `Text` | |
| `REFRESH-INTERVAL` (RFC 7986) | `Duration` | |
| `SOURCE`, `CONFERENCE` (RFC 7986) | `Uri` | |
| `IMAGE` (RFC 7986) | `Uri` | `Binary` |

## Methods

| Method | Returns | Notes |
| --- | --- | --- |
| `GetDefaultValueType(ReadOnlySpan<char> propertyName)` | `IcalValueType` | The type of the property's value when it has no `VALUE` parameter, or `Unknown` for a property whose value type isn't known. |
| `GetValueType(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> valueParameter)` | `IcalValueType` | The type a `VALUE` parameter, such as `DATE`, names for a property. Returns `Unknown` for a property whose value type isn't known, or a parameter that names no value type of RFC 5545. A known type is returned even if the property doesn't allow it. |
| `GetValueType(IcalContentLine line)` | `IcalValueType` | The value type of a content line, from its property name and the first value of any `VALUE` parameter. Returns `Unknown` as the overload above does. Throws `ArgumentNullException` if `line` is `null`. |
| `GetValueType(IcalContentLineView line)` | `IcalValueType` | The value type of the line a reader has just read, without allocation. Returns `Unknown` as the overloads above do. Throws `ObjectDisposedException` if the reader has been disposed, or `InvalidOperationException` if the reader has moved on to another line. |
| `GetValueTypeName(IcalValueType valueType)` | `string` | The name of a value type as a `VALUE` parameter writes it, such as `DATE-TIME`. Throws `ArgumentOutOfRangeException` for `Unknown`, which has no name, or for a value that isn't an `IcalValueType` member. |
| `IsAllowedValueType(ReadOnlySpan<char> propertyName, IcalValueType valueType)` | `bool` | `true` if the property is known and allows the type, as its default type or one a `VALUE` parameter may name. |
| `IsKnownProperty(ReadOnlySpan<char> propertyName)` | `bool` | `true` if the property is one whose value type is known: one listed above. |
| `TryParseValueTypeName(ReadOnlySpan<char> name, out IcalValueType valueType)` | `bool` | Parses the name of a value type, as given by a `VALUE` parameter, such as `DATE-TIME`. Returns `false`, with `valueType` set to `Unknown`, if the name isn't that of a value type of RFC 5545. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.

## See also

- [Standards compliance](../concepts/standards-compliance.md)
- [IcalValueType](IcalValueType.md)
- [IcalPropertyNames](IcalPropertyNames.md)
- [IcalContentLine](IcalContentLine.md)
- [IcalContentLineView](IcalContentLineView.md)
- [IcalValue](IcalValue.md)
- [File.iCal API reference](index.md)
