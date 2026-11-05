---
title: Convert between versions
parent: File.vCard
grand_parent: Packages
nav_order: 4
---

# Convert between 4.0, 3.0 and 2.1

A card is held in the same canonical form whichever version it was read from, so
converting it to another version is a matter of writing it in that version. This
page shows how. For how each version maps to the model, and what each can't hold,
see [Versions and canonical forms](../concepts/versions-and-canonical-forms.md).

## Convert a file

`VcardOptions.WriteVersion` says which version a writer writes. It is vCard 4.0 by
default.

```csharp
using DataStandardizer.File.vCard;

var options = new VcardOptions { WriteVersion = VcardVersion.V3_0 };
using var reader = new VcardReader("version40.vcf");
using var writer = new VcardWriter("version30.vcf", options);
writer.WriteCards(reader.ReadCards());
```

A reader takes cards of every version, so the input can mix them, and `SourceVersion`
says which each was. To upgrade cards to vCard 4.0, read them and write them with the
default options:

```csharp
using var reader = new VcardReader("phone-export.vcf");
using var writer = new VcardWriter("contacts.vcf");
writer.WriteCards(reader.ReadCards());
```

Upgrading never loses anything. A property that vCard 4.0 doesn't define, such as
`MAILER`, is kept as it is.

## When the card holds more than the version can

vCard 3.0 and 2.1 can't hold everything vCard 4.0 can, such as `KIND`, `LANG` or a
`PID`. Writing such a card throws `VcardFileException` with a `Lossy` diagnostic,
and writes nothing of that card. This is the default because the writer would
otherwise drop data without saying so.

To see what would be lost, without writing anything:

```csharp
foreach (var item in VcardVersionConverter.Analyze(card, VcardVersion.V3_0))
{
    Console.WriteLine($"{item.Kind}: {item.PropertyName}: {item.Message}");
}
```

To write the card anyway, leaving out what the version can't hold, set
`AllowLossyVersionConversion`:

```csharp
var options = new VcardOptions
{
    WriteVersion = VcardVersion.V3_0,
    AllowLossyVersionConversion = true
};
```

`VcardVersionConverter.Convert` makes a copy of a card with only what a version can
hold, and its `SourceVersion` set to that version, without writing it. It takes the
same options, and without `AllowLossyVersionConversion` throws as a writer would.

```csharp
var copy = VcardVersionConverter.Convert(card, VcardVersion.V2_1, options);
```

## Required properties

Each version has its own required properties. vCard 4.0 requires `FN`, vCard 3.0
requires `N` and `FN` (RFC 2426 §5), and vCard 2.1 requires `N` (§2.2.2). A card from
vCard 4.0 with no `N` can't be written as vCard 3.0 or 2.1, and throws with a
`MissingRequiredProperty` diagnostic. Add an `N`, or set
`AllowMissingRequiredProperties`. No name is made up from `FN`.

A card can also be valid in an older version and break a rule of vCard 4.0. RFC 2426
allows two `BDAY` properties, for example, and RFC 6350 doesn't, so such a card
throws when it is written as vCard 4.0. `Validate(VcardVersion.V4_0)` lists what to
fix before you write it.

## Work in the shape of an older version

`AsVersion30()` and `AsVersion21()` give a card as an `IVcard30Card` or an
`IVcard21Card`, with only the members that version defines, in its shape. They are
views of the same object, not copies, so a change through one is a change to the
card. Use them to read or build a card in terms of the version you are working with,
and write it in any version.

```csharp
IVcard21Card version21 = card.AsVersion21();
version21.Telephones.Add("+1-555-0100", new Vcard21PropertyParameters { IsPreferred = true, Types = { "CELL" } });
```

## Options for writing vCard 2.1

`WriteNamedTypesInVersion21` writes each `TYPE` with its name, as
`TEL;TYPE=WORK;TYPE=VOICE:`, instead of alone, as `TEL;WORK;VOICE:`. Both are vCard
2.1, and some older software reads only one. See
[Versions and canonical forms](../concepts/versions-and-canonical-forms.md#writing-vcard-21)
for the other forms the writer uses.

## jCard and xCard

jCard and xCard represent vCard 4.0 alone. A card of an older version read from
`.vcf` is written in the canonical form of vCard 4.0 by `JcardWriter` and
`XcardWriter`, as it is by `VcardWriter` with the default options. See
[Convert between .vcf, jCard and xCard](convert-formats.md).

## Related

- [Versions and canonical forms](../concepts/versions-and-canonical-forms.md)
- [The common model and the versioned interfaces](../concepts/common-model.md)
- [Handle legacy character sets](handle-legacy-character-sets.md)
- [VcardVersionConverter](../reference/VcardVersionConverter.md),
  [IVcard30Card](../reference/IVcard30Card.md) and
  [IVcard21Card](../reference/IVcard21Card.md)
