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
| **DataStandardizer.BCP47** | Supports **IETF BCP 47** language tags. |
| **DataStandardizer.Chronology** | Provides support for the TZ Database. |
| **DataStandardizer.Core** | Common types used to implement standards in the other packages.  You should not need to link to this package directly. |
| **DataStandardizer.File** | Provides implementations of standards-based file formats. |
| **DataStandardizer.ISO15924** | Supports **ISO 15924, *Codes for the representation of names of scripts***. |
| **DataStandardizer.ISO3166** | Supports **ISO 3166, *Codes for the representation of names of countries and their subdivisions*** parts 1 & 2. |
| **DataStandardizer.ISO4217** | Supports **ISO 4217, *Codes for the representation of currencies and funds***. |
| **DataStandardizer.ISO639** | Supports **ISO 639, *Codes for the representation of names of languages*** parts 1, 2, 3 & 5. |
| **DataStandardizer.Money** | Provides types for the handling of monetary values. |
| **DataStandardizer.UNM49** | Supports **UN M49** or the **Standard Country or Area Codes for Statistical Use (Series M, No. 49)**. |

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
| **DataStandardizer.BCP47** | [![DataStandardizer.BCP47 package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/27ae72fe-bacf-4dc1-a866-f3ef7d304d26/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.BCP47?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Chronology** | [![DataStandardizer.Chronology package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/8d12da01-040e-4624-8716-8855cf83ab93/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Chronology?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Core** | [![DataStandardizer.Core package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/b6a3e5a6-2d7a-447c-a155-1c0a086363fc/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Core?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.File** | [![DataStandardizer.File package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/c6f508d9-58a4-49ca-aa7b-7dd5b550b394/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.File?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.ISO15924** | [![DataStandardizer.ISO15924 package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/0b6df6ce-4422-4bda-90e2-5613843132d0/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.ISO15924?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.ISO3166** | [![DataStandardizer.ISO3166 package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/5f7595e1-827e-4fac-b6cd-17ebb6733e04/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.ISO3166?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.ISO4217** | [![DataStandardizer.ISO4217 package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/7947181f-caad-4096-923c-da7fc87e029f/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.ISO4217?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.ISO639** | [![DataStandardizer.ISO639 package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/2b81108e-e36c-4db0-99e2-f730b7fce81e/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.ISO639?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.Money** | [![DataStandardizer.Money package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/48e16c89-27a8-4e0d-9684-f2b2f00313ac/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.Money?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |
| **DataStandardizer.UNM49** | [![DataStandardizer.UNM49 package in DataStandardizer@Release feed in Azure Artifacts](https://feeds.dev.azure.com/solobyte/e60c6c5b-e7d1-4e3e-bc68-14798bf709a1/_apis/public/Packaging/Feeds/DataStandardizer@Release/Packages/be535293-fb75-40a2-a858-2b0e58182d2a/Badge)](https://dev.azure.com/solobyte/DataStandardizer/_artifacts/feed/DataStandardizer@Release/NuGet/DataStandardizer.UNM49?preferRelease=true) | [![Build Status](https://dev.azure.com/solobyte/DataStandardizer/_apis/build/status%2FDataStandardizer-Release?repoName=matthew25187%2FDataStandardizer&branchName=master)](https://dev.azure.com/solobyte/DataStandardizer/_build/latest?definitionId=62&repoName=matthew25187%2FDataStandardizer&branchName=master) |

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

Though each package contains many types, typically there will be only a few that you will end up using directly in your application.  Listed here are the main types you are most likely to include in your source code.

## DataStandardizer.BCP47

| Type | Description |
| --- | --- |
| `Bcp47LanguageTag` | Represents an IETF language tag.  May be created by using the provided static factory methods or by using the language tag builder. |
| `Bcp47LanguageTagBuilder` | Can be used to construct a language tag using a fluent-style syntax. |
| `SubtagRegistry` | Represents a copy of the *IANA Subtag Registry*.  May be loaded by various means, but the source must be in the original "record-jar" format as described in RFC 5646.  Used to create language tags based on the subtag registry (which defines most valid tags and subtags) as opposed to creating a language tag based just on the rules defined by RFC 5646. |
| `SubtagRegistryFileDateRecord` | Represents a "File-Date" record from the subtag registry. |
| `SubtagRegistrySubtagRecord` | Represents a "Subtag" record from the subtag registry. |
| `SubtagRegistryTagRecord` | Represents a "Tag" record from the subtag registry. |

## DataStandardizer.Chronology

| Type | Description |
| --- | --- |
| `TzDataTimezone` | An enum containing the timezones defined by the TZ Database. |

## DataStandardizer.File

| Type | Description |
| --- | --- |
| `CsvFieldMappingAttribute` | Declares the mapping of a property to a CSV field. |
| `CsvFileHeaderLine` | Represents a header line from a CSV file. |
| `CsvFileOptions` | Options for configuring the behaviour of a CSV reader or writer. |
| `CsvFileReader` | Reader of a CSV file sourced from a `Stream`, `TextReader` or file. |
| `CsvFileRecordLine` | Represents a record line from a CSV file. |
| `CsvFileWriter` | Writes a CSV file to a `Stream`, `TextReader` or file. |

### Usage

Included here is a brief primer on the basic functionality of the *Data Standardizer* CSV implementation.  Certain advanced topics will not be covered here.

Reading and writing of CSV files is handled in much the same way as you would read or write a regular text file.  The difference being, the `CsvFileReader` and `CsvFileWriter` will interpret the text of each line using the rules of RFC 4180 and any configuration options you supply.

To read a CSV file into memory (without a header line), you can process the file in a simple loop.

        var lines = new List<ICsvFileLine>();

        using (var reader = new StreamReader(@"file_name.csv"))
        using (var csvReader = new CsvFileReader<CsvFileRecordLine>(reader))
        {
            var line = csvReader.ReadLine();
            while (line != null)
            {
                lines.Add(line);

                line = csvReader.ReadLine();
            }
        }

Once you have loaded a line from the file, you can access its individual fields through the `ICsvFileLine` interface.  By default, the values of each of the fields on a line will be the raw string values extracted from the file.

        foreach (var line in lines)
        {
            if ((line["field_name"]?.Equals("some_field_value")).GetValueOrDefault())
            {
                // Process the line if a field contains a specific value.
            }
        }

The behaviour of the reader (or writer) can be configured by using an options object.  Here, we can tell the reader to expect the file to have a header line.

        var lines = new List<ICsvFileLine>();
        var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

        using (var reader = new StreamReader(@"file_name.csv"))
        using (var csvReader = new CsvFileReader<CsvFileRecordLine>(reader, options))
        {
            var line = csvReader.ReadLine();
            while (line != null)
            {
                lines.Add(line);

                line = csvReader.ReadLine();
            }
        }

Note that as each line is loaded (or afterwards if you are storing the lines in a collection) you will be able to tell if it is a header line or a record line by checking the type of the line.  For example,

        ICsvFileLine line = ...;
        
        if (line is CsvFileHeaderLine headerLine)
        {
            // This is a header line.
        }

When checking for a record line, the type you compare against should generally be the same as the generic type argument used for instantiating the `CsvFileReader` (or `CsvFileWriter`).

        ICsvFileLine line = ...;

        if (line is CsvFileRecordLine recordLine)
        {
            // This is a record line.
        }

The reader will automatically return a line of the appropriate type depending on how it was configured and what it was expecting.

You can define your own record line model that uses properties to access indivisual fields.  These custom record line models will derive from the base `CsvFileRecordLine` implementation.
```
public class MyCustomRecordLine : CsvFileRecordLine
{
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public string? Name
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string? Description
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
```

To use this custom record line, you would then specify this type when instantiating the reader, e.g. `CsvFileReader<MyCustomRecordLine>(...)`.

You can also map properties on a custom line model to fields in the CSV file.  This can be done in either of two ways: either declaratively, decorating the properties with an attribute that describes the field associated with the property, or imperatively, using a separate mapper implementation.

A declarative mapping might look something like this:
```
public class MyCustomRecordLine : CsvFileRecordLine
{
    [CsvFieldMapping("identifier")]
    [TypeConverter(typeof(Int32Converter))]
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    [CsvFieldMapping("person_name")]
    public string? Name
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    [CsvFieldMapping("person_description", IsOptional = true)]
    public string? Description
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
```

Whereas the same mapping defined imperatively might look like this:
```
public class MyCustomRecordLineMapper : CsvFileMapperBase<MyCustomRecordLine>
{
    public MyCustomRecordLineMapper()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldName("identifier")
            .ConvertUsing(typeof(Int32Converter));
        this.Map()
            .Property(x => x.Name)
            .HasFieldName("person_name");
        this.Map()
            .Property(x => x.Description)
            .HasFieldName("person_description")
            .IsOptional();
    }
}
```

Note that due to technical limitations, certain mapping functionality is unavailable in the declarative form and can only be utilized using imperative mappers.

Writing CSV files is very similar to reading them.

            ICsvFileLine recordLine = new CsvFileRecordLine
            {
                { "identifier", "1" },
                { "person_name", "John Doe" },
                { "person_description", "Male" }
            };
            var lines = new List<ICsvFileLine> { recordLine };

            using (var writer = new StreamWriter(@"file_name.csv"))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(writer))
            {
                foreach (var line in lines)
                {
                    csvWriter.WriteLine(line);
                }
            }

Note that if your file should contain a header line, it is up to you to make sure that the first line you write to the file is a `CsvFileHeaderLine` followed by `CsvFileRecordLine` objects (or derivative implementations) for the record lines.

## DataStandardizer.ISO15924

| Type | Description |
| --- | --- |
| `Iso15924Script` | An enum containing script codes from ISO 15924.  Includes both the four-letter alpha codes and three-digit numeric codes from the standard as the name and value of the members, respectively. |

## DataStandardizer.ISO3166

| Type | Description |
| --- | --- |
| `Iso3166Part1Alpha2Country` | An enum containing the country codes from ISO 3166-1 Alpha-2.  Includes both the two-letter alpha codes and numeric codes from the standard as the name and value of the members, respectively. |
| `Iso3166Part1Alpha3Country` | An enum containing the country codes from ISO 3166-1 Alpha-3.  Includes both the three-letter alpha codes and numeric codes from the standard as the name and value of the members, respectively. |
| `Iso3166Part2Subdivision` | An enum containing the subdivision codes from ISO 3166-2.  Given the hierarchical nature of these codes, this implementation uses a nested structure to access the codes so that each group of subdivision codes is grouped under a nested type named after the country code of the country the subdivision codes belong to. |

## DataStandardizer.ISO4217

| Type | Description |
| --- | --- |
| `Iso4217CurrencyCurrent` | An enum containing active currency codes from ISO 4217.  Includes both the three-letter alpha codes and numeric codes from the standard as the name and value of each member, respectively. |
| `Iso4217CurrencyHistoric` | An enum containing retired currency codes from ISO 4217.  Includes both the three-letter alpha codes and numeric codes from the standard as the name and value of each member, respectively. |

## DataStandardizer.ISO639

| Type | Description |
| --- | --- |
| `Iso639Part1Language` | An enum containing the alpha-2 language codes from ISO 639-1. |
| `Iso639Part2BLanguage` | An enum containing the bibliographic alpha-3 language codes from ISO 639-2. |
| `Iso639Part2TLanguage` | An enum containing the terminological alpha-3 language codes from ISO 639-2. |
| `Iso639Part3Language` | An enum containing the alpha-3 language codes from ISO 639-3. |
| `Iso639Part5LanguageFamily` | An enum containing the alpha-3 language family codes from ISO 639-5. |

## DataStandardizer.Money

| Type | Description |
| --- | --- |
| `Money` | A data type for handling a monetary value comprising an amount and a currency code.  Optionally supports user-specified rounding that is applied on conversion to a `decimal` value. |

## DataStandardizer.UNM49

| Type | Description |
| --- | --- |
| `UnM49AreaByAlpha2CountryCode` | An enum containing the numeric M49 codes from standard UN M49.  Because of technical requirements on the naming of members, each code is keyed on its corresponding ISO 3166-1 alpha-2 code. |
| `UnM49AreaByAlpha3CountryCode` | An enum containing the numeric M49 codes from standard UN M49.  Because of technical requirements on the naming of members, each code is keyed on its corresponding ISO 3166-1 alpha-3 code. |

N.B. Because of the way the source data is arranged, the above enums only directly include members representing M.49 codes that have a corresponding alpha-2 or alpha-3 code from ISO 3166-1.  There are additional M.49 codes representing supra-national regions or other areas that are included as metadata on these enum members, and can be retrieved using provided extension methods.