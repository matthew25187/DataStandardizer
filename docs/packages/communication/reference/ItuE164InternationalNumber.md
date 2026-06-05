---
title: ItuE164InternationalNumber Struct
parent: Communication
grand_parent: Packages
nav_exclude: true
---

# ItuE164InternationalNumber Struct

## Definition

Namespace: `DataStandardizer.Communication.E164`

An ITU-T E.164 international telephone number. A single value holds any kind of
number — geographic area, global service, network, group of countries, or trial.
Instances are created through the static `CreateNumberFor…` factory methods or
parsed from a string. The maximum number of significant digits is 15.

```csharp
public readonly struct ItuE164InternationalNumber : IEquatable<ItuE164InternationalNumber>, IFormattable, IItuE164InternationalNumberForGeographicAreas, IItuE164InternationalNumberForGlobalServices, IItuE164InternationalNumberForNetworks, IItuE164InternationalNumberForGroupsOfCountries, IItuE164InternationalNumberForTrials
```

## Remarks

The struct implements all five role interfaces, but the role-specific fields are
explicit interface implementations: access them by typing the variable as (or
casting to) the matching role interface. Reading a field that does not match the
number's actual kind throws `NotSupportedException`, and reading any field of an
uninitialised value throws `InvalidOperationException`.

The component factory overloads throw `ArgumentException` for an undefined
country/code value and `ArgumentOutOfRangeException` when a supplied field is too
long for the remaining digit budget. `Parse`/`TryParse` try each kind in turn and
identify the number's kind automatically; the caller does not specify it.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `Number` | `ulong Number { get; }` | The whole number. Throws `InvalidOperationException` if the value is uninitialised. Implements `ITelephonyNumber.Number`. |

### Explicit interface properties

Each is implemented explicitly and is callable only through the matching role
interface; reading it on a number of a different kind throws
`NotSupportedException`.

