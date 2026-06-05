---
title: Glossary
parent: Reference
nav_order: 2
---

# Glossary

Definitions of the standards and domain terms used throughout this documentation.

## Standards

**TZ Database**
: The community-maintained database of named time zones (e.g. `Europe/Berlin`),
also known as the IANA time-zone database or *tz*. Modelled by
[Chronology](../packages/chronology/index.md).

**Unix time**
: A point in time expressed as the number of seconds since the Unix epoch
(1 January 1970 UTC). Modelled by [Chronology](../packages/chronology/index.md).

**DOS date/time**
: A packed date-and-time representation as used by the MS-DOS file system,
covering the years 1980–2107. Modelled by [Chronology](../packages/chronology/index.md).

**ITU-T E.164**
: The ITU-T recommendation defining the international public telecommunication
numbering plan — the structure of international telephone numbers. Modelled by
[Communication](../packages/communication/index.md).

**RFC 4180**
: The specification of the common format and MIME type for Comma-Separated Values
(CSV) files. Implemented by [File.CSV](../packages/file-csv/index.md).

**ISO 3166-1**
: Codes for the representation of names of countries — Part 1: Country code.
Modelled by [Geography](../packages/geography/index.md).

**ISO 3166-2**
: Codes for the representation of names of countries and their subdivisions —
Part 2: Country subdivision code. Modelled by [Geography](../packages/geography/index.md).

**UN M49**
: The United Nations *Standard Country or Area Codes for Statistical Use* (Series
M, No. 49), assigning numeric codes to countries, areas, and regions. Modelled by
[Geography](../packages/geography/index.md).

**ISO 639**
: Codes for the representation of names of languages, published in several parts.
Modelled by [Language](../packages/language/index.md).

**ISO 639 Part 1**
: The alpha-2 (two-letter) language code, e.g. `en` for English.

**ISO 639 Part 2**
: The alpha-3 (three-letter) language code, published as a terminology (T) code
and a bibliographic (B) code.

**ISO 639 Part 3**
: An alpha-3 code providing comprehensive coverage of languages, extending beyond
the major languages covered by Parts 1 and 2.

**ISO 639 Part 5**
: An alpha-3 code for language families and groups.

**ISO 15924**
: Codes for the representation of names of scripts (writing systems), e.g. `Hans`
for Simplified Chinese script. Modelled by [Language](../packages/language/index.md).

**BCP 47**
: *Best Current Practice 47*, the IETF specification for language tags (as defined
by RFC 5646), used to identify languages in a hyphen-separated sequence of
subtags. Modelled by [LanguageTag](../packages/language-tag/index.md).

**ISO 4217**
: Codes for the representation of currencies and funds — current currency and
funds codes (Tables A.1–A.2) and historic denominations (Table A.3). Modelled by
[Money](../packages/money/index.md).

## Domain terms

**Money type**
: A value type that combines an amount with an ISO 4217 currency code, after
Martin Fowler's *Patterns of Enterprise Application Architecture*. See
[The Money type](../packages/money/concepts/money-type.md).

**Subtag**
: One hyphen-delimited part of a BCP 47 language tag — for example the primary
language, script, or region — each occupying a fixed position and carrying a
distinct meaning. See
[Anatomy of a language tag](../packages/language-tag/concepts/language-tag-anatomy.md).

**Code list**
: The full set of codes defined by a standard (for example, every ISO 4217
currency or ISO 3166-1 country), together with their numeric codes and names. The
[Reference](./index.md) section publishes these as generated tables.

**Strongly-typed code**
: A code from a standard represented as a distinct .NET type — an `enum` where the
codes map cleanly onto one, or a struct-based `IStringEnum` where they don't — so
that invalid values are caught at compile time rather than at runtime. See
[Strongly-typed codes](../concepts/strongly-typed-codes.md).
