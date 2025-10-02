# Use language codes

ISO 639 language codes are available across several enums implementing parts 1, 2, 3, and 5.  As the ISO 639 standard does not define numeric code counterparts for each of the language codes, these enums have been implemented as standard value types in which each of the language codes is a member with a string value being the language code from the standard.

To access an ISO 639:1 language code, use the `Iso639Part1Language` enum.

    var englishLanguage = Iso639Part1Language.en;

For historical reasons, ISO 639:2 was implemented in two forms defining codes for both bibliographic and terminological purposes.  Both these forms of the standard are available as enums.  For example,

    var englishLanguage = Iso639Part2BLanguage.eng; // bibliographic code for English

or,

    var englishLanguage = Iso639Part2TLanguage.eng; // terminological code for English

Similarly, the language codes defined by ISO 639:3 are available in their own enum.

    var phuThaiLanguage = Iso639Part3Language.pht;

Language families and groups have an enum for ISO 639:5.

    var caucasianLanguages = Iso639Part5LanguageFamily.cau;