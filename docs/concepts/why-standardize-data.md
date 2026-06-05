---
title: Why standardize data
parent: Concepts
nav_order: 1
---

# Why standardize data

A great deal of everyday data is drawn from published standards — currencies,
countries, languages, scripts — yet most code carries those values around as
plain strings or integers, where nothing distinguishes a valid code from a typo.
Data Standardizer's whole purpose is to turn those standardized values into
*types*, so that the compiler catches mistakes that would otherwise surface only
at runtime.

## The problem with raw strings and ints

Consider a currency amount tagged with a code:

```csharp
string currency = "EU";   // meant "EUR" — typo, compiles fine
ProcessPayment(amount, currency);
```

Nothing here is wrong as far as the compiler is concerned. `"EU"` is a perfectly
good `string`; it simply is not a currency. The mistake only reveals itself
later — a failed lookup, a rejected transaction, or worse, a silently wrong
result. The same trap is set wherever a standardized value travels as an
unconstrained primitive:

```text
raw representation        what can go wrong
──────────────────        ─────────────────────────────────────────────
"EUR" / "eur" / "EU"  →   typos, wrong case, codes that don't exist
840 (numeric code)    →   off-by-one digits, the wrong code list entirely
"en" vs "eng"         →   mismatched code forms that look interchangeable
```

These are not exotic bugs. They are the ordinary consequence of representing a
*constrained* set of values with an *unconstrained* type. Any string can hold
any text; any `int` can hold any number. The set of valid currency codes is a
tiny island in that vast space, and a primitive type does nothing to keep you on
it.

## Moving errors to compile time

The remedy is to give each standard its own type whose only legal values are the
codes the standard defines. When a currency is an `Iso4217CurrencyCurrent` rather
than a `string`, the earlier typo is no longer expressible:

```csharp
var currency = Iso4217CurrencyCurrent.EU;   // does not compile — no such member
var currency = Iso4217CurrencyCurrent.EUR;  // compiles — a real code
```

The error has moved from runtime to compile time, which is the cheapest place to
find it. The type also documents intent: a method that takes an
`Iso4217CurrencyCurrent` says, in its signature, exactly what it expects, and the
set of valid arguments is discoverable through IntelliSense rather than buried in
a specification.

## Why this matters across the whole library

Every Data Standardizer package applies the same idea to a different standard, so
the benefit compounds:

- **Mistakes become compile errors, not silent bugs.** Invalid codes simply
  cannot be written.
- **Intent is explicit.** A typed parameter states which standard, and which
  code form, a value belongs to.
- **Values are discoverable.** Members surface through IntelliSense instead of
  living only in a printed table.
- **Mismatches are prevented.** Two code forms that look alike as strings are
  distinct types, so they cannot be confused.

How each standard becomes a type — sometimes a C# `enum`, sometimes a
string-backed struct — is the subject of the next article.

## Related

- [Strongly-typed codes](strongly-typed-codes.md)
- [Metadata and lookups](metadata-and-lookups.md)
- [Data currency and versioning](data-currency-and-versioning.md)
