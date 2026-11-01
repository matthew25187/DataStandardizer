---
title: Resolve character sets
parent: Communication
grand_parent: Packages
nav_order: 5
---

# Resolve character sets

`MimeCharset` holds a character set name (RFC 2978 §2.3). `MimeCharsetResolver` turns
one into a `System.Text.Encoding`.

## Work with names

A name is kept as written, and names are compared case-insensitively. It isn't
checked against the IANA registry, so an unregistered name such as `x-mac-roman` is
valid. An alias is a different name:

```csharp
using DataStandardizer.Communication.Mime;

Console.WriteLine(MimeCharset.Parse("utf-8") == MimeCharset.Utf8);      // True
Console.WriteLine(MimeCharset.Parse("latin1") == MimeCharset.Iso88591); // False, although both resolve to the same encoding
```

## Find the character set of some text

Text is in the character set that the `charset` parameter of its media type names.
With no such parameter, `TryGetDefault` gives the default for the media type:

```csharp
var mediaType = MimeMediaType.Parse("text/plain");

MimeCharset charset;
if (mediaType.TryGetParameter("charset", out var parameter))
{
    charset = MimeCharset.Parse(parameter.Value);
}
else if (!MimeCharset.TryGetDefault(mediaType, out charset))
{
    throw new InvalidOperationException("The character set of the text isn't known.");
}

Console.WriteLine(charset); // US-ASCII
```

`TryGetDefault` ignores the parameters of the media type. It gives these results:

| Media type | Default | Standard |
| --- | --- | --- |
| `text/calendar`, `text/vcard` | `UTF-8` | RFC 5545 §8.1, RFC 6350 §10.1 |
| `text/html`, `text/xml` | None: the text gives the character set | HTML Living Standard §16.1.2, RFC 7303 §3.2 |
| `text/markdown` | None: the `charset` parameter is required | RFC 7763 §2 |
| Any other `text` type | `US-ASCII` | RFC 2046 §4.1.2, RFC 6657 §3 |
| Any other type | None | |

## Get the encoding

```csharp
using System.Text;

if (MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("latin1"), out var encoding))
{
    var text = encoding.GetString(bytes);
}
```

A character set is resolved by the first of these that knows its name:

1. **The built-in encodings:** `US-ASCII`, `ISO-8859-1`, `UTF-8`, `UTF-16`,
   `UTF-16BE`, `UTF-16LE`, `UTF-32`, `UTF-32BE` and `UTF-32LE`, and their aliases in
   the IANA registry, such as `latin1` and `csUTF8`. They give the same result on every
   target and runtime.
2. **`Encoding.GetEncoding(string)`**, which knows the encodings of the runtime and
   of any registered `EncodingProvider`.
3. **The resolver you passed** to the `MimeCharsetResolver` constructor, if any.

A character set that none of these knows isn't resolved, and it's up to you whether
that's an error. The machine's default encoding, `Encoding.Default`, is never used.

The built-in encodings don't throw. They read invalid bytes as U+FFFD, and write a
character that the character set can't hold as `?` (`US-ASCII` and `ISO-8859-1`) or
U+FFFD (the Unicode encodings). `UTF-16` and `UTF-32` are read as big-endian unless the
bytes start with a byte order mark, and are written as big-endian with no byte order
mark. `UTF-8` is written with no byte order mark.

## Register the code pages

The encodings that a runtime provides depend on the runtime, not on the target
framework of the package. .NET Framework has the Windows code pages, such as
`windows-1252` and `Shift_JIS`. .NET Core and .NET 5 and later have only the Unicode
encodings, `US-ASCII` and `ISO-8859-1`. So on those runtimes, `windows-1252` doesn't
resolve until your application registers the code pages, once, at startup:

```csharp
using System.Text;

MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("windows-1252"), out _); // false

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Parse("windows-1252"), out _); // true
```

`CodePagesEncodingProvider` is part of .NET Core 3.0 and later. On older runtimes,
it's in the
[System.Text.Encoding.CodePages](https://www.nuget.org/packages/System.Text.Encoding.CodePages)
package, which *DataStandardizer.Communication* doesn't depend on, so add it to your
application yourself.

`UTF-7` isn't available on .NET 5 and later, so there it resolves only through a
resolver you supply.

## Add your own character sets

Implement `IMimeCharsetResolver`, and pass it to the `MimeCharsetResolver` constructor.
It's asked only about the character sets that the default resolver doesn't resolve.

```csharp
using System.Diagnostics.CodeAnalysis;
using System.Text;

public sealed class MyCharsetResolver : IMimeCharsetResolver
{
    public bool TryGetEncoding(MimeCharset charset, [NotNullWhen(true)] out Encoding? encoding)
    {
        if (charset.Name.Equals("x-mine", StringComparison.OrdinalIgnoreCase))
        {
            encoding = Encoding.UTF8; // your own encoding here
            return true;
        }

        encoding = null;
        return false;
    }
}

var resolver = new MimeCharsetResolver(new MyCharsetResolver());
```

The parse methods of `MimeMediaType` and `MimeEncodedWord` take a resolver too, for
decoding text in other character sets.

> **Applies to:** the `[NotNullWhen(true)]` attribute and `Encoding?` are the form of the
> interface in the `net8.0` and `net10.0` builds. In the .NET Standard builds, the
> method is `bool TryGetEncoding(MimeCharset charset, out Encoding encoding)`.

## See also

- [MimeCharset](../reference/MimeCharset.md) and
  [MimeCharsetResolver](../reference/MimeCharsetResolver.md) in the API reference.
