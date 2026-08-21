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
| **DataStandardizer.Geography** | Supports the following standards:<ul><li>ISO 3166-1, *Codes for the representation of names of countries and their subdivisions – Part 1: Country code*</li><li>ISO 3166-2, *Codes for the representation of names of countries and their subdivisions – Part 2: Country subdivision code*</li><li>UN M49, *Standard Country or Area Codes for Statistical Use (Series M, No. 49)*</li></ul> |
| **DataStandardizer.Language** | Supports the following standards:<ul><li>ISO 639, *Code for the representation of names of languages*</li><ul><li>*Part 1: Alpha-2 code*</li><li>*Part 2: Alpha-3 code*</li><li>*Part 3: Alpha-3 code for comprehensive coverage of languages*</li><li>*Part 5: Alpha-3 code for language families and groups*</li></ul><li>ISO 15924, *Codes for the representation of names of scripts*</li></ul> |
| **DataStandardizer.LanguageTag** | Supports the following standards:<ul><li>*Best Current Practice (BCP) 47* for IETF language tags</li></ul> |
| **DataStandardizer.Money** | Supports the following standards:<ul><li>ISO 4217, *Codes for the representation of currencies and funds*</li><ul><li>Table A.1 – *Current currency & funds code list*</li><li>Table A.2 – *Current funds codes*</li><li>Table A.3 – *List of codes for historic denominations of currencies & funds*</li></ul><li>Money type, as described in *Patterns of Enterprise Application Architecture* by Martin Fowler</li></ul> |

To use a particular standard in your application, find the corresponding package from the above list and add it as a dependency to your project.  Instructions for doing so will depend on what development tooling you are using.

- **Visual Studio**: see [Install and manage packages in Visual Studio using the NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)
- **.Net CLI**: see [Install and manage NuGet packages with the dotnet CLI](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli)
- **Visual Studio Code**: see [NuGet in Visual Studio Code](https://code.visualstudio.com/docs/csharp/package-management)

See the [Get started](https://matthew25187.github.io/DataStandardizer/get-started/) guide for step-by-step installation and a quickstart.

## Software dependencies
Depending on which .Net platform you are targeting, the above packages will also depend on various other system- and third-party packages.  They will be included as static dependencies where required and should be automatically resolved, but if you are using a proxy for your package server you may need to make sure these other packages are also available.

## Latest releases
| Package | Release version | Release status |
| --- | --- | --- |
| **DataStandardizer.Chronology** | [![DataStandardizer.Chronology package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/8d12da01-040e-4624-8716-8855cf83ab93/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Chronology?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Communication** | [![DataStandardizer.Communication package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/ee0c3009-57af-4fe1-a590-246ab6cacdaf/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Communication?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Core** | [![DataStandardizer.Core package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/b6a3e5a6-2d7a-447c-a155-1c0a086363fc/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Core?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.File.CSV** | [![DataStandardizer.File.CSV package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/45cee3fe-3f84-4b98-86a3-6d9cbf2ecd20/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.File.CSV?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Geography** | [![DataStandardizer.Geography package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/37441333-f617-487d-968f-528a5ad00e63/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Geography?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Language** | [![DataStandardizer.Language package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/a8cb2370-178b-4a22-b34e-a402a6778b43/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Language?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.LanguageTag** | [![DataStandardizer.LanguageTag package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/672cb78f-c877-4f72-94e8-2eb0a7c0a897/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.LanguageTag?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Money** | [![DataStandardizer.Money package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/48e16c89-27a8-4e0d-9684-f2b2f00313ac/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Money?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |

The most recently produced release version (shown above) does not necessarily correspond with the latest package version published to NuGet or any other publically available source.

# Build and test

> *Data Standardizer* is open source so you can read, build, and adapt it for your own use. The project does **not** currently accept third-party code contributions; the instructions below are provided as a convenience for working with your own copy or fork of the repository.

After cloning the repository, you can compile the entire solution from the repository root:

    dotnet build DataStandardizer.sln

and run the XUnit-based tests with:

    dotnet test DataStandardizer.sln

Per-package solution filter (`*.slnf`) files let you build or test a single package in isolation.  For the full branching strategy, per-package builds, IDE testing, and the PowerShell scripts that regenerate the standard enums, see [Build from source](https://matthew25187.github.io/DataStandardizer/resources/build-from-source.html) in the documentation.

# Usage

Though each package contains many types, typically there are only a few you will use directly in your application.  See the [project documentation](https://matthew25187.github.io/DataStandardizer/) for user guides, concepts, and API reference covering each package — including built-in CSV reading, writing, and normalization in `DataStandardizer.File.CSV`.
