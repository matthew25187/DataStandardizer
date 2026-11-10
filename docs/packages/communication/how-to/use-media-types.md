---
title: Use media types
parent: Communication
grand_parent: Packages
nav_order: 2
---

# Use media types

`MimeMediaType` holds a media type and its parameters, as they appear in a
Content-Type header field (RFC 2045 §5.1), such as `text/plain;charset=UTF-8`.

All the MIME types are in the `DataStandardizer.Communication.Mime` namespace.

## Parse a media type

```csharp
using DataStandardizer.Communication.Mime;

var mediaType = MimeMediaType.Parse("text/plain;charset=UTF-8;format=flowed");

Console.WriteLine(mediaType.Type);             // text
Console.WriteLine(mediaType.Subtype);          // plain
Console.WriteLine(mediaType.Parameters.Count); // 2

if (mediaType.TryGetParameter("CHARSET", out var charset))
{
    Console.WriteLine(charset.Value);          // UTF-8
}
```

`Parse` throws `FormatException` when the value isn't valid. `TryParse` returns
`false` instead. Both have overloads that take a `string` or a
`ReadOnlySpan<char>`.

Parsing is strict by default. It follows the RFC 2045 §5.1 grammar, with no
whitespace or comments, and the names must follow the RFC 6838 §4.2 naming rules.
A common form such as `text/plain; charset=us-ascii` is therefore rejected, unless
you ask for it to be accepted:

```csharp
MimeMediaType.TryParse("text/plain; charset=us-ascii", out _); // false

var relaxed = MimeMediaType.Parse(
    "text/plain; charset=us-ascii",
    MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

Console.WriteLine(relaxed); // text/plain;charset=us-ascii
```

A real header field can also hold comments and folding. Remove them before you
parse: the type doesn't accept them under any option.

[MimeMediaTypeParseOptions](../reference/MimeMediaTypeParseOptions.md) lists each
accommodation and the rule it relaxes.

## Build a media type

Use a constructor, one of the static fields such as `MimeMediaType.TextPlain`, or
both. The type is immutable, so each `With` method returns a copy.

```csharp
var html = new MimeMediaType(MimeTopLevelTypes.Text, "html")
    .WithParameter("charset", "UTF-8");

Console.WriteLine(html);                                        // text/html;charset=UTF-8
Console.WriteLine(html.WithParameter("charset", "ISO-8859-1")); // text/html;charset=ISO-8859-1
Console.WriteLine(html.WithoutParameter("charset"));            // text/html
```

A value that isn't a token is written as a quoted-string:

```csharp
var mixed = MimeMediaType.MultipartMixed.WithParameter("boundary", "a b");

Console.WriteLine(mixed); // multipart/mixed;boundary="a b"
```

A media type is written with no whitespace. One that was parsed keeps the case it
was written in.

## Compare media types

The type, subtype and parameter names are compared case-insensitively. Parameter
values are compared case-sensitively, except for the value of `charset`. The order
of the parameters doesn't matter.

```csharp
var left = MimeMediaType.Parse("TEXT/Plain;CHARSET=utf-8");
var right = MimeMediaType.Parse("text/plain;charset=UTF-8");

Console.WriteLine(left == right); // True
```

To compare only the type and subtype, remove the parameters first:

```csharp
var isPlainText = left.WithoutParameters() == MimeMediaType.TextPlain; // true
```

## Read the suffix and registration tree

`Suffix` gives the structured syntax suffix of the subtype (RFC 6839), or an empty
string if it has none. `Tree` gives the registration tree (RFC 6838 §3), which the
prefix of the subtype shows.

```csharp
Console.WriteLine(MimeMediaType.Parse("application/ld+json").Suffix);    // json
Console.WriteLine(MimeMediaType.Parse("application/ld+json").Tree);      // Standards
Console.WriteLine(MimeMediaType.Parse("application/vnd.ms-excel").Tree); // Vendor
Console.WriteLine(MimeMediaType.Parse("image/prs.btif").Tree);           // Personal
Console.WriteLine(MimeMediaType.Parse("application/x-tar").Tree);        // Unregistered
```

## See also

- [Use RFC 2231 parameter values](use-rfc-2231-parameters.md), for values that are
  long, not US-ASCII or have a language.
- [Resolve character sets](resolve-character-sets.md), for what to do with a
  `charset` parameter.
- [MimeMediaType](../reference/MimeMediaType.md) and
  [MimeParameter](../reference/MimeParameter.md) in the API reference.
