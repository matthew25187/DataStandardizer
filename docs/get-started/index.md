---
title: Get started
nav_order: 3
has_children: true
---

# Get started

*Data Standardizer* is distributed as a series of NuGet packages. To use a
particular standard, add the corresponding package to your project and reference
its types.

## Install a package

Pick the package for the standard you need (see [Packages](../packages/index.md)) and add
it with the .NET CLI:

```shell
dotnet add package DataStandardizer.Chronology
```

You can also install via the Visual Studio NuGet Package Manager or the
Visual Studio Code NuGet tooling. Detailed links are on the
**Install a package** page.

## A first example

Once a package is referenced, standardised values are available as strongly-typed
members:

```csharp
using DataStandardizer.Money;

// A currency from ISO 4217 — not a magic string.
var currency = Iso4217CurrencyCurrent.NZD;   // New Zealand Dollar
```

See **Quickstart** for a fuller walkthrough.

## In this section

- [Install a package](install-a-package.md) — Visual Studio, .NET CLI, and
  VS Code instructions.
- [Quickstart](quickstart.md) — create and use your first strongly-typed value
  end to end.
