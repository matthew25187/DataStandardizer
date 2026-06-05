---
title: Money
parent: Packages
nav_order: 7
has_children: true
---

# DataStandardizer.Money

Strongly-typed support for money-related data standards: ISO 4217 currency codes
(current and historic) and a `Money` value type that pairs an amount with a
currency.

```shell
dotnet add package DataStandardizer.Money
```

## Standards

| Standard | What it provides |
| --- | --- |
| **ISO 4217** | Codes for the representation of currencies and funds — current currency & funds codes (Tables A.1–A.2) and historic denominations (Table A.3), each carrying the standard's numeric code and metadata. |
| **Money type** | A value type combining an amount with an ISO 4217 currency, as described in *Patterns of Enterprise Application Architecture* by Martin Fowler. |

## Platform support

Targets .NET Standard 1.0 and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes (net8.0 and net10.0).

## How-to guides

- [Use the Money data type](how-to/use-money-datatype.md)
- [Use currency codes](how-to/use-currency-codes.md)
- [Access currency metadata](how-to/access-currency-metadata.md)

## Concepts

- [The Money type](concepts/money-type.md)

## Reference

- [API reference](reference/index.md)
