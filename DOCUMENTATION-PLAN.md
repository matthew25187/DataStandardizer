# Data Standardizer — Documentation Revamp Plan

> Status: **Proposed** · Author: docs revamp · Branch: `develop`
> This is a planning document. It is intentionally placed at the repository
> root (outside `/docs`) so that the GitHub Pages / Just-the-Docs build does
> **not** publish it. Delete it once the revamp is complete.

---

## 1. Goals

1. Give `/docs` a **dual structure**: task-oriented *user* documentation **and**
   lookup-oriented *reference* documentation, for every package.
2. Adopt the **Microsoft Learn content model** (Overview → Quickstart → Concept →
   How-to → Tutorial → Reference → Resources) so readers learn the navigation
   once and reuse it across all seven packages.
3. Render as a real documentation **site** (sidebar nav, search, breadcrumbs)
   from pure Markdown on GitHub Pages — no new CI, no DocFX, no Azure DevOps work.
4. **Consolidate** scattered content: lift shared material (install, platform
   support, concepts) out of per-package duplication, and migrate the
   operational content currently trapped in `README.md` into the site.

### Non-goals

- Auto-generated XML-comment API reference (DocFX). Explicitly out of scope per
  decision below — reference will be **curated by hand**.
- Changing any product code, namespaces, or the PowerShell generator scripts.

---

## 2. Tooling decisions (confirmed)

| Decision | Choice | Rationale |
| --- | --- | --- |
| Site generator / theme | **Just-the-Docs** (Jekyll theme) | Pure Markdown, sidebar nav + built-in client-side search + breadcrumbs, native to GitHub Pages. |
| Reference docs | **Curated, hand-written** + generated code-list tables | DocFX-on-Azure-DevOps deployment judged too heavy. Hand-written reference covers the *handful* of public types users actually touch; code lists are emitted as Markdown tables. |
| Hosting / build | **GitHub Pages built-in Jekyll build** | GitHub Pages compiles Jekyll automatically. The ADO pipelines remain scoped to NuGet packages and are untouched. |

### Why this sidesteps the DocFX/ADO concern entirely

The docs site and the package CI are **separate systems**:

- **Packages** → built/tested/published by the existing Azure DevOps pipelines.
- **Docs** → built by GitHub Pages' native Jekyll engine straight from the
  `/docs` folder. Just-the-Docs is consumed via `remote_theme`, which the Pages
  build supports out of the box. **No Gemfile build step, no DocFX, no ADO.**

The only one-time setup is adding `docs/_config.yml` and confirming the Pages
"Source" is the branch + `/docs` folder already in use.

---

## 3. The content model (Microsoft Learn taxonomy)

Each package is documented with the same set of **content types**. Not every
package needs every type; the matrix in §6 shows what each one gets.

| Type | Answers | Voice | Example title |
| --- | --- | --- | --- |
| **Overview** | "What is this package, what standards, what platforms?" | Descriptive | *DataStandardizer.Money overview* |
| **Quickstart** | "Get one thing working in <10 min" | Imperative, minimal | *Quickstart: your first currency code* |
| **Concept** | "How does it work / why is it designed this way?" | Explanatory | *Strongly-typed standard codes* |
| **How-to guide** | "Accomplish a specific task" | Imperative, task-titled | *Use currency codes* |
| **Tutorial** | "Learn by building something end-to-end" | Guided, narrative | *Normalize a non-standard CSV file* |
| **Reference** | "Look up exact types / code lists" | Terse, structured | *Money API reference*, *ISO 4217 code list* |
| **Resources** | FAQ, troubleshooting, glossary, contributing | Mixed | *Troubleshooting*, *FAQ* |

Article conventions (Learn style):
- Lead with a single sentence stating *what* and *why*.
- H1 = the task or topic ("Use timezones"), not the type.
- How-to pages stay focused on one task; concepts explain the *why*.
- Code blocks use fenced ```` ```csharp ```` (today some are 4-space indented
  and untyped — these get normalized during migration).
- **Docs describe the current state of the packages.** Where a feature is tied to
  a particular release, add a short **"Applies to"** note at the top of the
  article (à la Microsoft Learn) rather than maintaining versioned doc sets.

---

## 4. Just-the-Docs setup

### 4.1 Files to add

```
docs/
├── _config.yml          # theme + site config (NEW)
└── (every .md page gets YAML front matter for nav — see §4.2)
```

`docs/_config.yml` (illustrative):

```yaml
title: Data Standardizer
description: .NET packages for international data standards
remote_theme: just-the-docs/just-the-docs
url: https://matthew25187.github.io
baseurl: /DataStandardizer
search_enabled: true
aux_links:
  "GitHub": https://github.com/matthew25187/DataStandardizer
  "NuGet": https://www.nuget.org/profiles/matthew25187
