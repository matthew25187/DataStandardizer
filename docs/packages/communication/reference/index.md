---
title: API reference
parent: Communication
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.Communication API reference

The public types of **DataStandardizer.Communication**. `ITelephonyNumber` and
`TelephonyInfo` are in the `DataStandardizer.Communication` namespace; the E.164
types are in the `DataStandardizer.Communication.E164` namespace.

## Structures

| Type | Description |
| --- | --- |
| [ItuE164InternationalNumber](ItuE164InternationalNumber.md) | An ITU-T E.164 international telephone number of any kind. |
| [ItuE164NationalSignificantNumber](ItuE164NationalSignificantNumber.md) | The national significant number (NSN) field of a geographic-area number. |
| [ItuE164SubscriberNumber](ItuE164SubscriberNumber.md) | The subscriber number field of a network, group-of-countries, or trial number. |
| [ItuE164GlobalSubscriberNumber](ItuE164GlobalSubscriberNumber.md) | The global subscriber number field of a global-service number. |

## Classes

| Type | Description |
| --- | --- |
| [TelephonyInfo](TelephonyInfo.md) | A culture/region-aware format provider for international numbers. |
| [ItuE164InternationalNumberFormatInfo](ItuE164InternationalNumberFormatInfo.md) | Holds the patterns used to format an international number. |
| [ItuE164SharedCodeAttribute](ItuE164SharedCodeAttribute.md) | Records the country code under which an assigned code is shared. |

## Enumerations

| Type | Description |
| --- | --- |
| [ItuE164InternationalNumberStyles](ItuE164InternationalNumberStyles.md) | Flags that control what a parse method tolerates in the input string. |
| [ItuE164AssignedCountryCodesForGeographicAreas](ItuE164AssignedCountryCodesForGeographicAreas.md) | Country codes assigned by the ITU for geographic areas. |
| [ItuE164AssignedCountryCodesForGlobalServices](ItuE164AssignedCountryCodesForGlobalServices.md) | Country codes assigned by the ITU for global services. |
| [ItuE164AssignedCountryCodesForNetworks](ItuE164AssignedCountryCodesForNetworks.md) | Country codes assigned by the ITU for networks. |
| [ItuE164AssignedCountryCodesForGroupsOfCountries](ItuE164AssignedCountryCodesForGroupsOfCountries.md) | Country codes assigned by the ITU for groups of countries. |
| [ItuE164AssignedCountryCodesForTrials](ItuE164AssignedCountryCodesForTrials.md) | Country codes assigned by the ITU for trials. |
| [ItuE164AssignedIdentificationCodesForNetworks](ItuE164AssignedIdentificationCodesForNetworks.md) | Identification codes assigned by the ITU within network country codes. |
| [ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries](ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries.md) | Group identification codes assigned by the ITU within group-of-countries codes. |
| [ItuE164AssignedTrialIdentificationCodesForTrials](ItuE164AssignedTrialIdentificationCodesForTrials.md) | Trial identification codes assigned by the ITU within trial codes. |

## Interfaces

| Type | Description |
| --- | --- |
| [ITelephonyNumber](ITelephonyNumber.md) | The root abstraction for any telephony number. |
| [IItuE164Field](IItuE164Field.md) | A field of an E.164 number; exposes its digit count. |
| [IItuE164InternationalNumber](IItuE164InternationalNumber.md) | The base for every kind of E.164 international number. |
| [IItuE164InternationalNumberForGeographicAreas](IItuE164InternationalNumberForGeographicAreas.md) | The geographic-area number role. |
| [IItuE164InternationalNumberForGlobalServices](IItuE164InternationalNumberForGlobalServices.md) | The global-service number role. |
| [IItuE164InternationalNumberForNetworks](IItuE164InternationalNumberForNetworks.md) | The network number role. |
| [IItuE164InternationalNumberForGroupsOfCountries](IItuE164InternationalNumberForGroupsOfCountries.md) | The group-of-countries number role. |
| [IItuE164InternationalNumberForTrials](IItuE164InternationalNumberForTrials.md) | The trial number role. |
</content>
</invoke>
