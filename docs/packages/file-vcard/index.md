---
title: File.vCard
parent: Packages
nav_order: 4.5
has_children: true
---

# DataStandardizer.File.vCard

Reading and writing of vCard data, in the `.vcf` format of vCard 4.0, 3.0 and 2.1 and
in its JSON (jCard) and XML (xCard) forms, with conversion between the versions.

```shell
dotnet add package DataStandardizer.File.vCard
```

## Standards

| Standard | What it provides |
| --- | --- |
| **RFC 6350** | *vCard Format Specification*: the content-line syntax, including groups, line folding and TEXT escaping; the typed values of §4; and the properties and parameters of §5 and §6, with their conformance rules. |
| **RFC 6868** | *Parameter Value Encoding in iCalendar and vCard*: caret encoding of double quotes and line breaks in parameter values. |
| **RFC 7095** | *jCard: The JSON Format for vCard*: reading and writing cards as JSON with `JcardReader` and `JcardWriter`, which aren't on `netstandard1.1`, and the jCard form of the typed values, `VcardValueFormat.Json`, which is on every target. See [Read and write jCard](how-to/read-write-jcard.md). |
| **RFC 6351** | *xCard: vCard XML Representation*: reading and writing cards as XML with `XcardReader` and `XcardWriter`, on every target, and the xCard form of the typed values, `VcardValueFormat.Xml`. See [Read and write xCard](how-to/read-write-xcard.md). |
| **vCard 2.1** | *The Electronic Business Card*: vCard 2.1 cards, including quoted-printable, `BASE64`, `CHARSET`, parameters with no name and the card an `AGENT` holds. See [Handle legacy character sets](how-to/handle-legacy-character-sets.md) and [Versions and canonical forms](concepts/versions-and-canonical-forms.md). |
| **RFC 2425, RFC 2426** | *vCard MIME Directory Profile*: vCard 3.0 cards, with their errata. See [Versions and canonical forms](concepts/versions-and-canonical-forms.md). |
| **RFC 2739** | *Calendar Attributes for vCard and LDAP*: `FBURL`, `CALADRURI`, `CAPURI` and `CALURI` in vCard 3.0. |
| **RFC 4770** | *vCard Extensions for Instant Messaging*: `IMPP` in vCard 3.0. |
| **RFC 9554** | *vCard Format Extensions for JSContact*: `CREATED`, `GRAMGENDER`, `LANGUAGE`, `PRONOUNS` and `SOCIALPROFILE`; the parameters `AUTHOR`, `AUTHOR-NAME`, `CREATED`, `DERIVED`, `LABEL`, `PHONETIC`, `PROP-ID`, `SCRIPT`, `SERVICE-TYPE` and `USERNAME`; the `TYPE` values `billing` and `delivery`; and the added components of N and ADR. |
| **RFC 6473, RFC 6869** | *vCard KIND:application* and *vCard KIND:device*: two more kinds of card. |
| **RFC 6474** | *vCard Format Extensions: Place of Birth, Place and Date of Death*: `BIRTHPLACE`, `DEATHPLACE` and `DEATHDATE`. |
| **RFC 6715** | *vCard Format Extensions: Representing vCard Extensions Defined by the Open Mobile Alliance (OMA) Converged Address Book (CAB) Group*: `EXPERTISE`, `HOBBY`, `INTEREST`, `ORG-DIRECTORY`, `LEVEL` and `INDEX`. |
| **RFC 8605** | *vCard Format Extensions: ICANN Extensions for the Registration Data Access Protocol (RDAP)*: `CONTACT-URI` and `CC`. |

The readers and writers follow these standards by default. A reader rejects a card
that breaks a MUST or MUST NOT rule, and a writer refuses to write one. A vCard 3.0
or 2.1 card is held to the rules of its own version. Each accommodation for common
non-compliant input is a separate option on [VcardOptions](reference/VcardOptions.md)
that you turn on by name. So a file that other software reads without complaint may
need one, for example `AcceptByteOrderMark` for a file saved with a byte order mark.
See [Standards compliance by default](concepts/standards-compliance.md).

JSContact itself (RFC 9553) isn't implemented, and nor are the vCard properties and
parameters of RFC 9555 that carry it. They are kept and written back like any other
property or parameter the package doesn't know.

## Platform support

Targets .NET Standard 1.1, 2.0 and 2.1, and .NET 10.

> **Applies to:** the async API and the file-path constructors need .NET Standard 2.0
> or later. jCard (`JcardReader` and `JcardWriter`) needs .NET Standard 2.0 or later
> too. On .NET Standard 1.1, read and write synchronously, from a `Stream`,
> `TextReader` or `TextWriter`. xCard is on every target. `VcardOptions` is a
> `record` on `net10.0`, so `with` expressions work there, and a plain `class` on the
> .NET Standard targets.

The package depends on [DataStandardizer.Communication](../communication/index.md) for
media types, character sets, base64 and quoted-printable, which brings in
**DataStandardizer.LanguageTag**, **DataStandardizer.Geography** and
**DataStandardizer.Language**.

See [Platform support](../../overview/platform-support.md) for the full matrix.

## How-to guides

- [Read and write .vcf files](how-to/read-write-vcf-files.md)
- [Read asynchronously](how-to/read-asynchronously.md)
- [Read with zero copies](how-to/read-zero-copy.md)
- [Convert between 4.0, 3.0 and 2.1](how-to/convert-versions.md)
- [Convert between .vcf, jCard and xCard](how-to/convert-formats.md)
- [Read non-compliant cards](how-to/read-non-compliant-cards.md)
- [Handle legacy character sets](how-to/handle-legacy-character-sets.md)
- [Read and write jCard](how-to/read-write-jcard.md)
- [Read and write xCard](how-to/read-write-xcard.md)

## Concepts

- [The layered model](concepts/layered-model.md)
- [The common model and the versioned interfaces](concepts/common-model.md)
- [Versions and canonical forms](concepts/versions-and-canonical-forms.md)
- [PREF, PID and ALTID](concepts/pref-pid-altid.md)
- [Standards compliance by default](concepts/standards-compliance.md)
- [The shared content-line engine](concepts/content-line-engine.md)

## Reference

- [API reference](reference/index.md)
