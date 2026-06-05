---
title: The E.164 number model
parent: Communication
grand_parent: Packages
nav_order: 10
---

# The E.164 number model

The Communication package models an ITU-T E.164 international telephone number as
a single value that is internally decomposed into the fields defined by the
standard. A handful of interfaces expose those fields, and which interface
applies depends on the *kind* of number you hold.

## One number, many shapes

Every international number is stored as the `ItuE164InternationalNumber` struct.
Whatever its kind, the whole number is available as a single unsigned integer
through the `Number` property (declared on the root `ITelephonyNumber`
interface). What changes between kinds is *how the digits are interpreted*.

E.164 reserves different ranges of country codes for different purposes, so a
number's leading digits determine which kind it is. The package recognises five
kinds:

| Kind | Created with | Distinguishing fields |
| --- | --- | --- |
| Geographic area | `CreateNumberForGeographicArea` | country code + national significant number |
| Global service | `CreateNumberForGlobalService` | country code + global subscriber number |
| Network | `CreateNumberForNetwork` | country code + identification code + subscriber number |
| Group of countries | `CreateNumberForGroupOfCountries` | country code + group identification code + subscriber number |
| Trial | `CreateNumberForTrial` | country code + trial identification code + (optional) subscriber number |

Regardless of kind, the total number of significant digits never exceeds 15, as
mandated by E.164.

## The field interfaces

The fields themselves are strongly typed. Each component type
(`ItuE164NationalSignificantNumber`, `ItuE164SubscriberNumber`,
`ItuE164GlobalSubscriberNumber`) implements `IItuE164Field`, which exposes a
single member:

```text
IItuE164Field
└── int DigitCount { get; }   // number of digits in the field value
```

`DigitCount` is what the factory methods use to validate that a number fits
within the 15-digit limit once the country code (and any identification code) is
accounted for.

## The role interfaces

The kinds above map onto a small interface hierarchy. The root is
`IItuE164InternationalNumber`, which extends `ITelephonyNumber` and adds the one
field common to every kind — the `CountryCode`:

```text
ITelephonyNumber                                  // ulong Number
└── IItuE164InternationalNumber                   // + ushort CountryCode
    ├── IItuE164InternationalNumberForGeographicAreas   // + NationalSignificantNumber
    ├── IItuE164InternationalNumberForGlobalServices    // + GlobalSubscriberNumber
    ├── IItuE164InternationalNumberForNetworks          // + IdentificationCode + SubscriberNumber
    ├── IItuE164InternationalNumberForGroupsOfCountries // + GroupIdentificationCode + SubscriberNumber
    └── IItuE164InternationalNumberForTrials            // + TrialIdentificationCode + SubscriberNumber?
```

`ItuE164InternationalNumber` implements **all five** role interfaces. That makes
it convertible to any of them, but only the interface matching the number's
actual kind will return its fields successfully. Accessing a field through the
wrong role interface throws `NotSupportedException`.

## Discover the kind, then read the fields

Because a single struct plays every role, you discover which role is valid at
runtime with the `IsNumberFor…` test methods, then cast to the matching
interface to read its fields:

```csharp
var myNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(15125550190L);

if (myNumber.IsNumberForGeographicArea())
{
    IItuE164InternationalNumberForGeographicAreas geographic = myNumber;
    ushort countryCode = geographic.CountryCode;
    var nationalNumber = geographic.NationalSignificantNumber;
}
```

The corresponding test methods are `IsNumberForGeographicArea`,
`IsNumberForGlobalService`, `IsNumberForNetwork`, `IsNumberForGroupOfCountries`,
and `IsNumberForTrial`.

## Why this design

- **One storage type.** Every kind shares `ItuE164InternationalNumber`, so a
  variable or field can hold any international number without committing to a kind
  up front — useful when a number comes from parsing.
- **Self-identifying numbers.** Because country-code ranges are pre-allocated per
  kind, `Parse`/`TryParse` can identify the kind from the digits alone; callers
  never declare it.
- **Type-safe fields.** Each field is its own struct implementing `IItuE164Field`,
  so digit-count rules are enforced when a number is built rather than left to the
  caller.

## Related

- [Use E.164 international numbers](../how-to/use-international-numbers.md)
- [API reference](../reference/index.md)
