---
title: Build from source
parent: Resources
nav_order: 3
---

# Build from source

How to build and test your own copy of the *Data Standardizer* repository.

> *Data Standardizer* is open source so you can read, build, and adapt it for
> your own use. The project does **not** currently accept third-party code
> contributions. The
> instructions on this page are provided purely as a convenience for working with
> your **own copy or fork** of the repository.

## Build the source code

First, clone the repository to your local machine. (GitHub's
[cloning a repository](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository)
guide covers how.)

With the source code in place, open a command prompt, change the current
directory to the repository root folder, and build the entire solution:

```shell
dotnet build DataStandardizer.sln
```

You can also work with the source in an IDE such as *Visual Studio* or
*Visual Studio Code* — open the `DataStandardizer.sln` solution file to access
the code.

## Build a single package

Alongside the main solution file, the repository root contains a solution filter
file (`*.slnf`) for each package. These filters narrow the scope to only the
projects needed to build and test a single package. You can build a filter
directly, or open it in your IDE if you only want to work with the code for one
package:

```shell
dotnet build DataStandardizer.Money.slnf
```

The CI pipelines use these filters to build and test each package individually.

## Run the tests

The included tests use the xUnit test framework, and the test projects bring in a
default test runner so you can run them from the command line. From the repository
root:

```shell
dotnet test DataStandardizer.sln
```

In *Visual Studio*, the
[Test Explorer](https://learn.microsoft.com/en-us/visualstudio/test/run-unit-tests-with-test-explorer?view=vs-2022)
lets you discover and run tests by various categorizations. Testing is also
supported in *Visual Studio Code* with the
[C# Dev Kit](https://code.visualstudio.com/docs/csharp/testing).

## Branching strategy

The repository uses two "main" branches:

| Branch | Role |
| --- | --- |
| `master` | The top-level branch from which all package **release** builds are produced. `develop` is merged into `master` when a new release is done. |
| `develop` | The **default** branch and the one from which **preview** package builds are produced. Changes are marshalled here before being included in a release build. |

Other branches that may be created from time to time are not relevant when
working with your own copy.

## Advanced: the enum-generation scripts

The repository includes a number of *PowerShell* scripts whose names start with
**Generate**. These regenerate the enums that implement each standard from the
official flat-file data sources published by the relevant standards body or
designated maintainer. Running them requires a *PowerShell* prompt, access to
those official data sources, and — for some scripts — a minimum *PowerShell*
version.

These scripts are described here for awareness only; they are how the code lists
are kept current with the standards bodies. The concept page
[Data currency and versioning](../concepts/data-currency-and-versioning.md)
explains how that regeneration fits into the wider design.

Other scripts and YAML files in the repository support the project's
infrastructure (pipelines, package hosting, and so on) and are not intended for
end-user use.
