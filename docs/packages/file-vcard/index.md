---
title: File.vCard
parent: Packages
nav_order: 4.5
has_children: true
---

# DataStandardizer.File.vCard

Reading and writing of vCard data, either as cards or at the level of its content lines. The
package is a preview under development: vCard 4.0 and 3.0 cards can be read and written so far, and
vCard 2.1 cards, jCard and xCard are still to come. See
[The card object model](card-model.md) and [Converting between versions](version-conversion.md).

## Standards

| Standard | What it provides |
| --- | --- |
| **RFC 6350** | *vCard Format Specification*: the content-line syntax, including groups, line folding and TEXT escaping; the typed values of §4; and the properties and parameters of §5 and §6, with their conformance rules. |
| **RFC 6868** | *Parameter Value Encoding in iCalendar and vCard*: caret encoding of double quotes and line breaks in parameter values. |
| **vCard 2.1** | *The Electronic Business Card*: the lexical syntax, including quoted-printable, `BASE64`, `CHARSET` and parameters with no name. See [Legacy syntax and character sets](legacy-syntax.md). |
| **RFC 2425, RFC 2426** | *vCard MIME Directory Profile*: vCard 3.0 cards, with their errata. See [Converting between versions](version-conversion.md). |
| **RFC 2739** | *Calendar Attributes for vCard and LDAP*: `FBURL`, `CALADRURI`, `CAPURI` and `CALURI` in vCard 3.0. |
| **RFC 4770** | *vCard Extensions for Instant Messaging*: `IMPP` in vCard 3.0. |
| **RFC 9554** | *vCard Format Extensions for JSContact*: `CREATED`, `GRAMGENDER`, `LANGUAGE`, `PRONOUNS` and `SOCIALPROFILE`; the parameters `AUTHOR`, `AUTHOR-NAME`, `CREATED`, `DERIVED`, `LABEL`, `PHONETIC`, `PROP-ID`, `SCRIPT`, `SERVICE-TYPE` and `USERNAME`; the `TYPE` values `billing` and `delivery`; and the added components of N and ADR. |
| **RFC 6473, RFC 6869** | *vCard KIND:application* and *vCard KIND:device*: two more kinds of card. |
| **RFC 6474** | *vCard Format Extensions: Place of Birth, Place and Date of Death*: `BIRTHPLACE`, `DEATHPLACE` and `DEATHDATE`. |
| **RFC 6715** | *vCard Format Extensions: Representing vCard Extensions Defined by the Open Mobile Alliance (OMA) Converged Address Book (CAB) Group*: `EXPERTISE`, `HOBBY`, `INTEREST`, `ORG-DIRECTORY`, `LEVEL` and `INDEX`. |
| **RFC 8605** | *vCard Format Extensions: ICANN Extensions for the Registration Data Access Protocol (RDAP)*: `CONTACT-URI` and `CC`. |

## Standards compliance

By default the reader accepts only what RFC 6350 and its extensions allow, with their errata
applied, and the writer writes only what they allow, except in a vCard 3.0 or 2.1 card, which follows its own specification. Each accommodation for common non-compliant input or output is a separate
option on [VcardOptions](reference/VcardOptions.md), off by default. So a file that other
software reads without complaint may need one, for example `AcceptByteOrderMark` for a
file saved with a byte order mark, or `AcceptUnescapedCommasInUris` for a `GEO:geo:1,2` that RFC 6350 §3.4
and its errata don't allow.

## Reference

See the [API reference](reference/index.md).
