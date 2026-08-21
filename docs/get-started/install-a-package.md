---
title: Install a package
parent: Get started
nav_order: 1
---

# Install a package

Add the package for the standard you need to your project using your preferred
.NET tooling.

Not sure which package you need? See [Standards coverage](../overview/standards-coverage.md)
to map each standard to its package.

## Installable packages

| Package | Install name |
| --- | --- |
| **Chronology** | `DataStandardizer.Chronology` |
| **Communication** | `DataStandardizer.Communication` |
| **File.CSV** | `DataStandardizer.File.CSV` |
| **Geography** | `DataStandardizer.Geography` |
| **Language** | `DataStandardizer.Language` |
| **LanguageTag** | `DataStandardizer.LanguageTag` |
| **Money** | `DataStandardizer.Money` |

**DataStandardizer.Core** is a shared dependency of the other packages and is
resolved automatically — you don't install it directly.

## .NET CLI

From your project directory, run `dotnet add package` with the package name. For
example:

```shell
dotnet add package DataStandardizer.Money
```

For more detail, see
[Install and manage NuGet packages with the dotnet CLI](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-dotnet-cli).

## Visual Studio

Use the NuGet Package Manager: right-click your project, choose **Manage NuGet
Packages**, search for the package name, and install it.

For more detail, see
[Install and manage packages in Visual Studio using the NuGet Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio).

## Visual Studio Code

Use the built-in NuGet tooling to add the package to your project.

For more detail, see
[NuGet in Visual Studio Code](https://code.visualstudio.com/docs/csharp/package-management).

## Where to next

- Walk through a full example in the [Quickstart](quickstart.md).
- Confirm the framework targets you need on [Platform support](../overview/platform-support.md).
