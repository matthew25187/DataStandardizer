# Introduction 
*Data Standardizer* provides implementations of various internationally recognised standards in data processing, covering topics ranging from languages to currencies and geographical entities.  With strongly-typed enumerations for each standard (where applicable) or other targeted data types, you can represent these elements in your code such that errors with invalid values are minimised.

Supported target platforms include (modern) .Net and .Net Standard.  *Data Standardizer* can be used in modern application software, but is also available as an option for older codebases that are being upgraded more gradually or may remain on older frameworks indefinitely.

## 📚 Documentation
Full documentation — user guides, concepts, and API reference for every package — is published at **[matthew25187.github.io/DataStandardizer](https://matthew25187.github.io/DataStandardizer/)**.

# Supporting the project
If you derive a commercial benefit from use of *Data Standardizer* or feel it otherwise adds value to your project, you are asked to please consider supporting the project.  You can do this by becoming a [GitHub sponsor](https://github.com/sponsors/matthew25187) to make a financial contribution.  *Data Standardizer* is maintained and enhanced by [@matthew25187](https://github.com/matthew25187) in his personal time and made available for free for all to use.

# Getting Started

## Installation
*Data Standardizer* is available as a series of packages from NuGet.org that can be linked to your existing projects.  Available packages include:

| Package | Description |
| --- | --- |
| **DataStandardizer.Chronology** | Supports the following standards:<ul><li>TZ Database</li><li>Unix time</li><li>DOS date & time</li></ul> |
| **DataStandardizer.Communication** | Supports the following standards:<ul><li>Recommendation ITU-T E.164, *The international public telecommunication numbering plan*</li></ul> |
| **DataStandardizer.Core** | Common types used to implement standards in the other packages.  You should not need to link to this package directly. |
| **DataStandardizer.File.CSV** | Supports the following standards:<ul><li>RFC 4180, *Common Format and MIME Type for Comma-Separated Values (CSV) Files*</li></ul> |
| **DataStandardizer.File.iCal** | Supports the following standards:<ul><li>RFC 5545, *Internet Calendaring and Scheduling Core Object Specification (iCalendar)*</li><li>RFC 6868, *Parameter Value Encoding in iCalendar and vCard*</li><li>RFC 7986, *New Properties for iCalendar*</li><li>RFC 7265, *jCal: The JSON Format for iCalendar*</li><li>RFC 6321, *xCal: The XML Format for iCalendar*</li></ul> |
| **DataStandardizer.Geography** | Supports the following standards:<ul><li>ISO 3166-1, *Codes for the representation of names of countries and their subdivisions – Part 1: Country code*</li><li>ISO 3166-2, *Codes for the representation of names of countries and their subdivisions – Part 2: Country subdivision code*</li><li>UN M49, *Standard Country or Area Codes for Statistical Use (Series M, No. 49)*</li></ul> |
| **DataStandardizer.Language** | Supports the following standards:<ul><li>ISO 639, *Code for the representation of names of languages*</li><ul><li>*Part 1: Alpha-2 code*</li><li>*Part 2: Alpha-3 code*</li><li>*Part 3: Alpha-3 code for comprehensive coverage of languages*</li><li>*Part 5: Alpha-3 code for language families and groups*</li></ul><li>ISO 15924, *Codes for the representation of names of scripts*</li></ul> |
| **DataStandardizer.LanguageTag** | Supports the following standards:<ul><li>IETF BCP 47 language tags, as defined by:<ul><li>RFC 5646, *Tags for Identifying Languages*, including the IANA Language Subtag Registry format</li></ul></li></ul> |
| **DataStandardizer.Money** | Supports the following standards:<ul><li>ISO 4217, *Codes for the representation of currencies and funds*</li><ul><li>Table A.1 – *Current currency & funds code list*</li><li>Table A.2 – *Current funds codes*</li><li>Table A.3 – *List of codes for historic denominations of currencies & funds*</li></ul><li>Money type, as described in *Patterns of Enterprise Application Architecture* by Martin Fowler</li></ul> |

To use a particular standard in your application, find the corresponding package from the above list and add it as a dependency to your project.  Instructions for doing so will depend on what development tooling you are using.

- **Visual Studio**: see [Install and manage packages in Visual Studio using the NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)
- **.Net CLI**: see [Install and manage NuGet packages with the dotnet CLI](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli)
- **Visual Studio Code**: see [NuGet in Visual Studio Code](https://code.visualstudio.com/docs/csharp/package-management)

See the [Get started](https://matthew25187.github.io/DataStandardizer/get-started/) guide for step-by-step installation and a quickstart.

## Software dependencies
Depending on which .Net platform you are targeting, the above packages will also depend on various other system- and third-party packages.  They will be included as static dependencies where required and should be automatically resolved, but if you are using a proxy for your package server you may need to make sure these other packages are also available.  For example, the `netstandard1.1` and `netstandard2.0` builds of **DataStandardizer.LanguageTag** depend on `System.Memory` for their span-based API.

## Latest releases
| Package | Promoted version | nuget.org | Release build |
| --- | --- | --- | --- |
| **DataStandardizer.Chronology** | [![DataStandardizer.Chronology promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.Chronology&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.Chronology&expanded=true) | [![DataStandardizer.Chronology on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.Chronology?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.Chronology) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.Communication** | [![DataStandardizer.Communication promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.Communication&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.Communication&expanded=true) | [![DataStandardizer.Communication on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.Communication?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.Communication) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.Core** | [![DataStandardizer.Core promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.Core&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.Core&expanded=true) | [![DataStandardizer.Core on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.Core?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.Core) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.File.CSV** | [![DataStandardizer.File.CSV promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.File.CSV&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.File.CSV&expanded=true) | [![DataStandardizer.File.CSV on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.File.CSV?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.File.CSV) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.File.iCal** | [![DataStandardizer.File.iCal promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.File.iCal&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.File.iCal&expanded=true) | [![DataStandardizer.File.iCal on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.File.iCal?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.File.iCal) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.Geography** | [![DataStandardizer.Geography promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.Geography&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.Geography&expanded=true) | [![DataStandardizer.Geography on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.Geography?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.Geography) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.Language** | [![DataStandardizer.Language promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.Language&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.Language&expanded=true) | [![DataStandardizer.Language on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.Language?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.Language) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.LanguageTag** | [![DataStandardizer.LanguageTag promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.LanguageTag&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.LanguageTag&expanded=true) | [![DataStandardizer.LanguageTag on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.LanguageTag?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.LanguageTag) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |
| **DataStandardizer.Money** | [![DataStandardizer.Money promoted version](https://img.shields.io/github/v/release/matthew25187/DataStandardizer?filter=*-DataStandardizer.Money&label=promoted)](https://github.com/matthew25187/DataStandardizer/releases?q=DataStandardizer.Money&expanded=true) | [![DataStandardizer.Money on nuget.org](https://img.shields.io/nuget/v/DataStandardizer.Money?label=nuget.org)](https://www.nuget.org/packages/DataStandardizer.Money) | [![Release build](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml/badge.svg?branch=master)](https://github.com/matthew25187/DataStandardizer/actions/workflows/release.yml?query=branch%3Amaster) |

The promoted version is the latest release promoted from this repository's builds (a GitHub Release). It does not necessarily correspond with the latest package version published to NuGet or any other publically available source.

# Build and test

> *Data Standardizer* is open source so you can read, build, and adapt it for your own use. The project does **not** currently accept third-party code contributions; the instructions below are provided as a convenience for working with your own copy or fork of the repository.

After cloning the repository, you can compile the entire solution from the repository root:

    dotnet build DataStandardizer.sln

and run the XUnit-based tests with:

    dotnet test DataStandardizer.sln

Per-package solution filter (`*.slnf`) files let you build or test a single package in isolation.  For the full branching strategy, per-package builds, IDE testing, and the PowerShell scripts that regenerate the standard enums, see [Build from source](https://matthew25187.github.io/DataStandardizer/resources/build-from-source.html) in the documentation.

# Usage

Though each package contains many types, typically there are only a few you will use directly in your application.  See the [project documentation](https://matthew25187.github.io/DataStandardizer/) for user guides, concepts, and API reference covering each package — including built-in CSV reading, writing, and normalization in `DataStandardizer.File.CSV`, and iCalendar reading, writing, and recurrence expansion in `DataStandardizer.File.iCal`.
