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
| **DataStandardizer.File.CSV** | `netstandard1.3`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Geography** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Language** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.LanguageTag** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Money** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |
| **DataStandardizer.Core** | `netstandard1.0`, `netstandard2.0`, `net8.0`, `net10.0` |

## The general stance

Modern .NET (`net8.0` and `net10.0`) is the primary target, giving access to the
latest runtime features — including nullable reference types, which are enabled
on those builds. Alongside these, `netstandard2.0` covers the broad middle ground
of the .NET ecosystem, and `netstandard1.0` extends reach to the oldest
supported frameworks.

## Where the targets differ

Most packages start at `netstandard1.0`. The exception is
**DataStandardizer.File.CSV**, whose lowest target is `netstandard1.3` — the CSV
reader and writer rely on stream and component-model APIs that are not available
on `netstandard1.0`.

## Framework-gated APIs

A few APIs are available only on some targets. For example, on
**DataStandardizer.Money** the `Money` struct implements `IConvertible` on
.NET Standard 1.3+ and modern .NET, but not on the `netstandard1.0` build;
similarly, `Iso3166Part2Subdivision` in **DataStandardizer.Geography** implements
`IConvertible` on .NET Standard 1.3+ and modern .NET only.

Also on **DataStandardizer.Money**, `Money` implements `ISpanFormattable`,
`IParsable<Money>` and `ISpanParsable<Money>` on .NET 7.0 and later, which is the
earliest version defining all three. The `CurrencyFormatInfo.CurrentInfo` and `MoneyInfo.CurrentMoney`
properties require .NET Standard 2.0 or later, because the current culture is
unavailable on the `netstandard1.0` build; the invariant equivalents are available
on every target.

Where an API is framework-gated, the per-package reference pages carry an
**"Applies to"** note spelling out exactly which targets it covers.
