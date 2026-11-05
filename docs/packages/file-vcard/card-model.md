---
title: The card object model
parent: File.vCard
grand_parent: Packages
nav_order: 5
---

# The card object model

`VcardCard` is the object model of a card, and `VcardReader` and `VcardWriter` read and write it.

```csharp
using (var reader = new VcardReader("contacts.vcf"))
{
    foreach (var card in reader.ReadCards())
    {
        Console.WriteLine(card.FormattedNames[0].Value);
    }
}
```

Only vCard 4.0 can be read and written so far. vCard 3.0 and 2.1 are still to come, and the model is
designed for them, as this page describes.

## Common and versioned members

The versions of vCard are not additive. For example, vCard 4.0 has `KIND`, which earlier versions
don't, and vCard 3.0 has `MAILER`, which 4.0 doesn't. So the model has two halves.

**The common members** are the public members of `VcardCard` and `VcardProperty<TValue>`. They cover
every version, and each concept is held once, in a **canonical form**: the form of vCard 4.0 wherever
4.0 has an equivalent. A card loads without trouble whatever its version, and `SourceVersion` records
the version it was read from.

**The versioned interfaces** give one version's shape. Each is implemented explicitly on the same
object, so a view is not a copy, and a change through one is a change through the other.

```csharp
IVcard40Card version40 = card.AsVersion40();
version40.Telephones.Add(new VcardTypedValue(new Uri("tel:+1-555-555-5555")));
```

The rules of a versioned interface, which each version follows:

- It exposes only the members its version defines, in that version's shape.
- It doesn't inherit from the interface of another version.
- It doesn't expose the raw `Properties` list or content lines, so code written against it is checked by
  the compiler. Unknown and `X-` properties are reached through `ExtensionProperties`, and unknown
  parameters through `ExtensionParameters`.
- A getter gives the common value directly or adapts it, and a setter adapts back into the canonical
  form. For vCard 4.0 the canonical form is its own, so its members pass the value through as it is.
- It hides what its version can't show. Reading is never lossy, so any card can be viewed as any
  version, including one read from another version.

Property wrappers and lists have versioned interfaces in the same way: `IVcard40Property<TValue>` and
`IVcard40PropertyList<TValue>`.

## The open list of properties

`VcardCard.Properties` holds every property in order, with its group and parameters, including unknown
and `X-` properties, and including any whose value isn't valid. The typed members read and change this
list, so nothing is held twice. It doesn't hold `BEGIN`, `VERSION` or `END`, which a reader takes in and
a writer writes.

A typed member leaves out a property whose value isn't valid, which it can only have if
`AllowInvalidValues` let it be read, or if it was added to `Properties` directly. It also leaves out one
with a `CALSCALE` other than `gregorian`, since RFC 6350 §5.8 says to ignore it.

## Preference and alternatives

```csharp
VcardContentLine preferred = card.GetPreferred(VcardPropertyNames.Telephone);
var byAltId = card.GetAlternatives(VcardPropertyNames.Title);
```

`GetPreferred` gives the first instance with the lowest `PREF`. An instance without one is least
preferred. `GetAlternatives` groups instances by `ALTID`. Two instances are alternatives if and only if
they have the same name and the same `ALTID`, and an `ALTID` may be reused on another property
(RFC 6350 §5.4). `VcardPropertyList<TValue>` has both methods as well, with typed values.

## Groups

`KIND:group` cards list their members with `MEMBER`, and `ResolveMembers` looks them up by `UID` in a set
of cards. A member may be something other than a card, such as a `mailto` URI, and nothing outside the
set is looked up.

```csharp
var cards = reader.ReadCards().ToList();
var members = group.ResolveMembers(cards);
```

## Conformance

By default a reader rejects what RFC 6350 doesn't allow, and a writer refuses to write it. `Validate()`
returns every problem in a card as a `VcardDiagnostic`, without throwing.

| Rule | Reference | Accepted with |
| --- | --- | --- |
| `VERSION` immediately after `BEGIN` | §6.7.9 | `AllowVersionNotFirst`, reading only |
| A `VERSION`, and at least one `FN` | §6.7.9, §6.2.1 | `AllowMissingRequiredProperties` |
| The cardinality of each property, and `MEMBER` only with `KIND:group` | §6, §6.6.5 | `AllowCardinalityViolations` |
| The syntax of each value, and `VALUE` only with a type the property allows | §4, §6 | `AllowInvalidValues` |
| Parameters only on the properties that allow them, `PREF` from 1 to 100, and a `CLIENTPIDMAP` for each `PID` source | §5, §6, §6.7.7 | `AllowInvalidValues` |
| No forms of vCard 2.1 or 3.0: a parameter with no name, `ENCODING` or `CHARSET` | §5, Appendix A | `AcceptLegacySyntax`, reading only |

Each option is separate, and off by default. Nothing accepts a `BEGIN` or `END` that isn't paired, a
group or parameter on them, or a version that can't be read.

A problem in a card that has been read in full doesn't stop the reader: the card's exception can be
caught, and reading goes on with the next card.

## See also

- [VcardCard](reference/VcardCard.md)
- [IVcard40Card](reference/IVcard40Card.md)
- [VcardReader](reference/VcardReader.md)
- [VcardWriter](reference/VcardWriter.md)
- [VcardOptions](reference/VcardOptions.md)