color_scheme: light
nav_external_links:
  - title: Sponsor the project
    url: https://github.com/sponsors/matthew25187
```

### 4.2 Navigation model

Just-the-Docs builds the sidebar from page front matter, **not** a separate TOC
file:

- Top-level sections: `nav_order` + `has_children: true`.
- Child pages: `parent: "<section title>"` (+ `grand_parent` for 3rd level).

Example front matter for a how-to page:

```yaml
---
title: Use currency codes
parent: How-to guides
grand_parent: Money
nav_order: 1
---
```

Because every existing `.md` file needs front matter added anyway, this is folded
into the migration in §7.

### 4.3 Publishing checklist (one-time)

1. Repo **Settings → Pages**: Source is currently `develop` + `/docs`. Maintainer
   is considering re-pointing to **`master` + `/docs`** so published docs track
   NuGet releases (see §11.1). Either works with this plan; pick before go-live.
2. Merge `docs/_config.yml` (set `baseurl: /DataStandardizer` for the project-site
   path).
3. Verify build succeeds (Pages "Actions" run) and search box appears.
4. No change to `nuget.config`, `global.json`, `/build`, or the ADO pipelines.

---

## 5. Target information architecture

```
docs/
├── index.md                              # HUB landing page (cards + standards matrix)
├── _config.yml                           # NEW (theme/nav)
│
├── overview/                             # nav_order 1
│   ├── index.md                          # "What is Data Standardizer"
│   ├── standards-coverage.md             # matrix: standard ↔ package ↔ type
│   └── platform-support.md               # .NET / .NET Standard TFM matrix
│
├── get-started/                          # nav_order 2
│   ├── index.md
│   ├── install-a-package.md              # VS / dotnet CLI / VS Code
│   └── quickstart.md                     # first strongly-typed value, end-to-end
│
├── concepts/                             # nav_order 3  (cross-cutting)
│   ├── index.md
│   ├── why-standardize-data.md
│   ├── strongly-typed-codes.md           # the enum/struct-per-standard design
│   ├── metadata-and-lookups.md           # names, numeric codes, code attributes
│   └── data-currency-and-versioning.md   # how code lists are regenerated
│
├── packages/                             # nav_order 4  (one child section each)
│   ├── chronology/
│   ├── communication/
│   ├── file-csv/
│   ├── geography/
│   ├── language/
│   ├── language-tag/
│   └── money/
│
├── reference/                            # nav_order 5
│   ├── index.md
│   ├── code-lists/                       # generated Markdown tables
│   │   ├── iso3166-countries.md
│   │   ├── iso3166-2-subdivisions.md
│   │   ├── unm49-areas.md
│   │   ├── iso639-languages.md
│   │   ├── iso15924-scripts.md
│   │   ├── iso4217-currencies-current.md
│   │   └── iso4217-currencies-historic.md
│   └── glossary.md
│
└── resources/                            # nav_order 6
    ├── index.md
    ├── faq.md
    ├── troubleshooting.md
    ├── support-the-project.md            # sponsorship (from README)
    ├── build-from-source.md              # build/test your own copy (no 3rd-party contributions)
    └── version-history.md                # pointer to NuGet version history
```

### Per-package shape (identical for all seven)

```
packages/<pkg>/
├── index.md            # Overview: standards, features, supported platforms
├── quickstart.md       # (where useful) get one thing working fast
├── concepts/           # (where the model needs explaining — CSV, Money, BCP47)
│   └── <topic>.md
├── how-to/             # task pages (migrated from today's how-tos)
│   └── <task>.md
├── tutorial/           # (optional) end-to-end walkthrough
│   └── <name>.md
└── reference/          # curated API surface for the package
    └── api.md          # + links to relevant /reference/code-lists/*
