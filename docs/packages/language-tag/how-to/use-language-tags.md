---
title: Use language tags
parent: LanguageTag
grand_parent: Packages
nav_order: 1
---

# Use language tags

The `Bcp47LanguageTag` type is an implementation of IETF language tags as defined
by BCP 47; it supports the composition and deconstruction of language tags from
strings or using a fluent-style builder.

## Create a language tag

You can create language tags using factory methods on the `Bcp47LanguageTag`
type. For example,

```csharp
Bcp47LanguageTag languageTag;
languageTag = Bcp47LanguageTag.Create("en");        // English
languageTag = Bcp47LanguageTag.Create("mas");       // Maasai
languageTag = Bcp47LanguageTag.Create("fr-CA");     // French as used in Canada
languageTag = Bcp47LanguageTag.Create("es-419");    // Spanish as used in Latin America
languageTag = Bcp47LanguageTag.Create("zh-Hans");   // Chinese written with Simplified script
```

A language tag created this way is checked for well-formedness only: it must match
the grammar of RFC 5646, and nothing is looked up. If it doesn't match, a
`LanguageTagFormatException` is thrown. See
[Well-formed, valid and the ISO code lists](../concepts/well-formed-and-valid.md) for
what each check covers and how to choose.

If you would rather avoid exceptions for invalid input, the type also provides
`TryCreate` overloads that return `false` instead of throwing.

## Check a tag without validating it

To check a tag's form without creating it, call `IsWellFormed`. It matches the grammar
and looks nothing up:

```csharp
var isWellFormed = Bcp47LanguageTag.IsWellFormed("Not_A_Valid_Tag"); // false
isWellFormed = Bcp47LanguageTag.IsWellFormed("zz");                  // true, though no language has this code
isWellFormed = Bcp47LanguageTag.IsWellFormed("de-1901-1901");        // true, though the variant is repeated
```

Subtags can also be checked. These methods check the shape of one subtag, and look
nothing up:

```csharp
bool hasShape;
hasShape = Bcp47LanguageTag.CheckPrimaryLanguageSubtag("en");    // two to eight letters
hasShape = Bcp47LanguageTag.CheckExtendedLanguageSubtag("xxx");  // three letters
hasShape = Bcp47LanguageTag.CheckScriptSubtag("Cyrl");           // four letters
hasShape = Bcp47LanguageTag.CheckRegionSubtag("419");            // two letters or three digits
hasShape = Bcp47LanguageTag.CheckVariantSubtag("1606nict");      // five to eight letters or digits, or a digit and three more
hasShape = Bcp47LanguageTag.CheckExtensionSubtag("u-Latn");      // a singleton and its subtags
hasShape = Bcp47LanguageTag.CheckPrivateUseSubtag("x-private");  // x and its subtags
```

To find out whether a subtag is an ISO code, use the enums in
`DataStandardizer.Language` and `DataStandardizer.Geography`, or the `ToIso…` methods of
a tag.

## Validate against a registry

Validity depends on the *IANA Language Subtag Registry*. You can load a copy from an
external source and use it to create a tag, or to check one:

```csharp
var subtagRegistry = SubtagRegistry.CreateFromFile("subtag_registry.txt");

var languageTag = Bcp47LanguageTag.Create("en", subtagRegistry);       // throws if it isn't valid
var isValid = Bcp47LanguageTag.IsValid("de-1901-1901", subtagRegistry); // false: the variant is repeated
isValid = Bcp47LanguageTag.Create("zz").IsValid(subtagRegistry);        // false: zz isn't in the registry
```

The registry can also answer questions about a subtag directly, such as whether
it is deprecated and what replaces it:

```csharp
using DataStandardizer.LanguageTag.Registry; // for SubtagRegistryRecordType

if (subtagRegistry.TryGetSubtag(SubtagRegistryRecordType.Region, "BU", out var region)
    && region.Deprecated != null)
{
    Console.WriteLine($"Use {region.PreferredValue} instead of BU.");
}
```

See [SubtagRegistry](../reference/SubtagRegistry.md) for the typed lists of
records and the other lookups.

## Use the ISO code lists

If you want more than the grammar and can't load a registry, opt in to the code lists
implemented by the other *Data Standardizer* packages:

