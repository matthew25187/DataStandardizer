---
title: Use encoded words
parent: Communication
grand_parent: Packages
nav_order: 7
---

# Use encoded words

A message header can hold only US-ASCII. RFC 2047 carries other text in encoded words,
such as `=?ISO-8859-1?Q?Andr=E9?=`. `MimeEncodedWord` reads and writes them.

## Decode the text of a header field

`DecodeText` reads a run of text that may hold encoded words:

```csharp
using DataStandardizer.Communication.Mime;

var subject = MimeEncodedWord.DecodeText("Re: =?UTF-8?Q?caf=C3=A9?= menu");

Console.WriteLine(subject); // Re: café menu
```

Whitespace between two encoded words is dropped (RFC 2047 §6.2). Anything else,
including a run that isn't a valid encoded word, is kept as it is, as RFC 2047 §6.1
says to do. `DecodeText` doesn't throw for bad words.

```csharp
MimeEncodedWord.DecodeText("=?ISO-8859-1?Q?a?= =?ISO-8859-1?Q?b?= c"); // ab c
MimeEncodedWord.DecodeText("=?x-foo?Q?=E9?=");                         // =?x-foo?Q?=E9?=
```

## Say where the text is

This type doesn't decide where an encoded word may appear. That's up to the grammar of
the header field, which you parse. RFC 2047 §5 allows encoded words in unstructured
text, in a comment and in a phrase, but never in a quoted-string, an addr-spec or a
parameter value. So pass only text in which they're allowed. In a comment, pass each run
of `ctext`, without the parentheses and quoted-pairs. In a phrase, pass the words, not
any quoted-strings.

The place also sets the characters that "Q"-encoded text may hold, which you choose with
`MimeEncodedWordContext`:

| Context | Place | Characters "Q"-encoded text may hold |
| --- | --- | --- |
| `Text` | Unstructured text, such as Subject (RFC 2047 §5(1)). | Any printable ASCII character other than `?`. |
| `Comment` | A comment (RFC 2047 §5(2)). | As `Text`, but not `(`, `)` or `"`. |
| `Phrase` | A phrase, such as the display name of an address (RFC 2047 §5(3)). | Only ASCII letters, digits, `!`, `*`, `+`, `-`, `/`, `=` and `_`. |

The overloads with no context use `Text`.

```csharp
var displayName = MimeEncodedWord.DecodeText(
    "=?ISO-8859-1?Q?Andr=E9?=",
    MimeEncodedWordContext.Phrase,
    MimeEncodedWordParseOptions.None,
    MimeCharsetResolver.Default);
```

## Read one encoded word

`Parse` and `TryParse` read one whole word:

```csharp
var word = MimeEncodedWord.Parse("=?ISO-8859-1?Q?Andr=E9?=");

Console.WriteLine(word.Text);     // André
Console.WriteLine(word.Charset);  // ISO-8859-1
Console.WriteLine(word.Encoding); // Q
Console.WriteLine(word);          // =?ISO-8859-1?Q?Andr=E9?=
```

A word may give the language of its text, as RFC 2231 §5 allows. `Language` is an empty
string when it doesn't.

```csharp
var named = MimeEncodedWord.Parse("=?US-ASCII*EN?Q?Keith_Moore?=");

Console.WriteLine(named.Text);     // Keith Moore
Console.WriteLine(named.Language); // EN
```

Two words are equal when their character sets, languages, encodings and texts are equal,
even when they're written differently, such as `=41` and `A`. Character sets and languages
are compared case-insensitively, and texts case-sensitively.

## Write text as encoded words

`EncodeText` writes text of any length, in UTF-8, in whichever of the "B" and "Q" encodings
is shorter. When they're the same length, it uses "Q".

```csharp
var words = MimeEncodedWord.EncodeText("Déjà vu", MimeEncodedWordContext.Text);

Console.WriteLine(string.Join(" ", words)); // =?UTF-8?B?RMOpasOgIHZ1?=
```

Each word is at most 75 characters (`MimeEncodedWord.MaxLength`), and a character or a
surrogate pair is never split between words. Spaces are encoded in the words, so the
text is read back whole when you write the words with whitespace between them. Fold the
header field between words to keep each line of it within the limits of RFC 2047 §2.

To give the language of the text, pass it as well. It must be an RFC 1766 tag.

```csharp
MimeEncodedWord.EncodeText("Déjà vu", MimeEncodedWordContext.Text, "fr");
// =?UTF-8*fr?B?RMOpasOgIHZ1?=
```

To choose the encoding, use a constructor. It throws `ArgumentException` if the text
doesn't fit in one word.

```csharp
new MimeEncodedWord("café", MimeEncodedWordEncoding.Q, MimeEncodedWordContext.Text).ToString();
// =?UTF-8?Q?caf=C3=A9?=

new MimeEncodedWord("café", MimeEncodedWordEncoding.B, MimeEncodedWordContext.Text).ToString();
// =?UTF-8?B?Y2Fmw6k=?=
```

## Accept words that break the rules

By default, a word must follow the RFC 2047 §2 grammar, be at most 75 characters long,
have a character set that the resolver resolves, and decode to octets that are valid in
that character set. `TryParse` rejects any other word, and `DecodeText` leaves it as it
is written. Many mail clients do break these rules, so each can be accepted with a
[MimeEncodedWordParseOptions](../reference/MimeEncodedWordParseOptions.md) flag:

| Flag | Accepts | Rule it relaxes |
| --- | --- | --- |
| `AllowLongWords` | A word longer than 75 characters. | RFC 2047 §2 |
| `AllowEmbeddedWords` | In `DecodeText`, a word with no whitespace beside it, such as `Re:=?UTF-8?Q?caf=C3=A9?=`. | RFC 2047 §5 |
| `AllowUnknownCharsets` | A character set the resolver doesn't resolve. Each octet is read as the character with the same number, so you can recover the octets and decode them yourself. | RFC 2047 §6.2 |
| `AllowLowercaseHexDigits` | `=c3=a9` in "Q"-encoded text. | RFC 2047 §4.2, RFC 2045 §6.7 |
| `AllowInvalidOctets` | Octets that aren't valid in the character set, such as `=?UTF-8?Q?=FF?=`. Each invalid sequence is read as U+FFFD. | RFC 2047 §6.3 |
| `AllowSplitCharacters` | In `DecodeText`, a character that one word ends partway through and the next words finish. | RFC 2047 §5 |

```csharp
MimeEncodedWord.DecodeText("Re:=?UTF-8?Q?caf=C3=A9?=");
// Re:=?UTF-8?Q?caf=C3=A9?=

MimeEncodedWord.DecodeText(
    "Re:=?UTF-8?Q?caf=C3=A9?=",
    MimeEncodedWordContext.Text,
    MimeEncodedWordParseOptions.AllowEmbeddedWords,
    MimeCharsetResolver.Default);
// Re:café
```

> **Applies to:** the `netstandard1.1` build can't change how an encoding treats invalid
> octets. So there, without `AllowInvalidOctets`, a word is accepted only if writing the
> decoded text with the encoding gives the same octets back. A valid word in a character
> set that can write a character in more than one way needs the option on that build.

## See also

- [MimeEncodedWord](../reference/MimeEncodedWord.md) in the API reference.
- [Resolve character sets](resolve-character-sets.md).