```

---

## 6. Per-package breakdown

Reference pages are curated around the **public types users actually touch**
(enumerated from source). The large generated code-list enums/structs are **not**
documented type-by-type; they become reference **tables** under
`/reference/code-lists/`.

### 6.1 Chronology
- **Standards:** TZ Database, Unix time, DOS date & time.
- **Concepts:** `concepts/system-time-model.md` (the `ISystemTime` /
  `ISystemTimeWithDate` / `WithTime` / `WithDateTime` hierarchy +
  `SystemTimeWithGregorianCalendar`).
- **How-to (migrate):** use-timezones, access-timezone-metadata, use-unix-time,
  use-dos-datetime.
- **Reference:** `UnixTime`, `DosDateTime`, `TzDataTimezone` (+ nested region
  classes `Europe`/`Asia`/…), `SystemTimeWithGregorianCalendar`,
  `TzDataTimezoneAttribute`, and the `*Extensions` helpers
  (`TzDataExtensions`, `DateTimeExtensions`, `DateOnlyExtensions`,
  `TimeOnlyExtensions`, `SystemTimeExtensions`).

### 6.2 Communication
- **Standards:** ITU-T E.164.
- **Concept:** `concepts/e164-number-model.md` (international number broken into
  fields; the `IItuE164InternationalNumberFor*` role interfaces).
- **How-to (migrate):** use-international-numbers.
- **Reference:** `ItuE164InternationalNumber`, `ItuE164NationalSignificantNumber`,
  `ItuE164SubscriberNumber`, `ItuE164GlobalSubscriberNumber`, `TelephonyInfo`,
  `ItuE164InternationalNumberFormatInfo`, interfaces `ITelephonyNumber` /
  `IItuE164*`, `ItuE164SharedCodeAttribute`.

### 6.3 File.CSV
- **Standards:** RFC 4180.
- **Concepts:** `concepts/csv-line-model.md` (`ICsvFileLine`,
  `CsvFileRecordLine`, `CsvFileHeaderLine`, header handling),
  `concepts/field-mapping.md` (the fluent `CsvFileMappingBuilder` /
  `CsvFieldMapping*Builder` pipeline).
- **How-to (migrate):** prerequisites, configuration, read, write, map.
- **Tutorial (new, from README example):** `tutorial/normalize-a-csv.md`.
- **Reference:** `CsvFileReader<T>`, `CsvFileWriter<T>`, `CsvFileOptions`
  (record + `ICsvFileOptions`), `CsvFileRecordLine`, `CsvFileHeaderLine`,
  `CsvFieldAttribute`, `CsvFileMapper` / `CsvFileMappingBuilder<T>`,
  `CsvFileException`.

### 6.4 Geography
- **Standards:** ISO 3166-1, ISO 3166-2, UN M49.
- **How-to (migrate):** use-country-codes, use-subdivision-codes,
  access-country-and-subdivision-metadata, use-area-codes, access-area-metadata.
- **Reference:** `Iso3166Part2Enum`, the `Iso3166Subdivision*Attribute` family,
  `UnM49AreaCodeAttribute`, `UnM49Extensions`; code-list tables for countries,
  subdivisions, and M49 areas.

### 6.5 Language
- **Standards:** ISO 639 (Parts 1/2/3/5), ISO 15924.
- **Concept:** `concepts/iso639-parts.md` (Alpha-2 vs Alpha-3 T/B vs Part 3 vs
  Part 5 families; the `IStringEnum` struct pattern).
- **How-to (migrate):** use-language-codes, access-language-metadata,
  use-script-codes, access-script-metadata.
- **Reference:** `Iso639Part1/2T/2B/3/5` (+ `*Language` / `*Family` variants as a
  table, not member-by-member), `Iso15924*`, `Iso639Extensions`,
  `Iso15924Extensions`, attributes; code-list tables for languages & scripts.

### 6.6 LanguageTag
- **Standards:** BCP 47.
- **Concepts:** `concepts/language-tag-anatomy.md` (subtags),
  `concepts/builder-pipeline.md` (the step-interface fluent builder).
- **How-to (migrate):** use-language-tags, create-language-tags-using-builder.
- **Reference:** `Bcp47LanguageTag`, `Bcp47LanguageTagBuilder` (+ the
  `IBcp47LanguageTagBuilderStep*` interface chain summarized as a flow diagram),
  `Bcp47KeyedSubtag`, `SubtagRegistry` + records, `LanguageTagFormatException`.

### 6.7 Money
- **Standards:** ISO 4217 (Tables A.1–A.3), Fowler's Money type.
- **Concept:** `concepts/money-type.md` (value + currency).
- **How-to (migrate):** use-money-datatype, use-currency-codes,
  access-currency-metadata.
- **Reference:** `Money`, `Iso4217Extensions`, `Iso4217CurrencyCodeAttribute`;
  code-list tables for current & historic currencies.
- **Deferred — out of scope:** `MoneyFormatter` is a work in progress and is
  **not** to be documented yet. Coverage (formatting concept + reference) will be
  added by the maintainer once that type is finished. Do not reference it in any
  page during this revamp.

### Core
- Not user-facing. A single note under `concepts/strongly-typed-codes.md`
  explaining `IStringEnum` / `StringEnum` / `CodeAttributeBase` is enough; no
  package section.

---

## 7. Migration map (existing → new)

All existing how-tos move under `packages/<pkg>/how-to/` and gain front matter.
Filenames are kept where sensible.

| Existing | New location |
| --- | --- |
| `docs/index.md` | `docs/index.md` (rewritten as hub) |
| `…/chronology-package-guide/guide-home.md` | `packages/chronology/index.md` |
| `…/chronology…/tzdatabase-standard/use-timezones.md` | `packages/chronology/how-to/use-timezones.md` |
| `…/chronology…/tzdatabase-standard/access-timezone-metadata.md` | `packages/chronology/how-to/access-timezone-metadata.md` |
| `…/chronology…/unixtime-standard/use-unix-time.md` | `packages/chronology/how-to/use-unix-time.md` |
| `…/chronology…/dosdatetime-standard/use-dos-datetime.md` | `packages/chronology/how-to/use-dos-datetime.md` |
| `…/communication-package-guide/guide-home.md` | `packages/communication/index.md` |
| `…/communication…/e164-standard/use-international-numbers.md` | `packages/communication/how-to/use-international-numbers.md` |
| `…/file-csv-package-guide/guide-home.md` | `packages/file-csv/index.md` |
| `…/file-csv…/rfc4180-standard/csv-prerequisites.md` | `packages/file-csv/how-to/prerequisites.md` |
| `…/file-csv…/rfc4180-standard/csv-configuration.md` | `packages/file-csv/how-to/configure-csv.md` |
| `…/file-csv…/rfc4180-standard/read-csv-files.md` | `packages/file-csv/how-to/read-csv-files.md` |
| `…/file-csv…/rfc4180-standard/write-csv-files.md` | `packages/file-csv/how-to/write-csv-files.md` |
| `…/file-csv…/rfc4180-standard/map-csv-files.md` | `packages/file-csv/how-to/map-csv-files.md` |
| `…/geography-package-guide/guide-home.md` | `packages/geography/index.md` |
| `…/geography…/iso3166-standard/*.md` | `packages/geography/how-to/*.md` |
| `…/geography…/unm49-standard/*.md` | `packages/geography/how-to/*.md` |
| `…/language-package-guide/guide-home.md` | `packages/language/index.md` |
| `…/language…/iso639-standard/*.md` | `packages/language/how-to/*.md` |
| `…/language…/iso15924-standard/*.md` | `packages/language/how-to/*.md` |
| `…/languagetag-package-guide/guide-home.md` | `packages/language-tag/index.md` |
| `…/languagetag…/bcp47-standard/*.md` | `packages/language-tag/how-to/*.md` |
| `…/money-package-guide/guide-home.md` | `packages/money/index.md` |
| `…/money…/use-money-datatype.md` | `packages/money/how-to/use-money-datatype.md` |
| `…/money…/iso4217-standard/*.md` | `packages/money/how-to/*.md` |

> The old `user-guides/` tree and the `*-standard/` sub-folders are removed.
> The standard a how-to relates to is conveyed by the page intro and the package
> overview, not by folder depth — this matches the existing flat Learn pattern
> and shortens URLs.

### README.md changes

- Keep README focused on the **GitHub/NuGet audience**: intro, badges, install
  table, a short "Documentation →" pointer to the Pages site.
- Move **Build & Test**, **Branching strategy**, and the **CSV deep-dive** into
  `resources/build-from-source.md` and `packages/file-csv/` respectively (README
  keeps a one-paragraph teaser + link). The build/test page is framed as working
  with your **own copy** of the repo, **not** an invitation to contribute (see
  §11.6).
- Move **Supporting the project** detail into `resources/support-the-project.md`
  (README keeps the badge/one-liner).

---

## 8. New pages to author (net-new content)

| Page | Source material |
| --- | --- |
| `index.md` (hub) | Rewrite of current index + package cards + standards matrix |
| `overview/index.md` | README "Introduction" |
| `overview/standards-coverage.md` | README install table, re-pivoted |
| `overview/platform-support.md` | README ".NET / .NET Standard" notes + `global.json` |
| `get-started/install-a-package.md` | README install section (VS/CLI/VS Code links) |
| `get-started/quickstart.md` | New — smallest meaningful example |
| `concepts/*` (4 pages) | New — design rationale, `IStringEnum`, metadata, regen |
| `packages/*/concepts/*` | New per §6 (CSV, Money, BCP47, E.164, ISO 639, system-time) |
| `packages/file-csv/tutorial/normalize-a-csv.md` | README CSV example, expanded |
| `packages/*/reference/api.md` | New curated reference per §6 |
| `reference/code-lists/*` | Generated tables (see §9) |
| `reference/glossary.md` | New — standards & domain terms |
| `resources/faq.md`, `troubleshooting.md` | New |
| `resources/build-from-source.md` | README Build/Test/Branching, framed as building your **own copy/fork** — states the project does **not** accept third-party contributions (+ a brief, advanced pointer to the existing `Generate*.ps1` enum-refresh scripts — documented, not modified) |
| `resources/version-history.md` | Thin page pointing to NuGet version history (no in-repo release notes — see §11) |

---

## 9. Generating the code-list reference tables

### 9.1 Why reflection works here (and not for API docs)

There are two different kinds of "documentation data," and only one of them is
reflectable from a compiled assembly:

- **XML `<summary>`/`<remarks>` doc comments** — **not** available via reflection
  at runtime. The compiler must emit a `.xml` doc file and you post-process that
  artifact (per the MSDN Magazine article, Oct 2019). This is precisely why API
  reference is **hand-written** (§2), so this limitation never applies.
- **Code-list data** (code ↔ numeric ↔ name) — this is **not** doc comments. It
  is enum/struct members plus the values carried by the `*CodeAttribute` types
  (`Iso4217CurrencyCodeAttribute`, `Iso3166SubdivisionNameAttribute`,
  `UnM49AreaCodeAttribute`, `Iso639LanguageCodeAttribute`, …). Ordinary runtime
  reflection — enumerate members, read custom attributes — produces it fully. **No
  `.xml` doc file required.**

### 9.2 Approach: a separate, internal, maintainer-run generator

**Not a CI / release-pipeline step.** Pages serves static committed Markdown from
`master/docs`, so pipeline generation would mean CI committing `.md` back into the
repo — fragile, and the same kind of pipeline-coupled dynamic artifact that has
made release notes unreliable. Doc generation is *purely internal*, so it stays
out of the package pipelines entirely.

- A **standalone console tool** (or a *new* PowerShell script) — **kept entirely
  separate from the existing `scripts/Generate*.ps1`, which are NOT touched.**
  Those scripts have a dual internal/external audience (refreshing enums outside
  the release cadence); the doc-table generator is internal-only and must not be
  entangled with them.
- It reflects over the **built assemblies'** members + `*CodeAttribute` metadata
  and emits Markdown tables into `docs/reference/code-lists/`.
- The maintainer runs it **on demand**, only when a standard's data actually
  changes (rare), and commits the resulting static `.md` files.

### 9.3 Coverage: generate the tractable lists, link out for the huge ones

| List | Approx. size | Treatment |
| --- | --- | --- |
| ISO 4217 currencies (current + historic) | ~180 + historic | Full generated table |
| ISO 3166-1 countries | ~249 | Full generated table |
| ISO 3166-2 subdivisions | ~5,000 | Generated, but split per country / consider link-out |
| ISO 15924 scripts | ~180 | Full generated table |
| UN M49 areas | ~290 | Full generated table |
| ISO 639-1/2 | ~180 / ~480 | Full generated table |
| **ISO 639-3 languages** | **~7,900** | **Short example + link to the official registry** — a full table is low-value noise |

These tables are **build artifacts of data already in the repo**, never
hand-maintained.

---

## 10. Phased implementation

Each phase is independently shippable (the site keeps working throughout).

| Phase | Scope | Outcome |
| --- | --- | --- |
| **0. Scaffold** | Add `_config.yml` (`baseurl: /DataStandardizer`), add hub `index.md` + section landing pages with front matter | Themed site with sidebar + search live |
| **1. Migrate** | Move existing how-tos into `packages/*/how-to/`, add front matter, fix code fences to ```csharp | No content lost; new nav in place |
| **2. Lift shared** | Author `overview/`, `get-started/`, `concepts/`; trim README | README/site no longer duplicate |
| **3. Per-package depth** | Package overviews, package concepts, CSV tutorial | Each package fully shaped |
| **4. Reference** | Curated `reference/api.md` per package + generated code-list tables | Lookup docs complete |
| **5. Resources** | FAQ, troubleshooting, glossary, contributing, support | Polished |
| **6. Cleanup** | Remove old `user-guides/` tree, delete this plan | Done |

