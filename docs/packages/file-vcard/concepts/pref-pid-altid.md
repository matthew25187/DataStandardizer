---
title: PREF, PID and ALTID
parent: File.vCard
grand_parent: Packages
nav_order: 13
---

# PREF, PID and ALTID

A card can have many instances of a property, such as several `TEL` or `EMAIL`
properties. Three parameters of RFC 6350 relate the instances to one another:

| Parameter | Says | RFC 6350 |
| --- | --- | --- |
| `PREF` | Which instance is preferred, from 1, the most preferred, to 100. | §5.3 |
| `ALTID` | Which instances are alternative forms of the same value, such as a title in two languages. | §5.4 |
| `PID` | Which instance this is, so that a copy of the card in another place can be matched with it. | §5.5, with `CLIENTPIDMAP` (§6.7.7) |

All three are vCard 4.0 only. vCard 3.0 and 2.1 have a weaker form of `PREF`, and
nothing like `ALTID` or `PID`.

## PREF

```csharp
using DataStandardizer.File.vCard;

var card = new VcardCard();
card.FormattedNames.Add("Jane Doe");
card.Emails.Add("jane@example.com", new VcardPropertyParameters { Pref = 1 });
card.Emails.Add("jane.doe@example.org", new VcardPropertyParameters { Pref = 2 });
card.Emails.Add("jd@example.net");

VcardProperty<string>? preferred = card.Emails.GetPreferred(); // jane@example.com
```

`GetPreferred` gives the first instance with the lowest `PREF`. An instance with no
`PREF` is least preferred, as RFC 6350 §5.3 says, so it is chosen only when no
instance has one. `Pref` on a property gives its value, or `null` if it has none
that is valid. `VcardCard.GetPreferred(name)` does the same for any property by
name, including one with no typed member, and gives the content line.

`VcardPropertyParameters.Pref` throws `ArgumentOutOfRangeException` for a value
outside 1 to 100. A card read with such a value is rejected with an
`InvalidParameter` diagnostic, unless `AllowInvalidValues` is set.

RFC 8605 §2.1 says a card with several `CONTACT-URI` properties gives them a
`PREF`, so a card that doesn't is rejected in the same way.

### PREF in vCard 3.0 and 2.1

vCard 3.0 says an instance is preferred with `TYPE=pref` (RFC 2426 §3.3.1), and
vCard 2.1 with the parameter `PREF` (§2.4.1). Neither has an order. So:

- Reading, `TYPE=pref` and `PREF` become `PREF=1`.
- Writing, the instances with the lowest `PREF` of their property get `TYPE=pref` or
  `PREF`, and the order among the others is lost. That is a `Lossy` diagnostic, so
  by default the writer throws, and `AllowLossyVersionConversion` leaves the order
  out.
- A vCard 3.0 property with no `TYPE`, such as `NOTE`, can't say it is preferred,
  so its `PREF` is lost too.

The versioned interfaces show `PREF` in the shape of their version, as
`IsPreferred` on [Vcard30PropertyParameters](../reference/Vcard30PropertyParameters.md)
and [Vcard21PropertyParameters](../reference/Vcard21PropertyParameters.md).

## ALTID

```csharp
using DataStandardizer.LanguageTag;

card.Titles.Add("Boss", new VcardPropertyParameters { AltId = "1", Language = Bcp47LanguageTag.Create("en") });
card.Titles.Add("Chef", new VcardPropertyParameters { AltId = "1", Language = Bcp47LanguageTag.Create("fr") });
card.Titles.Add("Treasurer");

foreach (var alternatives in card.Titles.GetAlternatives())
{
    // ["Boss", "Chef"], then ["Treasurer"]
}
```

Two instances are alternatives if and only if they have the same name and the same
`ALTID` (RFC 6350 §5.4). The same `ALTID` on another property means nothing, and
may be reused there. `GetAlternatives` groups the instances by `ALTID`, in the order
each group first appears, and an instance with no `ALTID` is a group of its own.
`VcardCard.GetAlternatives(name)` does the same by name.

Alternatives "count as 1 toward cardinality", so a card may have several `N`
properties with the same `ALTID`, though `N` may appear only once. The setters of
the properties that have one instance, such as `SetName`, replace every instance,
alternatives included, in the place of the first.

RFC 9554 §4.6 uses `ALTID` to tie a `PHONETIC` property to the one it says how to
pronounce, so the two must have the same `ALTID`. See
[The common model and the versioned interfaces](common-model.md#phonetic-values).

## PID and CLIENTPIDMAP

```csharp
card.ClientPidMaps.Add(new VcardClientPidMap(1, new Uri("urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b")));
card.Telephones.Add(new VcardTypedValue(new Uri("tel:+1-555-555-5555")),
    new VcardPropertyParameters { Pid = { new VcardPid(1, 1) } });

IReadOnlyList<VcardPid> pids = card.Telephones[0].Pid; // 1.1
```

A `PID` value is a local identifier, and optionally a source identifier after a
dot, as in `1.1`. A [VcardPid](../reference/VcardPid.md) holds both. The source
identifier is defined by a `CLIENTPIDMAP` property of the card, whose URI
identifies the client that assigned it (RFC 6350 §6.7.7). So a `PID` with a source
that no `CLIENTPIDMAP` defines is rejected with an `InvalidParameter` diagnostic,
unless `AllowInvalidValues` is set. `Pid` on a property gives the values that are
valid.

The package reads, keeps and writes `PID` and `CLIENTPIDMAP`. It doesn't merge two
copies of a card, as the synchronization of RFC 6350 §7 does, and it doesn't assign
`PID` values: that is up to the application that synchronizes the cards.

RFC 9554 §4.7 adds `PROP-ID`, a simpler identifier of a property among those of the
same name. Resolving a duplicate is "specific to the application", so `Validate()`
doesn't report one, and `GetDuplicatePropertyIds()` finds them.

## Converting to vCard 3.0 or 2.1

Neither version has `ALTID`, `PID` or `CLIENTPIDMAP`. Writing a card that has them as
vCard 3.0 or 2.1 throws `VcardFileException` with a `Lossy` diagnostic, and
`AllowLossyVersionConversion` leaves them out. `VcardVersionConverter.Analyze` lists
them without writing anything. See
[Versions and canonical forms](versions-and-canonical-forms.md#what-vcard-30-cant-hold).

## See also

- [The common model and the versioned interfaces](common-model.md)
- [VcardPropertyList](../reference/VcardPropertyList.md)
- [VcardProperty](../reference/VcardProperty.md)
- [VcardPropertyParameters](../reference/VcardPropertyParameters.md)
- [VcardPid](../reference/VcardPid.md)
- [VcardClientPidMap](../reference/VcardClientPidMap.md)
