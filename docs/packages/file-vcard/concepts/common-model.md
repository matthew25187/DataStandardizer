---
title: Common model and versioned interfaces
parent: File.vCard
grand_parent: Packages
nav_order: 11
---

# The common model and the versioned interfaces

`VcardCard` is the object model of a card, the top layer of the package (see [The layered model](layered-model.md)), and `VcardReader` and `VcardWriter` read and write it.

```csharp
using (var reader = new VcardReader("contacts.vcf"))
{
    foreach (var card in reader.ReadCards())
    {
        // FN is required from vCard 3.0, but a vCard 2.1 card can have N alone.
        Console.WriteLine(card.FormattedNames.GetPreferred()?.Value ?? "(no name)");
    }
}
```

vCard 4.0, 3.0 and 2.1 can be read and written. See [Versions and canonical forms](versions-and-canonical-forms.md).

## Common and versioned members

The versions of vCard are not additive. For example, vCard 4.0 has `KIND`, which earlier versions
don't, and vCard 3.0 has `MAILER`, which 4.0 doesn't. So the model has two halves.

**The common members** are the public members of `VcardCard` and `VcardProperty<TValue>`. They cover
every version, and each concept is held once, in a **canonical form**: the form of vCard 4.0 wherever
4.0 has an equivalent. A card of a version that can be read loads into these members, and
`SourceVersion` records the version it was read from.

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
`IVcard40PropertyList<TValue>`, `IVcard30Property<TValue>` and `IVcard30PropertyList<TValue>`, and `IVcard21Property<TValue>` and
`IVcard21PropertyList<TValue>`. `AsVersion30()` gives the [IVcard30Card](../reference/IVcard30Card.md) view, and `AsVersion21()` the
[IVcard21Card](../reference/IVcard21Card.md) view.

## The open list of properties

`VcardCard.Properties` holds every property in order, with its group and parameters, including unknown
and `X-` properties, and including any whose value isn't valid. The typed members read and change this
list, so nothing is held twice. It doesn't hold `BEGIN`, `VERSION` or `END`, which a reader takes in and
a writer writes.

A typed member leaves out a property whose value isn't valid, which it can only have if
`AllowInvalidValues` let it be read, or if it was added to `Properties` directly. It also leaves out one
with a `CALSCALE` other than `gregorian`, since RFC 6350 §5.8 says to ignore it.

## Extensions

The extensions of vCard 4.0 have members of their own, and aren't in `ExtensionProperties`. None of them exists in vCard 3.0 or 2.1.

| Standard | What it adds | Members |
| --- | --- | --- |
| RFC 6473, RFC 6869 | `KIND:application`, `KIND:device` | `VcardKind.Application`, `VcardKind.Device` |
| RFC 6474 | `BIRTHPLACE`, `DEATHPLACE`, `DEATHDATE` | `BirthPlace`, `DeathPlace`, `DeathDate` |
| RFC 6715 | `EXPERTISE`, `HOBBY`, `INTEREST`, `ORG-DIRECTORY`, and the parameters `LEVEL` and `INDEX` | `Expertise`, `Hobbies`, `Interests`, `OrganizationDirectories`, and `Level` and `Index` on a property |
| RFC 8605 | `CONTACT-URI`, and the parameter `CC` on `ADR` | `ContactUris`, and `CountryCode` on a property |
| RFC 9554 | `CREATED`, `GRAMGENDER`, `LANGUAGE`, `PRONOUNS`, `SOCIALPROFILE`, and ten parameters | `Created`, `GrammaticalGenders`, `DefaultLanguage`, `Pronouns`, `SocialProfiles`, and members on a property such as `ServiceType` and `Phonetic` |

```csharp
var hobbies = card.Hobbies.GetOrderedByIndex();
VcardLevel? level = hobbies[0].Level;
Iso3166Part1Alpha2Country? country = card.Addresses[0].CountryCode;
```

The `LANGUAGE` property of RFC 9554 is `DefaultLanguage`, because `Languages` is `LANG`, the languages that may be used for contact. JSContact itself (RFC 9553) isn't implemented, and the `JSPROP` property and the `JSPTR` and `JSCOMPS` parameters of RFC 9555 are kept and written back like any unknown property or parameter.

### The components of N and ADR

