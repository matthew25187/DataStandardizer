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
`IVcard40PropertyList<TValue>`.

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

`SetName` and `Addresses.Add` do the writing half, and `WithBackwardCompatibleComponents` does it for a value of your own. The RFC doesn't say how to combine the components of an address, so give the address a street address if the order matters: one it already has is left alone. A line added to `Properties` is written as it is. A card that was read keeps its values as they were.

```csharp
var name = VcardName.Parse("Doe;John;;;;Smith;Jr.".AsSpan(), VcardValueFormat.Text);
card.SetName(name); // N:Doe,Smith;John;;;Jr.;Smith;Jr.
```

### Phonetic values

A `PHONETIC` property says how to pronounce another of the same name, which has the same `ALTID` and no `PHONETIC` (§4.6). `Name` gives the one without it, whichever comes first.

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

None of this exists in vCard 3.0 or 2.1, and `VcardPropertyInfo.IsDefinedIn` says so. Writing an older version, which arrives with those versions, reports it as `Lossy`.

## Commas in URI values

RFC 6350 §3.4 says a comma in a value "MUST be escaped", and its verified errata 3845 and 3846 apply that to URI values, so a card has `GEO:geo:37.386013\,-122.082932`. A `System.Uri` in the model has no escaping, and a writer adds it. The RFC still prints some values with a bare comma and many writers write them, so `AcceptUnescapedCommasInUris` lets a reader accept them. A `GEO` parameter on `ADR` is a quoted string, to which this doesn't apply.

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

By default a reader rejects what RFC 6350 and its extensions don't allow, with their errata applied, and a writer refuses to write it. `Validate()`
returns every problem in a card as a `VcardDiagnostic`, without throwing.

| Rule | Reference | Accepted with |
| --- | --- | --- |
| `VERSION` immediately after `BEGIN` | §6.7.9 | `AllowVersionNotFirst`, reading only |
| A `VERSION`, and at least one `FN` | §6.7.9, §6.2.1 | `AllowMissingRequiredProperties` |
| A `CONTACT-URI` with a `mailto`, `http` or `https` URI, in a card that has others | RFC 8605 §2.1 | `AllowMissingRequiredProperties` |
| The cardinality of each property, and `MEMBER` only with `KIND:group` | §6, §6.6.5 | `AllowCardinalityViolations` |
| The syntax of each value, and `VALUE` only with a type the property allows | §4, §6 | `AllowInvalidValues` |
| A comma in a URI value escaped with a backslash | §3.4 and errata 3845 and 3846 | `AcceptUnescapedCommasInUris`, reading only |
| Parameters only on the properties that allow them, `PREF` from 1 to 100, and a `CLIENTPIDMAP` for each `PID` source | §5, §6, §6.7.7 | `AllowInvalidValues` |
| `LEVEL` by property, `INDEX` strictly positive, `CC` an ISO 3166 code on `ADR`, and a `PREF` among several `CONTACT-URI` properties | RFC 6715 §3, RFC 8605 | `AllowInvalidValues` |
| `SERVICE-TYPE` on a `SOCIALPROFILE` whose value is text, and `USERNAME` only with a URI | RFC 9554 §3.5, §4.10 | `AllowInvalidValues` |
| `GRAMGENDER` instances that differ in `LANGUAGE`, and the `TYPE` values `billing` and `delivery` only on `ADR` | RFC 9554 §3.2, §5 | `AllowInvalidValues` |
| `PHONETIC` only on `N` and `ADR`, with a registered value, an `ALTID` and a related property that has its components; `SCRIPT` with `PHONETIC=script`; and different `LANGUAGE` values among those with the same `ALTID` | RFC 9554 §4.6 | `AllowInvalidValues` |
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
