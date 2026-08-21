---
title: Data currency and versioning
parent: Concepts
nav_order: 4
---

# Data currency and versioning

Standards bodies revise their code lists over time — currencies are added or
retired, subdivisions change, languages are reclassified — so Data Standardizer
keeps its enums and structs in step by *generating* them from the official
flat-file data sources rather than maintaining them by hand.

## Generated, not hand-written

The code members you use are not typed out one by one. The repository ships a set
of PowerShell scripts, named with a `Generate` prefix, that read an official data
file for a standard and emit the corresponding C# type. Each script's header
names the standard it implements and the data source it draws from. A few
representative examples:

```text
script                                          standard / source
─────────────────────────────────────────────  ─────────────────────────────────
GenerateISO4217CurrentCurrencyAndFunds…  .ps1   ISO 4217 currencies (SIX Group)
GenerateISO639Part3Language…             .ps1   ISO 639 Part 3 (SIL Global)
GenerateISO15924Script…                  .ps1   ISO 15924 scripts (Unicode, Inc.)
GenerateISO3166Part2Subdivision…         .ps1   ISO 3166-2 subdivisions
GenerateUNM49Area…                       .ps1   UN M49 areas
```

The full set lives in the repository's `scripts/` folder and covers every
standard the library implements, each pointing at the relevant standards body or
designated maintainer's published list. A shared `StringEnumCodeGen` PowerShell
module under `scripts/` provides the common code-generation support these scripts
build on.

> Because generation is mechanical, the in-code values mirror the published data
> directly, which keeps human transcription errors out of the code lists.

## Who runs them, and when

These scripts are a maintenance tool, not part of consuming the packages. The
maintainer runs them to refresh the code lists when preparing a release, so each
published version reflects the source data as of that release. They are also
available to anyone working with their own copy of the repository: if you need
data that is newer than the latest release — for example a currency change that
has not yet been folded into an official version — you can run the relevant
script against the current source file to regenerate the type locally, outside
the official release cadence.

Running a generator requires a PowerShell prompt and access to the official
flat-file data source for the standard; some scripts also require a minimum
PowerShell version. (This page describes what the scripts are *for*; it is not a
guide to changing them.)

## What the documentation reflects

This documentation describes the **current released state** of each package. Code
lists move with the standards, and an API surface can also differ by target
framework, so where a detail is framework- or version-specific the per-package
pages carry an **"Applies to"** note spelling out exactly which targets or
conditions it covers. If you have regenerated a code list locally from newer
source data, your copy may contain entries that a published release — and these
docs — do not yet mention.

## Why this design

- **Accuracy.** Types are generated straight from the authoritative data, so the
  codes match the standard rather than a hand-kept copy of it.
- **Repeatability.** Refreshing a standard is re-running a script, not editing
  hundreds of members by hand.
- **Predictable releases.** Generation happens at release preparation, so a
  published version corresponds to a known snapshot of the source data.
- **Escape hatch.** Anyone with the repository can regenerate locally when they
  need data ahead of the official cadence.

## Related

- [Strongly-typed codes](strongly-typed-codes.md)
- [Metadata and lookups](metadata-and-lookups.md)
- [Platform support](../overview/platform-support.md)
