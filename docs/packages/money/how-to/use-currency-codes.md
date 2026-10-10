---
title: Use currency codes
parent: Money
grand_parent: Packages
nav_order: 2
---

# Use currency codes

ISO 4217 is implemented as two separate enums for current and historic currency
codes, so you can select a strongly-typed currency code wherever your code needs
one.

Each member of the enum includes the currency code from the standard as the name
of the enum member and the numeric code from the standard as the value of the
member.

To access an individual currency code from the current collection, you can use
it like any other enum:

```csharp
var currencyCode = Iso4217CurrencyCurrent.INR;  // Indian Rupee
```

Similarly, historic currency codes can be accessed from the relevant enum:

```csharp
var oldCurrencyCode = Iso4217CurrencyHistoric.ZWD;  // Zimbabwe Dollar
```

Both enums live in the `DataStandardizer.Money` namespace. To read the name,
minor units, and other metadata for a code, see
[Access currency metadata](access-currency-metadata.md).

## Shorten the type name with an alias

Most code only needs current currency codes. If so, you can give
`Iso4217CurrencyCurrent` a shorter name with a using alias directive:

```csharp
using Iso4217Currency = DataStandardizer.Money.Iso4217CurrencyCurrent;

var currencyCode = Iso4217Currency.INR;  // Indian Rupee
```

A using alias works in every C# version, but it only applies to the file that
declares it. From C# 10 you can declare the alias once for the whole project by
adding the `global` modifier, usually in a single file such as
`GlobalUsings.cs`:

```csharp
global using Iso4217Currency = DataStandardizer.Money.Iso4217CurrencyCurrent;
```

In an SDK-style project, you can declare the same global alias in the project
file instead. The SDK generates the `global using` directive for you, alongside
any implicit usings:

```xml
<ItemGroup>
  <Using Include="DataStandardizer.Money.Iso4217CurrencyCurrent" Alias="Iso4217Currency" />
</ItemGroup>
```

Global usings need C# 10 or later. .NET 6 and later projects use C# 10 or later
by default. Projects that target .NET Framework or .NET Standard default to an
older language version, so set `<LangVersion>` to `10` or later to use them.
