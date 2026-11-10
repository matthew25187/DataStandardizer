namespace DataStandardizer.Geography.Tests;

/// <summary>
/// ISO 639 language codes shared by the tests of the ISO 3166 and UN M49 name lookups.
/// </summary>
internal static class Iso639TestCodes
{
    /// <summary>
    /// The ISO 639-1 and ISO 639-2/T codes for English in lower, upper and mixed case, which RFC 5646 §2.1.1 makes equivalent.
    /// </summary>
    internal static readonly string[] EnglishCodesInAnyCase = { "en", "EN", "En", "eng", "ENG", "eNg" };

    /// <summary>
    /// "spa", the ISO 639-2 code for Spanish, with its "s" replaced by U+017F LATIN SMALL LETTER LONG S, whose upper case is "S": not an ISO
    /// 639 code, as those are made of the ASCII letters a-z (RFC 5646 §2.1).
    /// </summary>
    internal const string SpanishCodeWithLongS = "\x017Fpa";
}
