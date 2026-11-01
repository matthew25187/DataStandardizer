---
title: Packages
nav_order: 5
has_children: true
---

# Packages

Each package is themed around a kind of data you work with, and implements one or
more formal standards for it. Every package guide follows the same shape:

- **Overview** — what the package covers and which standards and platforms it
  supports.
- **How-to guides** — focused, task-oriented instructions.
- **Concepts** — the model behind the package, where it needs explaining.
- **Reference** — the curated public API surface.

## Available packages

| Package | Standards |
| --- | --- |
| [Chronology](chronology/index.md) | TZ Database · Unix time · DOS date & time |
| [Communication](communication/index.md) | ITU-T E.164 · MIME (RFC 2045 · RFC 2046 · RFC 2047 · RFC 2231 · RFC 2397 and others) |
| [File.CSV](file-csv/index.md) | RFC 4180 |
| [File.iCal](file-ical/index.md) | RFC 5545 · RFC 6868 · RFC 7986 · RFC 7265 · RFC 6321 |
| [Geography](geography/index.md) | ISO 3166-1 · ISO 3166-2 · UN M49 |
| [Language](language/index.md) | ISO 639 · ISO 15924 |
| [LanguageTag](language-tag/index.md) | BCP 47 |
| [Money](money/index.md) | ISO 4217 · Money type |

The internal **Core** package supplies shared building blocks used by the other
packages and is not intended to be referenced directly; see
[Strongly-typed codes](../concepts/index.md) for what it provides.
