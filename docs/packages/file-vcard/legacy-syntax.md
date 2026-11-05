---
title: Legacy syntax and character sets
parent: File.vCard
grand_parent: Packages
nav_order: 10
---

# Legacy syntax and character sets

vCard 2.1 has lexical forms that later versions removed. The content-line reader and
writer handle them in a vCard 2.1 card, from its `VERSION:2.1` line up to and including
its `END:VCARD` line, with no option needed. Anywhere else they are off, because RFC 2426
and RFC 6350 don't define them.

## What a vCard 2.1 card can have

```text
BEGIN:VCARD
VERSION:2.1
TEL;HOME;VOICE:+1-213-555-1234
NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=ISO-8859-1:caf=E9 on the=
 corner
SOUND;WAVE;BASE64:
    SGVsbG8=

END:VCARD
```

| Form | What the reader does |
| --- | --- |
| A parameter with no name (`TEL;HOME`) | Gives it as the parameter it belongs to: here `TYPE=HOME`. `QUOTED-PRINTABLE`, `BASE64`, `7BIT` and `8BIT` belong to `ENCODING`. |
| A quoted-printable soft line break | Joins the lines, removing the `=` and the line break. What the next line starts with is data, including white space. |
| A `BASE64` value | Joins the lines up to the empty line that ends it, which it consumes. A line that can't be base64, such as `END:VCARD`, shows that the empty line is missing, and is an error, as is reaching the end of the data first. |
| A fold | Removes only the line break, keeping the white space (vCard 2.1 §2.1.3). See [Folds](#folds). |
| A nested card (`AGENT:` and `BEGIN:VCARD`) | Treats it as part of the card around it, which is still vCard 2.1 after its `END:VCARD`. |
| A backslash | Leaves it alone in `GetTextValue`, since it escapes only a semicolon in a compound value. |

Line numbers stay right: `PhysicalLineNumber` and `PhysicalLinesRead` count every line of
the source. `LegacySyntax` on a line or a view says which of these features the line uses.

A line before the `VERSION` line isn't known to be vCard 2.1, so it doesn't get these
forms.

vCard 2.1 §2.9 allows a name to be left out only for a `knowntype`, and the example in
§2.6.3 does so for an encoding. So `PHOTO;URL:` and `NOTE;UTF-8:` are errors, even in a
vCard 2.1 card, unless `AcceptLegacySyntax` is set.

## Decode a value

The reader doesn't decode a value until it is asked to. `Value` keeps the text as it
appears in the line, once joined.

```csharp
using DataStandardizer.File.vCard;

using var reader = new VcardContentLineReader("contacts.vcf");
foreach (var line in reader.ReadLines())
{
    if (line.Name == "NOTE")
    {
        Console.WriteLine(line.GetDecodedText());
    }
    else if (line.Name == "SOUND")
    {
        byte[] sound = line.GetDecodedValue();
    }
}
```

`GetDecodedValue` applies `ENCODING`: `QUOTED-PRINTABLE`, `BASE64` and, from vCard 3.0, `b`.
`GetDecodedText` then applies `CHARSET`. With no `CHARSET`, a value in a vCard 2.1 card is
US-ASCII (vCard 2.1 §2.1.6), and otherwise UTF-8. A value in a 2.1 card with characters
outside US-ASCII also needs `ENCODING=8BIT` (§2.1.5), unless
`AcceptEightBitWithoutEncoding` is set.

These parameters apply only where the version defines them. In a vCard 4.0 card
`ENCODING=QUOTED-PRINTABLE` and `CHARSET` are unknown parameters, so `GetDecodedText` gives
the value as it is.

An invalid value throws `VcardFileException`, including an octet that isn't valid in the
character set. It isn't replaced.

## Character sets

A `CHARSET` is resolved by [`MimeCharsetResolver`](../communication/how-to/resolve-character-sets.md),
then by `VcardOptions.CharsetResolver` if you set one. The built-in encodings work on every
target: US-ASCII, ISO-8859-1 and the UTF-8, UTF-16 and UTF-32 family.

Which other character sets exist depends on the runtime. .NET Framework has the Windows code
pages. .NET Core and .NET 5 and later don't, until the application registers them. See
[Resolve character sets](../communication/how-to/resolve-character-sets.md). The package doesn't
depend on `System.Text.Encoding.CodePages`.

A character set that can't be resolved throws `VcardFileException`. To decode it as
ISO-8859-1 instead, which never fails but may give the wrong characters, set
`AllowUnknownCharsetAsLatin1`, and check what happened:

```csharp
string text = line.GetDecodedText(out bool usedCharsetFallback);
```

## Raw 8-bit values

A stream or file is read as strict UTF-8 by default, so a raw 8-bit value in another
character set is a decoding error, and the reader can't go on. To read one, read the data
as ISO-8859-1, which gives each octet as one character:

```csharp
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.vCard;

MimeCharsetResolver.Default.TryGetEncoding(MimeCharset.Iso88591, out var latin1);
var options = new VcardOptions { Encoding = latin1 };
```

`GetDecodedText` then gets the octets back and decodes them by the line's `CHARSET`. `Value`,
and anything else you read directly, holds one character for each octet. A value with no
`CHARSET` in a later version's card isn't decoded again, so UTF-8 text in such a card reads
wrongly in this mode.

## Folds

The two rules for unfolding disagree:

| Rule | `NOTE:a`, line break, ` b` |
| --- | --- |
| vCard 2.1 §2.1.3, as RFC 822: a line break and white space are equivalent to the white space | `a b` |
| RFC 6350 §3.2: remove the line break and one white space character | `ab` |

The reader follows the specification of the card, so a vCard 2.1 card keeps the white space.
Many writers of 2.1 cards rely on the later rule, and `AcceptRfc6350FoldsInVersion21` reads
those. It doesn't affect soft line breaks or `BASE64` values.

## What the writer does

In a vCard 2.1 card, `VcardContentLineWriter`:

- never folds a line, since no fold is read the same way by every reader
- breaks a value with `ENCODING=QUOTED-PRINTABLE` with soft line breaks, so that no line reaches 76 characters (vCard 2.1 §2.1.3); a line whose group, name and parameters leave no room for that on its first line can't be written
- writes a value with `ENCODING=BASE64` on indented lines after the property's own, with the empty line that ends it
- writes every parameter with its name (`TYPE=HOME`), which vCard 2.1 §2.9 allows for all of them

The value must already be in its encoding, and the line can't be written if it isn't. Use
`MimeQuotedPrintable` or `MimeBase64` from `DataStandardizer.Communication` to encode it. The
writer doesn't produce these forms outside a vCard 2.1 card.

## Cards that break the rules

`AcceptLegacySyntax` gives every line the forms above, other than the folds, for cards that use
them where they don't belong, such as quoted-printable in a vCard 3.0 card. A line that ends
in `=` in a value with `ENCODING=QUOTED-PRINTABLE` is then always a soft line break. The option
also accepts a parameter with no name that vCard 2.1 doesn't allow, as a `VALUE` or `CHARSET` if
it is one and otherwise as a `TYPE`, and in `GetTextValue` a backslash that RFC 6350 §3.4 doesn't
define, such as `\:`.
