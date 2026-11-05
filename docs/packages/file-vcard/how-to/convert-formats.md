---
title: Convert formats
parent: File.vCard
grand_parent: Packages
nav_order: 5
---

# Convert between .vcf, jCard and xCard

All three formats read into, and write from, the same card model, `VcardCard`. So to
convert, read with the reader for one format and write with the writer for another.
There is no separate converter.

| Format | Reader | Writer |
| --- | --- | --- |
| `.vcf` (RFC 6350, and vCard 3.0 and 2.1) | `VcardReader` | `VcardWriter` |
| jCard, JSON (RFC 7095) | `JcardReader` | `JcardWriter` |
| xCard, XML (RFC 6351) | `XcardReader` | `XcardWriter` |

> **Applies to:** `JcardReader` and `JcardWriter` need .NET Standard 2.0 or later.
> They aren't available on .NET Standard 1.1. The file-path constructors used below
> need .NET Standard 2.0 or later too; on .NET Standard 1.1, use a `Stream`, or for
> `.vcf` and xCard a `TextReader` or `TextWriter`.

## .vcf to jCard

```csharp
using DataStandardizer.File.vCard;

using (var reader = new VcardReader("contacts.vcf"))
using (var writer = new JcardWriter("contacts.json"))
{
    writer.WriteCards(reader.ReadCards());
}
```

## .vcf to xCard

```csharp
using (var reader = new VcardReader("contacts.vcf"))
using (var writer = new XcardWriter("contacts.xml"))
{
    writer.WriteCards(reader.ReadCards());
}
```

## jCard to .vcf

```csharp
using (var reader = new JcardReader("contacts.json"))
using (var writer = new VcardWriter("contacts.vcf"))
{
    writer.WriteCards(reader.ReadCards());
}
```

## xCard to jCard

```csharp
using (var reader = new XcardReader("contacts.xml"))
using (var writer = new JcardWriter("contacts.json"))
{
    writer.WriteCards(reader.ReadCards());
}
```

The other directions work the same way.

`ReadCards` reads one card at a time as `WriteCards` enumerates it, so a large
document doesn't have to fit in memory. The jCard and xCard writers finish the
document when they are disposed, so dispose the writer before you read the output.

## Older versions

jCard and xCard represent vCard 4.0 alone. A vCard 3.0 or 2.1 card read from `.vcf`
is written in the canonical form of vCard 4.0, as `VcardWriter` writes it by
default. Going the other way, a card read from jCard or xCard can be written as
vCard 3.0 or 2.1 by setting `WriteVersion` on the `VcardWriter`'s options. See
[Convert between 4.0, 3.0 and 2.1](convert-versions.md).

```csharp
var options = new VcardOptions { WriteVersion = VcardVersion.V3_0 };
using (var reader = new JcardReader("contacts.json"))
using (var writer = new VcardWriter("contacts.vcf", options))
{
    writer.WriteCards(reader.ReadCards());
}
```

## Convert asynchronously

Each writer's `WriteCardsAsync` accepts the `IAsyncEnumerable<VcardCard>` that a
reader's `ReadCardsAsync` returns:

```csharp
await using var reader = new VcardReader("contacts.vcf");
await using var writer = new JcardWriter("contacts.json");
await writer.WriteCardsAsync(reader.ReadCardsAsync(cancellationToken), null, cancellationToken);
```

See [Read asynchronously](read-asynchronously.md).

## Options and checks

Every reader and writer checks each card against RFC 6350 and its extensions,
whatever the format. A card that a reader accepts only with an option, such as
`AllowMissingRequiredProperties`, is refused by the writer unless you give it the
same option:

```csharp
var options = new VcardOptions { AllowMissingRequiredProperties = true };
using (var reader = new VcardReader("contacts.vcf", options))
using (var writer = new XcardWriter("contacts.xml", options))
{
    writer.WriteCards(reader.ReadCards());
}
```

The options for content lines, such as `AcceptBareLineFeed`, have no meaning for
jCard and xCard, which have no content lines. See
[Read non-compliant cards](read-non-compliant-cards.md).

## What a round trip keeps

Converting a vCard 4.0 card from one format to another and back keeps every
property, parameter and value. It doesn't keep:

- **Layout:** where `.vcf` lines were folded, XML white space, comments and namespace
  prefixes, or JSON formatting and the order of an object's keys.
- **Spelling:** a few values come back with the same meaning but spelled
  differently. Property and parameter names come back in upper case, TEXT escaping
  is applied again, so `\N` comes back as `\n`, BOOLEAN values come back in upper
  case, and the values of `TYPE`, `PID` and `SORT-AS` come back as one list, so
  `TYPE=work;TYPE=voice` comes back as `TYPE=work,voice`.
- **The `VALUE` parameter as written:** after a trip through jCard or xCard, a
  `VALUE` parameter comes back after the property's other parameters, and one that
  names the property's default type doesn't come back at all.

The full lists are in [Read and write jCard](read-write-jcard.md) and
[Read and write xCard](read-write-xcard.md), along with how each part of vCard maps
to each format.

## Related

- [Read and write .vcf files](read-write-vcf-files.md)
- [Read and write jCard](read-write-jcard.md)
- [Read and write xCard](read-write-xcard.md)
- [VcardReader](../reference/VcardReader.md), [JcardReader](../reference/JcardReader.md),
  [XcardReader](../reference/XcardReader.md), [VcardWriter](../reference/VcardWriter.md),
  [JcardWriter](../reference/JcardWriter.md) and [XcardWriter](../reference/XcardWriter.md)
