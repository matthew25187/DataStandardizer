---
title: Geography
parent: Packages
nav_order: 4
has_children: true
---

# DataStandardizer.Geography

Strongly-typed support for geography-related data standards: country codes,
country subdivision codes, and standard country or area codes for statistical
use.

```shell
dotnet add package DataStandardizer.Geography
```

## Standards

| Standard | What it provides |
| --- | --- |
| **ISO 3166-1** | Codes for the representation of names of countries — alpha-2, alpha-3, and numeric. |
| **ISO 3166-2** | Codes for the representation of country subdivisions (e.g. `AU-NT`). |
| **UN M49** | Standard country or area codes for statistical use, with a geographical region hierarchy. |

## Platform support

Targets .NET Standard 1.0 and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes (.NET 8 and .NET 10).

## How-to guides

- [Use country codes](how-to/use-country-codes.md)
- [Use subdivision codes](how-to/use-subdivision-codes.md)
- [Access country and subdivision metadata](how-to/access-country-and-subdivision-metadata.md)
- [Use area codes](how-to/use-area-codes.md)
- [Access area metadata](how-to/access-area-metadata.md)

## Reference

- [API reference](reference/index.md)
