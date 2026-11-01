---
title: Use RFC 2231 parameter values
parent: Communication
grand_parent: Packages
nav_order: 3
---

# Use RFC 2231 parameter values

RFC 2231 extends media type parameters in two ways. A long value can be split into
numbered sections, and a value can carry a character set and a language.
`MimeMediaType` decodes both when it parses, and `MimeParameter` writes both.

## Read extended values

Parsing gives each parameter once, with its whole decoded value. These are the
examples from RFC 2231:

```csharp
using DataStandardizer.Communication.Mime;

// RFC 2231 §4: a character set and a language
var extended = MimeMediaType.Parse(
    "application/x-stuff;title*=us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A");

extended.TryGetParameter("title", out var title);

Console.WriteLine(title.Value);    // This is ***fun***
Console.WriteLine(title.Charset);  // us-ascii
Console.WriteLine(title.Language); // en-us

// RFC 2231 §3: a value split into sections
var split = MimeMediaType.Parse(
    "message/external-body;access-type=URL;" +
    "URL*0=\"ftp://\";URL*1=\"cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar\"");

split.TryGetParameter("url", out var url);

Console.WriteLine(url.Value); // ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar
```

When a parameter is given both as `name` and in an RFC 2231 form such as `name*`,
the RFC 2231 form is used. Extended values are decoded with
`MimeCharsetResolver.Default`, or with a resolver you pass to `Parse` or `TryParse`.
See [Resolve character sets](resolve-character-sets.md).

## Write extended values

A value of printable US-ASCII characters, spaces and tabs, with no language, is
written as a token or a quoted-string. Any other value, and any value with a
language, is written as an RFC 2231 extended value in UTF-8.

```csharp
new MimeParameter("name", "two words").ToString();      // name="two words"
new MimeParameter("title", "£ rates").ToString();       // title*=UTF-8''%C2%A3%20rates
new MimeParameter("title", "£ rates", "en").ToString(); // title*=UTF-8'en'%C2%A3%20rates
```

A parsed parameter is written in UTF-8 too, whatever character set it was read in.
`Charset` still tells you what that character set was. `Charset` and `Language` are
empty strings when there is none.

The language must be an RFC 1766 tag, as RFC 2231 §4 calls for, so its subtags are 1
to 8 letters. A later BCP 47 tag with other subtags, such as `es-419`, throws
`ArgumentException`.

## Split a long value

`FormatSections` splits a parameter into sections of at most a given length, to keep
the lines of a header field within 78 characters. Write each section as a separate
parameter after a `;`, and fold the header field between them.

```csharp
var note = new MimeParameter("title", "£ rates and more text here", "en");

foreach (var section in note.FormatSections(30))
{
    Console.WriteLine(section);
}

// title*0*=UTF-8'en'%C2%A3%20rat
// title*1*=es%20and%20more%20tex
// title*2*=t%20here
```

A character, and the octets of its encoding, are never split between sections. The
length doesn't count the `;` or the whitespace before a section. `MimeMediaType`'s
own `ToString` doesn't split values, so use `FormatSections` when you write a header
field whose line length matters.

## Accept values that break the rules

By default, a parameter that breaks the RFC 2231 rules makes the whole media type
invalid. Mail clients do write such values, so each can be accepted with a
[MimeMediaTypeParseOptions](../reference/MimeMediaTypeParseOptions.md) flag:

| Flag | Accepts | Rule it relaxes |
| --- | --- | --- |
| `AllowMissingSections` | Sections with gaps, such as `title*0` and `title*2`. | RFC 2231 §3 |
| `AllowDuplicateSections` | Two sections of the same number. The first is used. | RFC 2231 §3 |
| `AllowLeadingZeroSectionNumbers` | A section number such as `title*01`. | RFC 2231 §3 |
| `AllowUnknownCharsets` | A character set the resolver doesn't resolve. Each octet is read as the character with the same number, so you can recover the octets and decode them yourself. | RFC 2231 §4 |
| `AllowQuotedExtendedValues` | An extended value written as a quoted-string. | RFC 2231 §7 |
| `AllowLowercaseHexDigits` | `%c3%a9` in an extended value. | RFC 2231 §7 |
| `AllowInvalidOctets` | Octets that aren't valid in the character set. Each invalid sequence is read as U+FFFD. | RFC 2231 §4 |

`AllowNonstandardSections` combines the first three. `AllowNonstandardExtendedValues`
combines `AllowQuotedExtendedValues` and `AllowLowercaseHexDigits`.

```csharp
MimeMediaType.TryParse("a/b;t*0=x;t*2=y", out _); // false

var joined = MimeMediaType.Parse(
    "a/b;t*0=x;t*2=y",
    MimeMediaTypeParseOptions.AllowNonstandardSections);

Console.WriteLine(joined); // a/b;t=xy
```

> **Applies to:** the `netstandard1.1` build can't change how an encoding treats
> invalid octets. So there, without `AllowInvalidOctets`, a value is accepted only if
> writing the decoded text with the encoding gives the same octets back. A valid value
> in a character set that can write a character in more than one way needs the option
> on that build.

## See also

- [MimeParameter](../reference/MimeParameter.md) in the API reference.
