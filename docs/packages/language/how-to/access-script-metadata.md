---
title: Access script metadata
parent: Language
grand_parent: Packages
nav_order: 4
---

# Access script metadata

Each ISO 15924 script code carries associated metadata that you retrieve through
extension methods on the `Iso15924Script` enum.

## Age

The ISO 15924 standard defines an age for each script code. This age can be
retrieved from a script code:

```csharp
var age = Iso15924Script.Arab.GetAge();
```

## Alias

Some script codes have an alias defined for them. Where a script code has an
alias, you can retrieve it like so:

```csharp
var alias = Iso15924Script.Adlm.GetAlias();
```

## Date

Each script code has a date associated with it. You can retrieve the date using
an extension method on a script code:

```csharp
var scriptDate = Iso15924Script.Egyp.GetDate();
```

> **Applies to:** on .NET 6 and later `GetDate()` returns a `DateOnly?`; on the
> .NET Standard targets it returns a `DateTime?`.

## English name

Script codes have an English name defined for them. The English name can be
retrieved like so:

```csharp
var englishName = Iso15924Script.Perm.GetEnglishName();
```

## French name

The standard defines a French name for each of the script codes. You can
retrieve a script's French name with an extension method on the script code:

```csharp
var frenchName = Iso15924Script.Arab.GetFrenchName();
```
