---
title: API reference
parent: Communication
grand_parent: Packages
nav_order: 20
---

# DataStandardizer.Communication API reference

The public types of **DataStandardizer.Communication**. `ITelephonyNumber` and
`TelephonyInfo` are in the `DataStandardizer.Communication` namespace; the E.164
types are in the `DataStandardizer.Communication.E164` namespace; the MIME types are
in the `DataStandardizer.Communication.Mime` namespace.

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

## MIME structures

| Type | Description |
| --- | --- |
| [MimeCharset](MimeCharset.md) | A character set name (RFC 2978 §2.3). |
| [MimeDataUri](MimeDataUri.md) | A `data:` URI (RFC 2397). |
| [MimeEncodedWord](MimeEncodedWord.md) | An RFC 2047 encoded word. |
| [MimeMediaType](MimeMediaType.md) | A media type and its parameters (RFC 2045 §5.1). |
| [MimeParameter](MimeParameter.md) | A media type parameter, with RFC 2231 extended values. |
| [MimeTransferEncoding](MimeTransferEncoding.md) | A Content-Transfer-Encoding mechanism (RFC 2045 §6.1). |

## MIME classes

| Type | Description |
| --- | --- |
| [MimeBase64](MimeBase64.md) | Encodes and decodes whole values in base64. |
| [MimeBase64Decoder](MimeBase64Decoder.md) | Decodes base64 data as it arrives, buffer by buffer. |
| [MimeBase64Encoder](MimeBase64Encoder.md) | Encodes data in base64 as it arrives, buffer by buffer. |
| [MimeCharsetResolver](MimeCharsetResolver.md) | Gets the `Encoding` for a character set. |
| [MimeQuotedPrintable](MimeQuotedPrintable.md) | Encodes and decodes whole values in quoted-printable. |
| [MimeQuotedPrintableDecoder](MimeQuotedPrintableDecoder.md) | Decodes quoted-printable data as it arrives, buffer by buffer. |
| [MimeQuotedPrintableEncoder](MimeQuotedPrintableEncoder.md) | Encodes data in quoted-printable as it arrives, buffer by buffer. |
| [MimeTopLevelTypes](MimeTopLevelTypes.md) | The registered top-level media types. |

## MIME enumerations

| Type | Description |
| --- | --- |
| [MimeBase64DecoderOptions](MimeBase64DecoderOptions.md) | Checks a base64 decoder makes beyond those of its mode. |
| [MimeBase64Mode](MimeBase64Mode.md) | A form of base64: strict, or the MIME transfer encoding. |
| [MimeDataDomain](MimeDataDomain.md) | The kind of data a transport must carry for a body. |
| [MimeEncodedWordContext](MimeEncodedWordContext.md) | The place in a header field where an encoded word appears. |
| [MimeEncodedWordEncoding](MimeEncodedWordEncoding.md) | The "B" or "Q" encoding of an encoded word. |
| [MimeEncodedWordParseOptions](MimeEncodedWordParseOptions.md) | Accommodations for encoded words that don't follow the rules. |
| [MimeMediaTypeParseOptions](MimeMediaTypeParseOptions.md) | Accommodations for media types that don't follow the strict grammar. |
| [MimeQuotedPrintableDecoderOptions](MimeQuotedPrintableDecoderOptions.md) | Forms a quoted-printable decoder accepts beyond those the standards allow. |
| [MimeQuotedPrintableEncoderOptions](MimeQuotedPrintableEncoderOptions.md) | How a quoted-printable encoder treats the data it encodes. |
| [MimeQuotedPrintableMode](MimeQuotedPrintableMode.md) | A form of quoted-printable: a body, or a "Q" encoded word in one of its places. |
| [MimeRegistrationTree](MimeRegistrationTree.md) | The registration tree of a media type. |

## MIME interfaces

| Type | Description |
| --- | --- |
| [IMimeCharsetResolver](IMimeCharsetResolver.md) | Gets the `Encoding` for a character set; implement it to add character sets. |
