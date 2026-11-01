---
title: Communication
parent: Packages
nav_order: 2
has_children: true
---

# DataStandardizer.Communication

Strongly-typed support for communication-related data standards: ITU-T E.164
international telephone numbers, and MIME media types, transfer encodings, character
sets and other building blocks of internet messages.

```shell
dotnet add package DataStandardizer.Communication
```

## Standards

| Standard | What it provides |
| --- | --- |
| **ITU-T E.164** | The international public telecommunication numbering plan — international telephone numbers for geographic areas, global services, networks, groups of countries, and trials, with parsing and culture-aware formatting. |
| **RFC 2045**, **RFC 2046** | MIME: media types and their parameters, and the `base64` and `quoted-printable` transfer encodings. |
| **RFC 2047** | Encoded words, which carry text that isn't US-ASCII in a message header. |
| **RFC 2231** | Parameter values that are split into sections or have a character set and language. |
| **RFC 2397** | `data:` URIs. |
| **RFC 2978**, **RFC 6657** | Character set names, and the default character set of a text media type. |
| **RFC 4289** | Extension transfer encodings. |
| **RFC 4648** | The strict base64 of §4, used by iCalendar BINARY values and "B" encoded words. |
| **RFC 6838**, **RFC 6839** | Media type naming rules, registration trees and structured syntax suffixes. |

The MIME types are new in version 2.0, and are in the
`DataStandardizer.Communication.Mime` namespace.

## Platform support

Targets .NET Standard 1.1 and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes (.NET 8 and .NET 10).

## How-to guides

- [Use E.164 international numbers](how-to/use-international-numbers.md)
- [Use media types](how-to/use-media-types.md)
- [Use RFC 2231 parameter values](how-to/use-rfc-2231-parameters.md)
- [Encode and decode with base64 and quoted-printable](how-to/encode-and-decode-bodies.md)
- [Resolve character sets](how-to/resolve-character-sets.md)
- [Use data URIs](how-to/use-data-uris.md)
- [Use encoded words](how-to/use-encoded-words.md)

## Concepts

- [The E.164 number model](concepts/e164-number-model.md)
- [The MIME layer](concepts/mime-layer.md)

## Reference

- [API reference](reference/index.md)
