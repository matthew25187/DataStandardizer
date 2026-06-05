---
title: Strongly-typed codes
parent: Concepts
nav_order: 2
---

# Strongly-typed codes

Data Standardizer represents each standard's code set as a type, but the *kind*
of type depends on the codes: where they obey C# enum rules they are a plain
`enum`, and where they do not they are a `readonly struct` that behaves enum-like
through a small shared contract in the `Core` package.

## Two shapes for one idea

A C# `enum` is backed by an integer. That is a perfect fit when the standard
itself assigns numbers to its codes and those numbers obey the language's rules
for enum members. ISO 15924 (script codes) and the ISO 4217 currency lists are
modelled this way — they have numeric codes, so each is a conventional C# `enum`
(for example `Iso15924Script`, `Iso4217CurrencyCurrent`).

Many standards, though, identify their codes by *string*, not number, and those
strings break enum rules — they are alphanumeric, case-significant, or begin with
a digit, none of which a C# enum member can express. For these the library uses a
`readonly struct` whose underlying value is the code string itself:

```text
                    does the standard number its codes,
                    and do those numbers fit enum rules?
                                    │
                 ┌──────────────────┴──────────────────┐
               yes                                     no
                │                                       │
        C# enum (int-backed)              readonly struct : IStringEnum
        Iso15924Script                    Iso639Part1Language
        Iso4217CurrencyCurrent            Iso639Part3Language ...
```

The two shapes are deliberately used the same way. An enum member is
`Iso15924Script.Latn`; a struct member is `Iso639Part1Language.en`. Both are
accessed as static members, so calling code reads identically whichever shape
underlies it. The structs even define an implicit conversion to `string`, so a
code drops straight into anywhere a string is expected.

## The `IStringEnum` contract

The string-backed structs all implement `IStringEnum`, declared in
**DataStandardizer.Core**. It is a marker-style interface: it adds no members of
its own beyond requiring `IComparable` (and `IConvertible` on .NET Standard 1.3+
and modern .NET), so an `IStringEnum` value can be compared and converted like
any ordinary value. Its real job is to *identify* a type as a string enumeration
— `Core`'s helper code recognises a string enum precisely by whether it is a
value type implementing this interface.

## The `StringEnum` helper

`StringEnum` is a static class in `Core` that provides for string-backed structs
the operations the runtime gives enums for free. Its methods mirror the familiar
`System.Enum` surface, verified from the source:

- `GetName` / `GetNames` — the member name(s) for a value, or all names in a type.
- `GetValues` — all values defined in a type.
- `IsDefined` — whether a given name or value exists.
- `Parse` / `TryParse` — convert a name or underlying value into a member, with
  optional case-insensitive overloads.
- `ToObject` — build a member from its underlying value.

Each comes in a generic form constrained to `where TEnum : struct, IStringEnum`
and a non-generic `Type`-based form. Internally `StringEnum` reflects over a
type's public static fields and caches the name/value mappings, so repeated
lookups on the same type are cheap. (One caution noted in the source: these
methods must not be called from *within* a string enumeration's own members, or
infinite recursion can result.)

## `CodeAttributeBase`

The third `Core` building block is `CodeAttributeBase`, the abstract base for the
per-standard `*CodeAttribute` types that carry metadata. It is an `Attribute`
that holds English and French name collections and exposes `EnglishName` /
`EnglishNames` and `FrenchName` / `FrenchNames` (the singular forms return the
first entry). Each package derives a concrete attribute from it to attach
standard-specific data to individual code members. That mechanism — and the
`Get*` methods that read it back — is covered in
[Metadata and lookups](metadata-and-lookups.md).

## `Core` is an internal dependency

**DataStandardizer.Core** is the shared foundation that defines `IStringEnum`,
`StringEnum`, and `CodeAttributeBase`. You do not reference it directly: each
public package brings it in as a transitive dependency, and the enum-like types
you actually use live in those packages (Money, Language, Geography, and so on).
`Core` is the plumbing that makes the pattern consistent across all of them.

## Why this design

- **One way to use a code.** Whether a standard is an enum or a struct, members
  are static and read the same at the call site.
- **Faithful to each standard.** Numeric standards stay enums; string standards
  keep the string as their value rather than being forced into integers.
- **Enum ergonomics everywhere.** `StringEnum` gives the struct types the
  `Parse`/`TryParse`/`GetNames`/`IsDefined` operations enums get from the runtime.
- **Shared, not duplicated.** The contract and helpers live once in `Core`.

## Related

- [Why standardize data](why-standardize-data.md)
- [Metadata and lookups](metadata-and-lookups.md)
- [Understanding the ISO 639 parts](../packages/language/concepts/iso639-parts.md)