RFC 9554 adds components to `N` and `ADR`, and says what to do so that software that doesn't know them can still read a card.

| | `N` (§2.2) | `ADR` (§2.1) |
| --- | --- | --- |
| **Writing** | A secondary surname also goes in the family names, and a generation in the honorific suffixes. | The new components are combined in the street address. |
| **Reading** | Ignore a value in those older components that is also in the new one: `FamilyNamesExcludingSecondarySurnames`, `HonorificSuffixesExcludingGenerations`. | Ignore the street address if `HasDetailedComponents` is `true`. |

`SetName` and `Addresses.Add` do the writing half, and `WithBackwardCompatibleComponents` does it for a value of your own. The RFC doesn't say how to combine the components of an address, so give the address a street address if the order matters: one it already has is left alone. A line added to `Properties` is written as it is, and so is a value with a `PHONETIC` parameter, because §4.6 needs its related property to have a value in each of its components. A card that was read keeps its values as they were.

```csharp
var name = VcardName.Parse("Doe;John;;;;Smith;Jr.".AsSpan(), VcardValueFormat.Text);
card.SetName(name); // N:Doe,Smith;John;;;Jr.;Smith;Jr.
```

### Phonetic values

A `PHONETIC` property says how to pronounce another of the same name, which has the same `ALTID` and no `PHONETIC` (§4.6). `Name` gives the one without it, whichever comes first. `SetName` replaces every `N`, so to write a pair, add both lines to `Properties`.

```csharp
foreach (var alternatives in card.Addresses.GetAlternatives())
{
    var phonetic = alternatives.FirstOrDefault(address => address.Phonetic != null);
}
```

### Derived values and identifiers

`DERIVED=true` says that a value is worked out from other properties, and RFC 9554 §4.4 says a client "MUST NOT update the property". So a setter such as `SetName` throws `InvalidOperationException` on one. Software that works the value out again removes the instance first, with `RemoveProperties`.

`PROP-ID` identifies a property among those of the same name. An application "SHOULD assign a unique identifier", and resolving a duplicate is "specific to the application" (§4.7), so `Validate()` doesn't report one. `GetDuplicatePropertyIds()` finds them.

### Writing to an older version

None of this exists in vCard 3.0 or 2.1, and `VcardPropertyInfo.IsDefinedIn` says so. Writing vCard 3.0 reports it as `Lossy`; see [Versions and canonical forms](versions-and-canonical-forms.md).

## Commas in URI values

RFC 6350 §3.4 says a comma in a value "MUST be escaped", and its verified errata 3845 and 3846 apply that to URI values, so a card has `GEO:geo:37.386013\,-122.082932`. A `System.Uri` in the model has no escaping, and a writer adds it. The RFC still prints some values with a bare comma and many writers write them, so `AcceptUnescapedCommasInUris` lets a reader accept them. A `GEO` parameter on `ADR` is a quoted string, to which this doesn't apply.

## Preference and alternatives

`GetPreferred` and `GetAlternatives` find the preferred instance of a property and the alternatives among its instances. See [PREF, PID and ALTID](pref-pid-altid.md).

## Groups

`KIND:group` cards list their members with `MEMBER`, and `ResolveMembers` looks them up by `UID` in a set
of cards. A member may be something other than a card, such as a `mailto` URI, and nothing outside the
set is looked up.

```csharp
var cards = reader.ReadCards().ToList();
var members = group.ResolveMembers(cards);
```

## Conformance

`Validate()` returns every problem in a card as a `VcardDiagnostic`. A problem in the card never throws, but an exception from a `CharsetResolver` of your own, which can be asked to resolve a `CHARSET` as the card is checked, isn't caught, and `Validate(version)` checks a card by the rules of a version. The rules, and the options that accept a card that breaks one, are in [Standards compliance by default](standards-compliance.md).

## See also

- [The layered model](layered-model.md)
- [Versions and canonical forms](versions-and-canonical-forms.md)
- [PREF, PID and ALTID](pref-pid-altid.md)
- [Standards compliance by default](standards-compliance.md)
- [VcardCard](../reference/VcardCard.md)
- [IVcard40Card](../reference/IVcard40Card.md)
- [IVcard30Card](../reference/IVcard30Card.md)
- [IVcard21Card](../reference/IVcard21Card.md)
