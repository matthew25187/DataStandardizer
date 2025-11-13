# Introduction 
*Data Standardizer* provides implementations of various internationally recognised standards in data processing, covering topics ranging from languages to currencies and geographical entities.  With strongly-typed enumerations for each standard (where applicable) or other targeted data types, you can represent these elements in your code such that errors with invalid values are minimised.

Supported target platforms include (modern) .Net and .Net Standard.  *Data Standardizer* can be used in modern application software, but is also available as an option for older codebases that are being upgraded more gradually or may remain on older frameworks indefinitely.

# Supporting the project
If you derive a commercial benefit from use of *Data Standardizer* or feel it otherwise adds value to your project, you are asked to please consider supporting the project.  You can do this by becoming a [GitHub sponsor](https://github.com/sponsors/matthew25187) to make a financial contribution.  *Data Standardizer* is maintained and enhanced by [@matthew25187](https://github.com/matthew25187) in his personal time and made available for free for all to use.

# Getting Started

## Installation
*Data Standardizer* is available as a series of packages from NuGet.org that can be linked to your existing projects.  Available packages include:

| Package | Description |
| --- | --- |
| **DataStandardizer.Chronology** | Provides support for the TZ Database. |
| **DataStandardizer.Core** | Common types used to implement standards in the other packages.  You should not need to link to this package directly. |
| **DataStandardizer.File.CSV** | Supports the following standards:<br>- RFC 4180, *Common Format and MIME Type for Comma-Separated Values (CSV) Files* |
| **DataStandardizer.Geography** | Supports the following standards:<br>- ISO 3166-1, *Codes for the representation of names of countries and their subdivisions – Part 1: Country code*<br>- ISO 3166-2, *Codes for the representation of names of countries and their subdivisions – Part 2: Country subdivision code*<br>- UN M49, *Standard Country or Area Codes for Statistical Use (Series M, No. 49)* |
| **DataStandardizer.Language** | Supports the following standards:<br>- ISO 639, *Code for the representation of names of languages*<br>    - *Part 1: Alpha-2 code*<br>    - *Part 2: Alpha-3 code*<br>    - *Part 3: Alpha-3 code for comprehensive coverage of languages*<br>    - *Part 5: Alpha-3 code for language families and groups*<br>- ISO 15924, *Codes for the representation of names of scripts* |
| **DataStandardizer.LanguageTag** | Supports the following standards:<br>- *Best Current Practice (BCP) 47* for IETF language tags |
| **DataStandardizer.Money** | Supports the following standards:<br>- ISO 4217, *Codes for the representation of currencies and funds*<br>    - Table A.1 – *Current currency & funds code list*<br>    - Table A.2 – *Current funds codes*<br>    - Table A.3 – *List of codes for historic denominations of currencies & funds*<br>- Money type, as described in *Patterns of Enterprise Application Architecture* by Martin Fowler |

To use a particular standard in your application, find the corresponding package from the above list and add it as a dependency to your project.  Instructions for doing so will depend on what development tooling you are using.

- **Visual Studio**: see [Install and manage packages in Visual Studio using the NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)
- **.Net CLI**: see [Install and manage NuGet packages with the dotnet CLI](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli)
- **Visual Studio Code**: see [NuGet in Visual Studio Code](https://code.visualstudio.com/docs/csharp/package-management)

## Software dependencies
Depending on which .Net platform you are targeting, the above packages will also depend on various other system- and third-party packages.  They will be included as static dependencies where required and should be automatically resolved, but if you are using a proxy for your package server you may need to make sure these other packages are also available.

The repository includes a number of *PowerShell* scripts with names starting with **Generate**.  These scripts are used to re-generate the enums that comprise the implementations of each corresponding standard and require the use of a *PowerShell* shell prompt to execute as well as access to the official flat-file data sources provided by the relevant standards body or designated maintainer.  Some scripts may also require a minimum version of *PowerShell* to run.

Other scripts and YAML files are included to support the infrastructure (IaC) used by the *Data Standardizer* project for functions such as pipelines, package hosting, etc.  These files are not intended to be used by the end-user.

## Latest releases
| Package | Release version | Release status |
| --- | --- | --- |
| **DataStandardizer.Chronology** | [![DataStandardizer.Chronology package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/8d12da01-040e-4624-8716-8855cf83ab93/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Chronology?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Core** | [![DataStandardizer.Core package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/b6a3e5a6-2d7a-447c-a155-1c0a086363fc/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Core?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.File.CSV** |  | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Geography** |  | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Language** |  | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.LanguageTag** |  | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Money** | [![DataStandardizer.Money package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/48e16c89-27a8-4e0d-9684-f2b2f00313ac/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Money?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |

The most recently produced release version (shown above) does not necessarily correspond with the latest package version published to NuGet or any other publically available source.

# Build and Test

## Branching strategy
The *Data Standardizer* repository makes use of two "main" branches.  They are:

| Name | Description |
| --- | --- |
| `master` | Top-level branch from which all package release builds are produced.  The `develop` branch will be merged into this branch when a new release is done. |
| `develop` | Default branch and the branch from which preview package builds are produced.  Changes are marshalled on this branch before being included in a release build. |

Other branches that may be created from time-to-time are not relevant to non-contributors.

## Build source code
To compile the source code, first you will need to clone the repository to your local machine.  You can find instructions for doing so [here](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository).

With the source code, you can then open a command prompt, change the current directory to the repository root folder, and use the following command to compile the entire solution:

    dotnet build DataStandardizer.sln

You can also work with the source code in IDEs such as *Visual Studio* or *Visual Studio Code*.  In these cases, open the DataStandardizer.sln solution file to access the source code.

There are also solution filter files (*.slnf) for each of the projects (packages) in the repository root folder alongside the main solution file.  These files narrow the scope of projects included to only those needed to build and test a single package.  You can also build these solution filters if so desired, and even open them in your IDE if you only want to work with the code for one package.  They are included mainly because they are used by the CI pipelines to enable the building and testing of each package individually.

## Running tests
The included tests are based on the XUnit test framework.  To run the tests, you will need a test runner able to work with XUnit.  The test projects do include a default test runner dependency, which enables you to run the tests from the command line.  With a command prompt open (as described above), you can run all tests in the solution:

    dotnet test DataStandardizer.sln

*Visual Studio* includes the *Test Explorer* that enables you to discover available tests and execute those tests by various categorizations.  Find out more about *Test Explorer* [here](https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer?view=vs-2022).  Testing is also supported in Visual Studio Code with use of the C# Dev Kit (learn more [here](https://code.visualstudio.com/docs/csharp/testing)).

# Usage

Though each package contains many types, typically there will be only a few that you will end up using directly in your application.  Please refer to the [project documentation](https://matthew25187.github.io/DataStandardizer/) for more information.  Includes articles on how to use the packages for specific tasks.

## 📄 CSV File Support

The `DataStandardizer.File.CSV` package includes built-in support for working with CSV (Comma-Separated Values) files — a common format for structured data exchange. This functionality is designed to be lightweight, flexible, and compatible with legacy .NET applications via support for .NET Standard 1.x and 2.0.

### ✅ What It Offers

- Read and write CSV files with customizable delimiters
- Normalize inconsistent CSV structures for downstream processing
- Handle headers, quoted fields, and edge cases gracefully
- Designed for extensibility and integration into broader data workflows

### 💡 Example: Reading and Normalizing a CSV File

        var inputPath = "data.csv";
        var outputPath = "normalized.csv";

        var csvInputOptions = new CsvFileOptions
        {
            TerminatorLineBreak = "\n"  // source file has non-standard line breaks
        };
        var csvOutputOptions = csvInputOptions with
        {
            TerminatorLineBreak = "\r\n", // write lines to the output file with standard line breaks
            QuoteHandling = CsvFieldQuoteHandling.Required  // quote field values only when needed
        };

        using (var input = File.OpenRead(inputPath))
        using (var csvReader = new CsvFileReader<CsvFileRecordLine>(input, csvInputOptions))
        using (var output = File.Create(outputPath))
        using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(output, csvOutputOptions))
        {
            var line = csvReader.ReadLine();
            while (line is not null)
            {
                csvWriter.WriteLine(line);

                line = csvReader.ReadLine();
            }
        }

### 📦 Where to Find It

CSV support is provided by the `DataStandardizer.File.CSV` NuGet package. You can install it via:

    dotnet add package DataStandardizer.File.CSV

For more advanced usage and configuration options, see the [project documentation](https://matthew25187.github.io/DataStandardizer/).