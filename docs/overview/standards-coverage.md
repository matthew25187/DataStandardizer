---
title: Standards coverage
parent: Overview
nav_order: 1
---

# Standards coverage

Which internationally recognised standard is implemented by which *Data
Standardizer* package.

Each package name links to its overview page, where you'll find install
instructions, how-to guides, and an API reference.

| Standard | Package | Description |
| --- | --- | --- |
| **TZ Database** | [Chronology](../packages/chronology/index.md) | Standardised, named time zones (e.g. `Europe/Berlin`) with associated metadata. |
| **Unix time** | [Chronology](../packages/chronology/index.md) | A point in time as seconds since the Unix epoch (1 January 1970 UTC). |
| **DOS date & time** | [Chronology](../packages/chronology/index.md) | A packed date/time as used by the MS-DOS file system (1980–2107). |
| **ITU-T E.164** | [Communication](../packages/communication/index.md) | The international public telecommunication numbering plan (telephone numbers). |
| **RFC 2045** | [Communication](../packages/communication/index.md) | MIME Part One: Content-Type values, Content-Transfer-Encoding tokens, and the quoted-printable and base64 encodings. |
| **RFC 2046** | [Communication](../packages/communication/index.md) | MIME Part Two: top-level media types and the `charset` parameter. |
| **RFC 2047** | [Communication](../packages/communication/index.md) | MIME Part Three: encoded words, which carry text that isn't US-ASCII in a message header. |
| **RFC 2231** | [Communication](../packages/communication/index.md) | MIME parameter value and encoded word extensions: character sets, languages and continuations. |
| **RFC 2397** | [Communication](../packages/communication/index.md) | The `data:` URL scheme. |
| **RFC 2978** | [Communication](../packages/communication/index.md) | IANA charset registration procedures: the syntax of character set names. |
| **RFC 4289** | [Communication](../packages/communication/index.md) | MIME Part Four: Content-Transfer-Encoding extension tokens. |
| **RFC 4648** | [Communication](../packages/communication/index.md) | The strict base64 of §4, used by iCalendar BINARY values and "B" encoded words. |
| **RFC 6657** | [Communication](../packages/communication/index.md) | The default `charset` of textual media types. |
| **RFC 6838** | [Communication](../packages/communication/index.md) | Media type naming rules and registration trees. |
| **RFC 6839** | [Communication](../packages/communication/index.md) | Structured syntax suffixes for media types, such as `+json` and `+xml`. |
| **RFC 4180** | [File.CSV](../packages/file-csv/index.md) | Common format and MIME type for Comma-Separated Values (CSV) files. |
| **RFC 5545** | [File.iCal](../packages/file-ical/index.md) | Internet Calendaring and Scheduling Core Object Specification (iCalendar): `.ics` files, their components and properties, and recurrence rules. |
| **RFC 6868** | [File.iCal](../packages/file-ical/index.md) | Parameter value encoding in iCalendar and vCard (caret encoding of quotes and line breaks in parameter values). |
| **RFC 7986** | [File.iCal](../packages/file-ical/index.md) | New properties for iCalendar, such as `NAME`, `COLOR`, `IMAGE` and `REFRESH-INTERVAL`. |
| **RFC 7265** | [File.iCal](../packages/file-ical/index.md) | jCal: the JSON format for iCalendar. |
| **RFC 6321** | [File.iCal](../packages/file-ical/index.md) | xCal: the XML format for iCalendar. |
| **RFC 5322** | [File.InternetMessage](../packages/file-internet-message/index.md) | Internet Message Format: header fields, addresses, date-times and message identifiers, in `.eml` files. |
| **RFC 6532** | [File.InternetMessage](../packages/file-internet-message/index.md) | Internationalized email headers: UTF-8 in field bodies. |
| **RFC 6854** | [File.InternetMessage](../packages/file-internet-message/index.md) | Group syntax in the From and Sender header fields. |
| **RFC 2045, RFC 2046** | [File.InternetMessage](../packages/file-internet-message/index.md) | MIME header fields, multipart bodies, embedded messages and transfer decoding in a message. The values and codecs come from [Communication](../packages/communication/index.md). |
| **RFC 2047** | [File.InternetMessage](../packages/file-internet-message/index.md) | Encoded words in display names, keyword phrases, comments and unstructured fields. |
| **RFC 2183** | [File.InternetMessage](../packages/file-internet-message/index.md) | The Content-Disposition header field. |
| **RFC 2231** | [File.InternetMessage](../packages/file-internet-message/index.md) | Extended parameters of the Content-Type and Content-Disposition fields. |
| **RFC 2387** | [File.InternetMessage](../packages/file-internet-message/index.md) | The `multipart/related` content type. |
| **RFC 2392** | [File.InternetMessage](../packages/file-internet-message/index.md) | `cid:` URIs and the parts they refer to. |
| **RFC 2557** | [File.InternetMessage](../packages/file-internet-message/index.md) | MHTML: web pages saved with their resources, in `.mht` and `.mhtml` files. |
| **RFC 3282** | [File.InternetMessage](../packages/file-internet-message/index.md) | The Content-Language header field. |
| **RFC 4155** | [File.InternetMessage](../packages/file-internet-message/index.md) | The mbox format, in the mboxrd, mboxo, mboxcl and mboxcl2 variants. |
| **EMLX** | [File.InternetMessage](../packages/file-internet-message/index.md) | The message files of Apple Mail, which have no published standard. The package follows the layout that open-source readers agree on. |
| **ISO 3166-1** | [Geography](../packages/geography/index.md) | Codes for the representation of names of countries — Part 1: Country code. |
| **ISO 3166-2** | [Geography](../packages/geography/index.md) | Codes for the representation of names of countries — Part 2: Country subdivision code. |
| **UN M49** | [Geography](../packages/geography/index.md) | Standard Country or Area Codes for Statistical Use (Series M, No. 49). |
| **ISO 639** (Parts 1, 2, 3, 5) | [Language](../packages/language/index.md) | Codes for the representation of names of languages (alpha-2, alpha-3, comprehensive coverage, and language families/groups). |
| **ISO 15924** | [Language](../packages/language/index.md) | Codes for the representation of names of scripts. |
| **BCP 47** | [LanguageTag](../packages/language-tag/index.md) | IETF language tags (as defined by RFC 5646). |
| **ISO 4217** (Tables A.1–A.3) | [Money](../packages/money/index.md) | Codes for the representation of currencies and funds — current codes, funds codes, and historic denominations. |
| **Money type** | [Money](../packages/money/index.md) | A monetary value combining an amount with an ISO 4217 currency code, after Martin Fowler's *Patterns of Enterprise Application Architecture*. |

## A note on DataStandardizer.Core

**DataStandardizer.Core** provides common types used to implement the standards
in the other packages. It is pulled in automatically as a dependency, so you
should not need to reference it directly.

## Where to next

- See [Platform support](platform-support.md) for the .NET / .NET Standard
  target matrix.
- Ready to use one? Head to [Get started](../get-started/index.md).