---

## 11. Resolved decisions

1. **Pages source branch.** Currently `develop/docs`. Maintainer is leaning toward
   re-pointing Pages to **`master/docs`** so the published docs match the
   NuGet-released packages (preview packages are rarely published straight from
   `develop`). **Implication for this work:** docs are authored on the working
   branch as usual; they go live when merged to whichever branch Pages serves.
   The plan is branch-agnostic. If the switch to `master` happens, it reinforces
   decision #5 (docs reflect released state). *Action item owned by maintainer; no
   blocker for docs authoring.*
2. **Old URLs — no redirects.** Existing article URLs may break; unlikely anyone
   has bookmarked them. The old `user-guides/` tree is simply removed.
3. **Existing PS generator scripts — do not touch.** They serve a dual
   internal/external audience (refreshing enums outside the release cadence / RC
   prep) and are out of scope. Any code-list table generation uses a **separate,
   internal-only** tool (§9.2). The build-from-source page may *reference* the
   scripts but must not modify them.
4. **Release notes — not documented in-repo.** They are pipeline-generated into
   the NuGet `ReleaseNotes` package metadata, don't exist in the repo or on
   GitHub, and are currently unreliable (a separate issue to fix). Docs add only a
   thin `resources/version-history.md` pointing to the **NuGet version history**;
   no attempt to reconstruct a release history.
