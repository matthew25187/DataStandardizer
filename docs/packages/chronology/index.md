---
title: Chronology
parent: Packages
nav_order: 1
has_children: true
---

# DataStandardizer.Chronology

Strongly-typed support for chronology-related data standards: named time zones,
Unix time, and DOS date & time.

```shell
dotnet add package DataStandardizer.Chronology
```

## Standards

| Standard | What it provides |
| --- | --- |
| **TZ Database** | Standardised, named time zones (e.g. `Europe/Berlin`) with associated metadata. |
| **Unix time** | A point in time as seconds since the Unix epoch (1 January 1970 UTC). |
| **DOS date & time** | A packed date/time as used by the MS-DOS file system (1980–2107). |

## Platform support

Targets .NET Standard 1.x and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes.

## How-to guides

- [Use time zones](how-to/use-timezones.md)
- [Access time zone metadata](how-to/access-timezone-metadata.md)
- [Use Unix time](how-to/use-unix-time.md)
- [Use DOS date/time](how-to/use-dos-datetime.md)

## Concepts

- [The system-time model](concepts/system-time-model.md)

## Reference

- [API reference](reference/index.md)