```csharp
var languageTag = Bcp47LanguageTag.Create("en-NZ", LanguageTagConformance.IsoCodeLists);
var isListed = Bcp47LanguageTag.IsValid("zz", LanguageTagConformance.IsoCodeLists);  // false
isListed = Bcp47LanguageTag.IsValid("eng", LanguageTagConformance.IsoCodeLists);     // false: use en
```

This approximates validity, and has gaps: for example, it doesn't check variant
subtags. See [The ISO code lists](../concepts/well-formed-and-valid.md#the-iso-code-lists).

## Extract subtags

The subtags that comprise a language tag object can be extracted from the language
tag using properties for access to the raw subtag values, or methods for
conversion of the subtags to members of the relevant enums.

```csharp
var languageTag = Bcp47LanguageTag.Create("gsw-u-sd-chzh");
var primaryLanguageSubtag = languageTag.ToIso639Part3();
var extensionSubtags = languageTag.ExtensionSubtags;
```

For more on the parts that make up a tag, see
[Anatomy of a language tag](../concepts/language-tag-anatomy.md).

## Performance

Tags are checked by a parser that reads the tag once, from left to right. A
well-formed check looks at the shape of each subtag. The ISO code lists and registry
checks also look each subtag up. A few habits keep it fast:

- **Parse once, then keep the tag.** Creating a `Bcp47LanguageTag` parses the
  tag and keeps its subtags, so reading `PrimaryLanguageSubtag`, `RegionSubtag`
  and the other properties doesn't parse it again. If you use a tag more than
  once, keep the `Bcp47LanguageTag` rather than the string. The list
  properties (`ExtendedLanguageSubtags`, `VariantSubtags` and
  `ExtensionSubtags`) return the same read-only list on every read, so there is
  no need to copy them into a local first.
- **Check spans, not substrings.** `IsWellFormed`, `IsValid`, the
  `Check…Subtag` methods and `TryCreate` take a `ReadOnlySpan<char>` on every
  target, so you can check a tag inside a
  larger buffer without making a string of it first:

  ```csharp
  var header = "fr-CA;q=0.8".AsSpan();
  var tagPart = header.Slice(0, header.IndexOf(';'));
  if (Bcp47LanguageTag.TryCreate(tagPart, out var languageTag))
  {
      // languageTag is fr-CA
  }
  ```

- **Load a subtag registry once.** Loading the *IANA Language Subtag Registry*
  reads a file of about 800 KB, and the first validation against a loaded
  registry builds an index of its subtags. Both are one-off costs for that
  `SubtagRegistry` instance, so load it once and share it; it is safe to use
  from several threads.

Where allocation happens:

| Operation | Allocates |
| --- | --- |
| `IsWellFormed`, `IsValid` and the `Check…Subtag` methods | Nothing. |
| `TryCreate` with a `ReadOnlySpan<char>` that it rejects | Nothing. |
| `Create` or `TryCreate` with a tag it accepts | The subtag strings and the object that holds them. From a span, the tag string too. |
| `ExtendedLanguageSubtags`, `VariantSubtags`, `ExtensionSubtags` | A small read-only wrapper the first time a non-empty list is read; nothing after that. |
| The other subtag properties and `AsSpan()` | Nothing. |
| `ToString()` and the implicit conversion to `string` | Nothing; they return the tag string kept by the instance. |

The first use of the ISO code lists in a process builds them, once, which takes tens of
milliseconds. A well-formed check doesn't need them, so it has no such cost.

On `net10.0`, the ISO code lists and registry checks look subtags up in hash sets; the
other targets search sorted arrays. The results are the same on every target. To see
what this costs on each target, run the benchmark project below.

To measure the package on your own machine, or to compare a change, use the
[LanguageTag benchmark project](https://github.com/matthew25187/DataStandardizer/tree/master/benchmarks/DataStandardizer.LanguageTag.Benchmarks).
Its README explains how to run it and what each benchmark measures.

## Related

- [Create language tags using the builder](create-language-tags-using-builder.md)
- [Well-formed, valid and the ISO code lists](../concepts/well-formed-and-valid.md)
- [API reference](../reference/index.md)