5. **Versioning — single "latest" docs set.** Documentation reflects the **current
   state of the packages** (more so once Pages points to `master`). Release-tied
   functionality is handled per-article with an **"Applies to"** note (§3), not
   versioned doc sets.
6. **No third-party contributions.** The project is open source for reading,
   building, and adapting, but does **not** accept external code contributions
   (no capacity to manage a contributor community). Therefore the docs avoid the
   word "contributing": build/test guidance lives at
   `resources/build-from-source.md`, framed as working with your **own copy/fork**
   and stating the no-contributions policy explicitly.

---

## 12. API reference architecture (per-type) — supersedes §6/§9/§5 reference plans

The single curated `reference/api.md` per package is replaced with **one document
per public type**, modelled on Microsoft Learn API pages.

### 12.1 Per-type page template

H1 = `{TypeName} {Kind}` — e.g. "Money Struct", "Iso4217CurrencyCurrent Enum",
"Iso4217Extensions Class", "ICsvFileLine Interface". The Kind suffix appears
**only** in the page title (H1); index-table Type columns and See-also links use
the **bare type name**. Then, omitting any empty section:

- **## Definition** — a `Namespace:` line, a prose description, then **Syntax** as
  a pure code block (declaration only; no inline prose, no trailing comments).
  Where the declaration differs by target framework, give one **labelled** block
  per variant: a bold framework label line (e.g. `**.NET Standard 1.0:**`) above
  each `csharp` block. See `packages/money/reference/Money.md` for a worked
  example.
