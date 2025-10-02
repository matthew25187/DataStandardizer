# Access language metadata

There is metadata associated with each of the members of the ISO 639 enums.  Extension methods are available to retrieve this metadata by members of the enums.

## English names

A language code may have one or more English names.  To get a single English name:

    var englishName = Iso639Part1Language.tl.GetEnglishName();

Some language codes have multiple English names.

    var englishNames = Iso639Part1Language.nl.GetEnglishNames();

These are methods available on the `Iso639Part1Language`, `Iso639Part2BLanguage`, `Iso639Part2TLanguage`, `Iso639Part3Language` and `Iso639Part5LanguageFamily` enums.

## French names

A language code may have one or more French names.  To get a single French name:

    var frenchName = Iso639Part1Language.de.GetFrenchName();

Some language codes have multiple French names.

    var frenchNames = Iso639Part1Language.nn.GetFrenchNames();

These are methods available on the `Iso639Part1Language`, `Iso639Part2BLanguage`, `Iso639Part2TLanguage` and `Iso639Part5LanguageFamily` enums.

## Inverted name

ISO 639:3 defines inverted names for its language codes.  To get an inverted name:

    var invertedName = Iso639Part3Language.est.GetInvertedName();

## Language type

Language types are included in the ISO 639:3 standard.  To get a language type from a language code:

    var languageType = Iso639Part3Language.slk.GetLanguageType();

Language types indicate whether the language is living, ancient, historical, extinct, constructed or special.

## Macrolanguage

Macrolanguages are defined as part of the ISO 639:3 standard.  To get a macrolanguage type from a language code:

    var macrolanguage = Iso639Part3Language.pst.GetMacrolanguageCode();

## Part 1 code

The ISO 639 standard associates part 1 codes with other language codes.  To get the part 1 code for a language code:

    var part1Code = Iso639Part3Language.ita.GetPart1Code();

Part 1 codes are associated, where available, with language codes from the `Iso639Part2BLanguage`, `Iso639Part2TLanguage` and `Iso639Part3Language` enums.

## Part 2 code

The ISO 639 standard associates part 2 codes with other language codes.  Associated part 2 codes are available in both bibliographic and terminological forms.

To get the bibliographic part 2 code associated with a language code:

    var part2Code = Iso639Part1Language.fj.GetPart2BCode();

Similarly, terminological part 2 codes associated with a language code can be retrieved like so:

    var part2Code = Iso639Part1Language.fj.GetPart2TCode();

Part 2 codes are associated, where available, with language codes from the `Iso639Part1Language` and `Iso639Part3Language` enums.

## Print name

Print names are defined by the ISO 639:3 standard and can be retrieved like so:

    var printName = Iso639Part3Language.tat.GetPrintName();

## Scope

ISO 639:3 defines a scope for its language codes.  You can retrieve the scope of a language from a `Iso639Part3Language` language code:

    var scope = Iso639Part3Language.heb.GetScope();