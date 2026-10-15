---
title: Platform support
parent: Overview
nav_order: 2
---

# Platform support

The target frameworks (TFMs) each *Data Standardizer* package builds for.

Every package multi-targets modern .NET **and** .NET Standard, so it can be
adopted in new applications as well as older codebases that are being upgraded
gradually or must remain on older frameworks indefinitely.

| Package | Target frameworks |
| --- | --- |
| **DataStandardizer.Chronology** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Communication** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.File.CSV** | `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, `net8.0`, `net10.0` |
| **DataStandardizer.File.iCal** | `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, `net10.0` |
| **DataStandardizer.Geography** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Language** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.LanguageTag** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Money** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Core** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |

## The general stance

Modern .NET (`net8.0` and `net10.0`) is the primary target, giving access to the
latest runtime features — including nullable reference types, which are enabled
on those builds and on the `netstandard2.1` builds of **DataStandardizer.File.CSV**
and **DataStandardizer.File.iCal**.
Alongside these, `netstandard2.0` covers the broad middle ground of the .NET
ecosystem, and `netstandard1.0` extends reach to the oldest supported frameworks.

## Where the targets differ

Most packages start at `netstandard1.0`. The exceptions are the two file packages.
**DataStandardizer.File.CSV**'s lowest target is `netstandard1.3` — the CSV
reader and writer rely on stream and component-model APIs that are not available
on `netstandard1.0`. **DataStandardizer.File.iCal**'s lowest target is
`netstandard1.1`, because the `System.Memory` package it uses for its span-based
parsing starts at `netstandard1.1`. The supported platforms for .NET Standard 1.0
and 1.1 are the same, so this costs no reach.

The two file packages are also the only ones with a `netstandard2.1` target,
which gives .NET Standard 2.1 platforms (Mono and Unity, for example) the in-box
span types and span-based BCL overloads with no extra package dependencies. The
`netstandard1.3` and `netstandard2.0` builds of **DataStandardizer.File.CSV** get
those span types from the `System.Memory` package instead, which NuGet installs
automatically; the `netstandard2.1`, `net8.0` and `net10.0` builds have no extra
dependency. Likewise, the `netstandard1.1` and `netstandard2.0` builds of
**DataStandardizer.File.iCal** get the span types from `System.Memory`, and the
`netstandard2.0` build gets `IAsyncEnumerable<T>` and `IAsyncDisposable` from
`Microsoft.Bcl.AsyncInterfaces`.

## Framework-gated APIs

A few APIs are available only on some targets. For example, on
**DataStandardizer.Money** the `Money` struct implements `IConvertible` on
.NET Standard 1.3+ and modern .NET, but not on the `netstandard1.0` build;
similarly, `Iso3166Part2Subdivision` in **DataStandardizer.Geography** implements
`IConvertible` on .NET Standard 1.3+ and modern .NET only.

The `netstandard1.1` build of **DataStandardizer.File.iCal** is the most limited:

- **No async API.** The async members, such as `IcalReader.ReadCalendarsAsync`,
  `IcalWriter.WriteCalendarsAsync` and `DisposeAsync`, exist on `netstandard2.0`
  and later only. On `netstandard1.1`, read and write synchronously.
- **No jCal.** jCal (the JSON form of iCalendar, RFC 7265) needs
  System.Text.Json, so `JcalReader` and `JcalWriter` are not available.
  xCal (`XcalReader` and `XcalWriter`) uses the in-box `XmlReader` and
  `XmlWriter`, so it is available on every target.
- **No file-path constructors.** Construct readers and writers from a `Stream`,
  `TextReader` or `TextWriter` instead.
- **No `System` time-zone resolver.** `IcalTimeZoneResolver.System`, which uses
  `TimeZoneInfo`, exists on `netstandard2.0` and later only.

For jCal, the `netstandard2.0` and `netstandard2.1` builds get
System.Text.Json from its NuGet package, which NuGet installs automatically; on
`net8.0` and `net10.0` it is part of the framework. The jCal lexical form of
single values, `IcalValueFormat.Json`, needs no JSON library, so the value types
support it on every target, `netstandard1.1` included.

Where an API is framework-gated, the per-package reference pages carry an
**"Applies to"** note spelling out exactly which targets it covers.

## CPU architectures

The packages contain architecture-neutral (AnyCPU) IL and run on x86, x64, ARM64
and any other architecture the consuming .NET runtime supports.