- **## Remarks** — pertinent non-descriptive, non-syntax information.
- Member-list sections, each H2, included only if non-empty: **## Constructors**,
  **## Fields**, **## Properties**, **## Methods**, **## Operators**.
  - Member lists are sorted **alphabetically by member name**.
  - On a type's own page the member tables **omit a declaring-type column**
    (redundant — every member belongs to that type). Inherited members, if listed,
    are marked explicitly. The extension-class page keeps an **Extends** column
    (§12.2).
  - **Methods** uses H3 sub-sections by category only: `### Explicit
    implementation`, `### Implicit implementation`, `### Extension`. Explicit
    interface implementations are **enumerated in full**; per-member framework
    availability is shown with an italicised prefix (e.g. *(netstandard2.0+/.NET)*).
  - **Operators** is its own H2 (conversion + overloaded operators).
  - Indexers are listed under **Properties**.
- **## Applies to** — target frameworks and any version-specific notes.
- **## See also** — related pages.

Public types and members only. Anything that doesn't fit these sections is raised
with the maintainer before refactoring that document.

### 12.2 Extension methods

Documented in full on their declaring extension-class page (a public static type,
e.g. `Iso4217Extensions`) under `## Methods → ### Extension`. The *extended*
type's page lists the applicable extensions by name with a link — no duplicated
detail.

