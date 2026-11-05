---
title: The layered model
parent: File.vCard
grand_parent: Packages
nav_order: 10
---

# The layered model

The package is built in three layers. Each layer can be used on its own, so you can
stop at the lowest layer that does the job. The lower layers are fast and avoid
allocation. The higher layers are easier to use and allocate by design.

All public types here are in the `DataStandardizer.File.vCard` namespace.

## The three layers

```text
              .vcf (4.0, 3.0, 2.1)            jCard (JSON)          xCard (XML)
                     │                             │                     │
Layer 1   VcardContentLineReader                   │                     │
          VcardContentLineWriter                   │                     │
          VcardContentLineView                     │                     │
          VcardContentLine                         │                     │
                     │                             │                     │
Layer 2   value types: VcardName, VcardAddress, VcardDateAndOrTime, VcardValue, ...
          (VcardValueFormat.Text)      (VcardValueFormat.Json)  (VcardValueFormat.Xml)
                     │                             │                     │
Layer 3   VcardReader / VcardWriter     JcardReader / JcardWriter   XcardReader / XcardWriter
                     │                             │                     │
                     └──────────────► VcardCard ◄──┴─────────────────────┘
                                          │
                     AsVersion40(), AsVersion30(), AsVersion21()
                     VcardVersionConverter
```

jCard and xCard sit beside Layer 1. They have no content lines, so `JcardReader`,
`JcardWriter`, `XcardReader` and `XcardWriter` map their documents straight to and
from the Layer 3 model. A card read from any of the three formats can be written in
any other. See [Convert between .vcf, jCard and xCard](../how-to/convert-formats.md).

## Layer 1: content lines

A vCard file is a sequence of content lines (RFC 6350 §3.3), such as
`TEL;TYPE=work:tel:+1-555-0100`. Layer 1 reads and writes these lines and nothing
more. It knows nothing of cards or properties, so a line may appear outside a card.

- `VcardContentLineReader` reads the lines of a stream, file or `TextReader`. It
  unfolds each line, splits it into a group, a name, parameters and a value, and
  checks that structure.
- `ReadContentLine` and `CurrentLine` give a `VcardContentLineView`, which hands out
  spans of the reader's own buffer without allocating. See
  [Read with zero copies](../how-to/read-zero-copy.md).
- `ReadLine` and `ReadLines` give a `VcardContentLine` for each line, a copy that
  stays valid after the reader moves on.
- `VcardContentLineWriter` writes lines, folding them at 75 octets.

Layer 1 follows the version of each card from its `VERSION` line, because the
versions differ even at this level. RFC 6868 caret encoding applies only in a
vCard 4.0 card, and a vCard 2.1 card has its own forms: parameters with no name,
quoted-printable soft line breaks, `BASE64` values that end at an empty line, and a
different rule for unfolding. See
[Handle legacy character sets](../how-to/handle-legacy-character-sets.md).

The same engine reads and writes the content lines of iCalendar in
**DataStandardizer.File.iCal**. See [The shared content-line engine](content-line-engine.md).

## Layer 2: values

The value types parse and format the values of RFC 6350 §4, and the structured
values of the properties that have them:

- `VcardValue` for TEXT, BOOLEAN, INTEGER, FLOAT, URI and LANGUAGE-TAG.
- `VcardDate`, `VcardTime`, `VcardDateTime`, `VcardDateAndOrTime`, `VcardTimestamp`
  and `VcardUtcOffset` for dates and times.
- `VcardName`, `VcardAddress`, `VcardOrganization`, `VcardGender`, `VcardGeo`,
  `VcardClientPidMap` and `VcardPid` for the structured values and parameters.
- `VcardValueTypeRegistry`, which says which value type a property has, and the
  `VALUE` parameter can change.

Each takes a `VcardValueFormat`: `Text` for `.vcf`, `Json` for jCard and `Xml` for
xCard, where the form differs. Some take a `VcardVersion` too, since vCard 3.0 and
2.1 write some values differently, such as `1985-04-12` for a date that vCard 4.0
writes as `19850412`. Parsing from a span into a struct doesn't allocate, and the
structured values have a view, such as `VcardName.TryParseView`, that reads their
components from a span without copying them.

Use Layer 2 with Layer 1 to read values without building a card, or on its own to
parse a value from elsewhere.

## Layer 3: the card model

`VcardReader` reads whole cards into `VcardCard`, and `VcardWriter` writes them.
`VcardCard` holds every property in order, in `Properties`, and has typed members
over that list, such as `FormattedNames`, `Telephones` and `Name`. A card read from
vCard 3.0 or 2.1 is held in the same canonical form as one read from vCard 4.0, and
`AsVersion30()` and `AsVersion21()` give it in the shape of an older version. See
[The common model and the versioned interfaces](common-model.md) and
[Versions and canonical forms](versions-and-canonical-forms.md).

Layer 3 checks each card against the rules of its version. A reader rejects a card
that breaks one, and a writer refuses to write it, unless an option on
`VcardOptions` accepts it. See [Standards compliance by default](standards-compliance.md).

## Choosing a layer

| You want to | Use |
| --- | --- |
| Read cards and work with their properties | `VcardReader` and `VcardCard` (Layer 3) |
| Write cards you have built or changed | `VcardWriter` (Layer 3) |
| Convert cards between vCard 4.0, 3.0 and 2.1 | `VcardWriter` with `WriteVersion`, or `VcardVersionConverter` (Layer 3) |
| Convert cards between `.vcf`, jCard and xCard | A reader of one format and a writer of the other (Layer 3) |
| Pick a few values out of a large file | `VcardContentLineReader` and `VcardContentLineView` (Layer 1), with the value types (Layer 2) |
| Copy or filter lines without interpreting them | `VcardContentLineReader` and `VcardContentLineWriter` (Layer 1) |
| Parse or format one value | The value types (Layer 2) |

## See also

- [The common model and the versioned interfaces](common-model.md)
- [The shared content-line engine](content-line-engine.md)
- [Read and write .vcf files](../how-to/read-write-vcf-files.md)
- [Read with zero copies](../how-to/read-zero-copy.md)
- [API reference](../reference/index.md)
