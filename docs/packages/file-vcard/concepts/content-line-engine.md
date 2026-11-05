---
title: The shared content-line engine
parent: File.vCard
grand_parent: Packages
nav_order: 15
---

# The shared content-line engine

vCard and iCalendar have the same kind of content line: a name, parameters and a
value, folded at 75 octets, with the same quoting of parameter values and the same
RFC 6868 caret encoding (RFC 6350 §3.3, RFC 5545 §3.1). So
**DataStandardizer.File.vCard** and **DataStandardizer.File.iCal** read and write
their content lines with one engine.

The engine is a Shared Project, `DataStandardizer.File.ContentLine.Shared`, that is
compiled into each package. Its types are internal, so it adds no assembly and no
dependency between the two packages, and neither exposes it. What you use is each
package's own Layer 1: `VcardContentLineReader` and `VcardContentLineWriter` here,
and `IcalContentLineReader` and `IcalContentLineWriter` in **File.iCal**. See
[The layered model](layered-model.md).

## What the engine does

**Reading.** The scanner finds the lines in a block of characters that it rents from
a pool. It unfolds each line in place, so a logical line is one run of the block,
and holds its group, name, parameters and value as slices of that run. That is how
a `VcardContentLineView` can hand out spans without copying anything. Caret
decoding and TEXT unescaping are done only when asked for, into a scratch buffer, so
the raw slices stay as they were. The scanner does no I/O: the reader refills the
block when it needs more, synchronously or asynchronously, and the same scanner
serves both.

**Repairing folds.** RFC 6350 §3.2 notes that a simple writer may fold a line in the
middle of a multi-octet UTF-8 character, and says a reader should put the character
back together. Before a stream is decoded, each such fold is moved to just after the
character, so the character decodes whole. A `TextReader` has already decoded the data, so it can't
be repaired there.

**Writing.** The formatter checks a line and measures it before writing anything, so
a line that can't be written leaves nothing behind. It then writes the line into a
pooled buffer, folded at `FoldLength` octets of UTF-8 without splitting a character,
with its line break, ready for one call to the underlying writer. A double quote or
a line break in a parameter value is caret-encoded where RFC 6868 applies. Elsewhere
it is an error, or, with `AllowLossyVersionConversion`, the double quote is left out
and the line break becomes a space.

**Errors.** Each package gives the engine a factory for its own exception, so a
problem the engine finds reaches you as a `VcardFileException`, with the line and
property where it was found.

## What this package adds

The engine is the same for both formats. What differs is in each package:

- **Versions.** vCard has three versions with different lexical rules, and iCalendar
  one. The vCard reader and writer follow the version of each card from its
  `BEGIN:VCARD` and `VERSION` lines. Caret encoding applies only in a vCard 4.0
  card, since RFC 6868 updates RFC 6350 and not RFC 2426 or vCard 2.1.
- **vCard 2.1.** The engine lets a package decide, at the end of each physical line,
  whether the logical line goes on. This package uses that for the two ways a
  vCard 2.1 value continues: a quoted-printable soft line break, and a `BASE64` value
  that ends at an empty line. A vCard 2.1 card also unfolds by its own rule, keeping
  the white space. The writer writes those forms, and doesn't fold a vCard 2.1 line.
  See [Handle legacy character sets](../how-to/handle-legacy-character-sets.md).
- **Groups.** A vCard property can have a group, such as `item1.EMAIL`, which
  iCalendar doesn't have.
- **Decoding.** `GetDecodedValue` and `GetDecodedText` apply `ENCODING` and
  `CHARSET` where the card's version defines them.

## What this means for you

The two packages behave the same where their standards are the same. The options
that control the engine have the same names and meanings in `VcardOptions` and
`IcalOptions`: `AcceptBareLineFeed`, `MaxLineLength`, `Encoding`, `FoldLength`,
`FoldWhitespace` and `LineBreak`. A fix in the engine reaches both packages, and its
release note says so in both.

## See also

- [The layered model](layered-model.md)
- [Read with zero copies](../how-to/read-zero-copy.md)
- [VcardContentLineReader](../reference/VcardContentLineReader.md)
- [VcardContentLineWriter](../reference/VcardContentLineWriter.md)
- [File.iCal: the layered model](../../file-ical/concepts/layered-model.md)