| Property | Signature | Notes |
| --- | --- | --- |
| `CountryCode` | `ushort IItuE164InternationalNumber.CountryCode { get; }` | The country code, common to every kind. |
| `GlobalSubscriberNumber` | `ItuE164GlobalSubscriberNumber IItuE164InternationalNumberForGlobalServices.GlobalSubscriberNumber { get; }` | Global-services role. |
| `GroupIdentificationCode` | `ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries IItuE164InternationalNumberForGroupsOfCountries.GroupIdentificationCode { get; }` | Groups-of-countries role. |
| `IdentificationCode` | `ItuE164AssignedIdentificationCodesForNetworks IItuE164InternationalNumberForNetworks.IdentificationCode { get; }` | Networks role. |
| `NationalSignificantNumber` | `ItuE164NationalSignificantNumber IItuE164InternationalNumberForGeographicAreas.NationalSignificantNumber { get; }` | Geographic-areas role. |
| `SubscriberNumber` | `ItuE164SubscriberNumber IItuE164InternationalNumberForNetworks.SubscriberNumber { get; }` | Networks role. |
| `SubscriberNumber` | `ItuE164SubscriberNumber IItuE164InternationalNumberForGroupsOfCountries.SubscriberNumber { get; }` | Groups-of-countries role. |
| `SubscriberNumber` | `ItuE164SubscriberNumber? IItuE164InternationalNumberForTrials.SubscriberNumber { get; }` | Trials role (nullable). |
| `TrialIdentificationCode` | `ItuE164AssignedTrialIdentificationCodesForTrials IItuE164InternationalNumberForTrials.TrialIdentificationCode { get; }` | Trials role. |

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `CreateNumberForGeographicArea(ulong number)` | `ItuE164InternationalNumber` | Static factory. Whole-number overload. |
| `CreateNumberForGeographicArea(ItuE164AssignedCountryCodesForGeographicAreas countryCode, ItuE164NationalSignificantNumber nationalSignificantNumber)` | `ItuE164InternationalNumber` | Static factory. Component overload. |
| `CreateNumberForGlobalService(ulong number)` | `ItuE164InternationalNumber` | Static factory. Whole-number overload. |
| `CreateNumberForGlobalService(ItuE164AssignedCountryCodesForGlobalServices countryCode, ItuE164GlobalSubscriberNumber globalSubscriberNumber)` | `ItuE164InternationalNumber` | Static factory. Component overload. |
| `CreateNumberForGroupOfCountries(ulong number)` | `ItuE164InternationalNumber` | Static factory. Whole-number overload. |
| `CreateNumberForGroupOfCountries(ItuE164AssignedCountryCodesForGroupsOfCountries countryCode, ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries groupIdentificationCode, ItuE164SubscriberNumber subscriberNumber)` | `ItuE164InternationalNumber` | Static factory. Component overload. |
| `CreateNumberForNetwork(ulong number)` | `ItuE164InternationalNumber` | Static factory. Whole-number overload. |
| `CreateNumberForNetwork(ItuE164AssignedCountryCodesForNetworks countryCode, ItuE164AssignedIdentificationCodesForNetworks identificationCode, ItuE164SubscriberNumber subscriberNumber)` | `ItuE164InternationalNumber` | Static factory. Component overload. |
| `CreateNumberForTrial(ulong number)` | `ItuE164InternationalNumber` | Static factory. Whole-number overload. |
| `CreateNumberForTrial(ItuE164AssignedCountryCodesForTrials countryCode, ItuE164AssignedTrialIdentificationCodesForTrials trialIdentificationCode, ItuE164SubscriberNumber? subscriberNumber)` | `ItuE164InternationalNumber` | Static factory. Component overload (subscriber number optional). |
| `Equals(ItuE164InternationalNumber other)` | `bool` | Compares the underlying numbers. |
| `IsNumberForGeographicArea()` | `bool` | Kind test. |
| `IsNumberForGlobalService()` | `bool` | Kind test. |
| `IsNumberForGroupOfCountries()` | `bool` | Kind test. |
| `IsNumberForNetwork()` | `bool` | Kind test. |
| `IsNumberForTrial()` | `bool` | Kind test. |
| `Parse(string s)` | `ItuE164InternationalNumber` | Static. Throws `FormatException` if `s` is not a recognised number. |
| `Parse(string s, ItuE164InternationalNumberStyles numberStyles)` | `ItuE164InternationalNumber` | Static. As above, with permitted styles. |
| `ToString()` | `string` | Override. Uses the invariant "G" format. |
| `ToString(IFormatProvider? formatProvider)` | `string` | Pass a `TelephonyInfo` to format for a culture/region. |
| `ToString(string? format)` | `string` | |
| `ToString(string? format, IFormatProvider? formatProvider)` | `string` | `IFormattable` implementation (a normal public method). |
| `TryParse(string s, out ItuE164InternationalNumber result)` | `bool` | Static. Returns `false` instead of throwing. |
| `TryParse(string s, ItuE164InternationalNumberStyles numberStyles, out ItuE164InternationalNumber result)` | `bool` | Static. As above, with permitted styles. |

## Operators

| Operator | Signature | Notes |
| --- | --- | --- |
| Explicit | `explicit operator ulong(ItuE164InternationalNumber)` | Unwraps to the whole number. Throws `InvalidCastException` if the value is uninitialised. |

## Applies to

Targets `netstandard1.0`, `netstandard2.0`, `net8.0`, and `net10.0`. On the
.NET Standard targets the `string`/`IFormatProvider` parameters of the `ToString`
overloads are non-nullable; on `net8.0`/`net10.0` they are nullable reference types
(`string?` / `IFormatProvider?`). The available overloads are otherwise the same on
all targets.

## See also

- [Use E.164 international numbers](../how-to/use-international-numbers.md)
- [The E.164 number model](../concepts/e164-number-model.md)
- [IItuE164InternationalNumber](IItuE164InternationalNumber.md)
- [ItuE164InternationalNumberStyles](ItuE164InternationalNumberStyles.md)
- [TelephonyInfo](TelephonyInfo.md)
- [Communication API reference](index.md)
</content>
