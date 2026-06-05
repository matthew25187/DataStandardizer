---
title: FAQ
parent: Resources
nav_order: 1
---

# FAQ

Short answers to common questions about using the *Data Standardizer* packages,
with links to the pages that cover each topic in full.

## Which package do I need?

Pick the package for the standard you want to work with. Each *Data Standardizer*
package covers a specific set of internationally recognised standards — chronology,
communication, files, geography, languages, language tags, and money.

See [Standards coverage](../overview/standards-coverage.md) for the full
standard-to-package mapping, then head to that package's overview under
[Packages](../packages/index.md) for install instructions and guides.

## What platforms are supported?

Every package multi-targets modern .NET **and** .NET Standard, so it can be used
in new applications as well as older codebases that are being upgraded gradually
or must remain on older frameworks indefinitely.

The exact target frameworks per package — and the few framework-gated APIs — are
listed on [Platform support](../overview/platform-support.md).

## Why are some codes structs instead of enums?

Each standard is implemented as a strongly-typed code so that invalid values are
caught at compile time rather than becoming runtime bugs. Where a standard's codes
map cleanly onto a C# `enum`, an enum is used; where they don't (for example,
alphabetic codes that aren't valid C# identifiers), a struct-based `IStringEnum`
pattern is used instead.

The design rationale is covered in
[Strongly-typed codes](../concepts/strongly-typed-codes.md).

## Do I need to reference DataStandardizer.Core?

No. **DataStandardizer.Core** provides common types used internally to implement
the standards in the other packages. It is pulled in automatically as a
dependency, so you should not need to reference it directly.

See the [note on DataStandardizer.Core](../overview/standards-coverage.md) in
Standards coverage.

## How do I get the human-readable name or numeric code for a code?

The name, numeric code, and other attributes of a standard's codes are attached
to the code members and retrieved through lookup helpers and extension methods.

See [Metadata and lookups](../concepts/metadata-and-lookups.md) for the general
approach, and the relevant package's how-to guides — for example
[Access currency metadata](../packages/money/how-to/access-currency-metadata.md)
in Money.

## Can I contribute?

No. *Data Standardizer* is open source so you can **read, build, and adapt** it
for your own use, but the project does **not** currently accept third-party code
contributions.

The build and test instructions on [Build from source](build-from-source.md)
are provided purely as a convenience for working with your own copy or fork. If
the project adds value for you, the best way to help is to
[support the project](support-the-project.md).