### 12.3 Code enums/structs = the code list

Each code enum (or `IStringEnum` struct) is its own type page, and its **Fields**
section IS the code list — the full member table (field · numeric value · name ·
metadata), generated from the regular generated enum source (the reflection-based
generator of §9 can formalise this). This **absorbs** the former separate
"code lists" idea (dropped from §5); the top-level **Reference** section reduces to
the Glossary.

Tractable sets (ISO 4217 ~180/~134, ISO 3166-1, ISO 15924) render as a single
Fields table. **Very large or nested sets split into sub-pages, grouped sensibly:**
- ISO 639-3 (~8,000): by initial-letter ranges (A–F, G–L, …).
- ISO 3166-2 subdivisions (~5,000, nested): per country.
- TZ Database timezones (nested): per region.

The enum's main page carries Definition/Remarks/Applies to/See also and links to
the grouped Fields sub-pages.

### 12.4 Folded / relocated content

- The ~15 BCP 47 builder step interfaces (`IBcp47LanguageTagBuilderStep…`) are
  **folded** into the `Bcp47LanguageTagBuilder` page and the builder-pipeline
  concept — no per-interface pages.
- Cross-type narrative (interface hierarchies, the builder pipeline, field-type
  overviews) lives in the **concept** pages or the relevant type's **Remarks**,
  not in per-type reference.

### 12.5 Navigation

Each package keeps `packages/<pkg>/reference/`, containing:
- `index.md` — the nav-visible **"API reference"** page, grouping the package's
  types (Structures / Classes / Enumerations / Interfaces / Delegates /
  Exceptions) with links.
- one page per type, `nav_exclude: true` — kept out of the sidebar; reached via the
  index, cross-links, and search. Front matter `parent: <Package>`,
  `grand_parent: Packages`.

Stays within Just-the-Docs' 3-level nav.

### 12.6 Status — COMPLETE

All seven packages are converted to the per-type structure (336 reference pages).
Large code lists are generated from the regular enum/struct source:
- Money: ISO 4217 current (178) + historic (134).
- Communication: E.164 geographic-area country codes (251) + network identification codes (53).
- Chronology: `TzDataTimezone` split into 9 per-region sub-pages (~312 zones).
- Geography: ISO 3166-1 alpha-2/alpha-3 (249 each), UN M49 alpha-2/alpha-3 (248 each),
  and `Iso3166Part2Subdivision` split into 200 per-country sub-pages (5,046 subdivisions).
- Language: ISO 15924 scripts (226), ISO 639 parts 1/2B/2T/5 (183/486/486/115),
  and `Iso639Part3Language` split into 4 letter-range sub-pages (~7,900 codes).

The top-level **Reference** section is now the Glossary plus pointers; the separate
"code lists" concept is absorbed into the enum/struct Fields pages.
