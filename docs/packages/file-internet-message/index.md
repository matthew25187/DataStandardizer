---
title: File.InternetMessage
parent: Packages
nav_order: 5
has_children: true
---

# DataStandardizer.File.InternetMessage

Reading and writing of Internet messages (email) in EML, MHTML, mbox and EMLX files:
one message format and the files that hold it. Everything is byte-based, and
anything you don't change is written back byte for byte.

```shell
dotnet add package DataStandardizer.File.InternetMessage
```

## Standards

| Standard | What it provides |
| --- | --- |
| **RFC 5322** | *Internet Message Format*: header fields, addresses, date-times, message identifiers, and the conformance checks of §3.6. The obsolete syntax of §4 is read, and never written. |
| **RFC 6532** | *Internationalized Email Headers*: UTF-8 in field bodies. |
| **RFC 6854** | *Update to Internet Message Format to Allow Group Syntax in the "From:" and "Sender:" Header Fields*. |
| **RFC 2045** | *MIME Part One*: the MIME-Version, Content-Type, Content-Transfer-Encoding, Content-ID and Content-Description fields, and transfer decoding. |
| **RFC 2046** | *MIME Part Two*: multipart bodies, embedded messages and boundaries. |
| **RFC 2047** | *MIME Part Three*: encoded words in display names, keyword phrases, comments and unstructured fields. |
| **RFC 2183** | *The Content-Disposition Header Field*. |
| **RFC 2231** | *MIME Parameter Value and Encoded Word Extensions*, in Content-Type and Content-Disposition parameters. |
| **RFC 2387** | *The MIME Multipart/Related Content-type*. |
| **RFC 2392** | *Content-ID and Message-ID Uniform Resource Locators*: `cid:` URIs. |
| **RFC 2557** | *MIME Encapsulation of Aggregate Documents, such as HTML (MHTML)*. |
| **RFC 3282** | *Content Language Headers*. |
| **RFC 4155** | *The application/mbox Media Type*: mbox files, in the mboxrd, mboxo, mboxcl and mboxcl2 variants. |

EMLX, the format of Apple Mail, has no published standard. The package follows the
layout that open-source readers agree on.

The readers and writers follow the standards by default. A reader rejects input that
breaks a rule, except where the standard requires a receiver to accept it, and a
writer refuses to write a message that breaks one. Each accommodation for common
non-compliant input is a separate option that you turn on by name. See
[Compliant defaults and the opt-ins](concepts/compliant-defaults.md).

The MIME values and codecs, such as `MimeMediaType` and `MimeCharset`, come from
[DataStandardizer.Communication](../communication/index.md), which NuGet installs
automatically.

The package doesn't send or receive mail, verify or create signatures, lock a
mailbox, or read Outlook `.msg` files.

## Platform support

Targets .NET Standard 1.1, 2.0 and 2.1, and .NET 10.

> **Applies to:** the async API and the file-path constructors need .NET Standard 2.0
> or later. On .NET Standard 1.1, read and write synchronously, from a `Stream`.
> `ImfOptions` is a `record` on `net10.0`, so `with` expressions work there, and a
> plain `class` on the .NET Standard targets.

See [Platform support](../../overview/platform-support.md) for the full matrix.

## How-to guides

- [Read a message and its attachments](how-to/read-a-message.md)
- [Stream a large message](how-to/stream-a-large-message.md)
- [Build and write a message](how-to/build-and-write-a-message.md)
- [Edit one header without breaking a signature](how-to/edit-a-header.md)
- [Open a saved web page](how-to/open-a-saved-web-page.md)
- [Read, filter and rewrite an mbox file](how-to/read-and-rewrite-mbox.md)
- [Read Apple Mail EMLX files](how-to/read-emlx.md)
- [Read text in other character sets](how-to/register-code-pages.md)

## Concepts

- [The layers](concepts/layers.md)
- [Byte-exact round trips](concepts/round-trips.md)
- [Compliant defaults and the opt-ins](concepts/compliant-defaults.md)
- [Limits for hostile input](concepts/hostile-input-limits.md)
- [One message format, several file formats](concepts/file-formats.md)

## Reference

- [API reference](reference/index.md)
