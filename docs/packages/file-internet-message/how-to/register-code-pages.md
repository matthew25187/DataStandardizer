---
title: Read text in other character sets
parent: File.InternetMessage
grand_parent: Packages
nav_order: 8
---

# Read text in other character sets

Mail is often in a character set other than UTF-8, such as `windows-1252` or `Shift_JIS`. On
.NET Core and .NET 5 and later, those don't resolve until your application registers the code
pages, once, at startup:

```csharp
using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

Until it does, reading such a message works, and only decoding its text fails.
`ImfEntity.GetText` throws `ImfFileException`, because the character set can't be resolved.
The same character set in an RFC 2047 encoded word stays as ordinary text, as RFC 2047 §6.2 lets
a reader show a word it can't decode as it is.

`CodePagesEncodingProvider` is part of .NET Core 3.0 and later. On older runtimes, it's in the
[System.Text.Encoding.CodePages](https://www.nuget.org/packages/System.Text.Encoding.CodePages)
package, which you add to your application yourself. .NET Framework has the code pages already.
See [Register the code pages](../../communication/how-to/resolve-character-sets.md#register-the-code-pages)
in the Communication documentation.

## Use your own resolver

To control which character sets are available, or add your own, give `ImfOptions.CharsetResolver`
an `IMimeCharsetResolver`. The default is `MimeCharsetResolver.Default`. See
[Add your own character sets](../../communication/how-to/resolve-character-sets.md#add-your-own-character-sets).

```csharp
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.InternetMessage;

var options = new ImfOptions { CharsetResolver = new MimeCharsetResolver(new MyCharsetResolver()) };

using var reader = new ImfReader("message.eml", options);
```

## Text that isn't what it says

These are about text that names the wrong character set, or none. Each option is off by default:

| Problem | Option | With it |
| --- | --- | --- |
| A text body has octets that aren't valid in its character set. Old messages may send 8-bit text and name none, which means US-ASCII. | `AllowInvalidTextOctets` | `GetText` reads each such octet as U+FFFD. |
| A field body isn't valid UTF-8, such as raw Latin-1 in an old message. | `AllowEightBitHeaders` | The field is read, and `GetValue` decodes it with `HeaderFallbackCharset`, which is ISO-8859-1 by default. |
| An encoded word names a character set that can't be resolved, or holds octets that aren't valid in it. | `AllowMalformedEncodedWords` | The word is decoded as far as it can be. |

## Related

- [Read a message and its attachments](read-a-message.md)
- [Compliant defaults and the opt-ins](../concepts/compliant-defaults.md)
- [Resolve character sets](../../communication/how-to/resolve-character-sets.md)
