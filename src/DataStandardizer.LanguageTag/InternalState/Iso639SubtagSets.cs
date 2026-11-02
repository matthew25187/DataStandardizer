using System.Collections.Generic;
using System.Linq;
using DataStandardizer.Core;
using DataStandardizer.Language;

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// The ISO 639 codes that may be Primary Language subtags (ref. RFC 5646 §2.2.1).
    /// </summary>
    /// <remarks>
    /// RFC 5646 §2.2.1: "When languages have both an ISO 639-1 two-character code and a three-character code (assigned by ISO 639-2, ISO 639-3, or ISO 639-5), only the
    /// ISO 639-1 two-character code is defined in the IANA registry." So <c>eng</c>, which is in both ISO 639-2 and ISO 639-3, is left out of both sets here.
    /// The codes to leave out come from the ISO 639-1 enum, which has about 180 members. Asking each of the thousands of ISO 639-3 members for its ISO 639-1 code instead
    /// would scan the enum's fields once per member. An integration test checks that the two give the same answer.
    /// </remarks>
    internal static class Iso639SubtagSets
    {
        /// <summary>
        /// Serbo-Croatian. The ISO 639-3 data links it to <c>sh</c>, which is not an ISO 639-1 code, so <see cref="Iso639Part1Language"/> rightly has no member to find it through.
        /// The IANA registry has <c>sh</c> and not <c>hbs</c>, and this opt-in approximates validity against that registry, so <c>hbs</c> is left out.
        /// </summary>
        private const string SerboCroatianPart3Code = "hbs";

        private static readonly HashSet<string> Part2TCodesOfPart1Languages =
            new HashSet<string>(StringEnum.GetValues<Iso639Part1Language>().Select(language => language.GetPart2TCode()).OfType<string>());

        /// <summary>
        /// Alpha-2 codes from ISO 639-1 (ref. RFC 5646 §2.2.1¶1).
        /// </summary>
        internal static IEnumerable<string> Part1 => StringEnum.GetNames<Iso639Part1Language>();

        /// <summary>
        /// Alpha-3 codes from ISO 639-2/T of languages with no ISO 639-1 code (ref. RFC 5646 §2.2.1¶2A).
        /// </summary>
        internal static IEnumerable<string> Part2TWithoutPart1Equivalent => StringEnum.GetNames<Iso639Part2TLanguage>().Where(code => !Part2TCodesOfPart1Languages.Contains(code));

        /// <summary>
        /// Alpha-3 codes from ISO 639-3 of languages with no ISO 639-1 code (ref. RFC 5646 §2.2.1¶2B).
        /// </summary>
        internal static IEnumerable<string> Part3WithoutPart1Equivalent => StringEnum.GetNames<Iso639Part3Language>().Where(code => !Part2TCodesOfPart1Languages.Contains(code) && code != SerboCroatianPart3Code);

        /// <summary>
        /// Alpha-3 codes from ISO 639-5, which are language families and so have no ISO 639-1 code (ref. RFC 5646 §2.2.1¶2C).
        /// </summary>
        internal static IEnumerable<string> Part5 => StringEnum.GetNames<Iso639Part5LanguageFamily>();

        /// <summary>
        /// Alpha-3 codes from <c>qaa</c> to <c>qtz</c>, reserved for local use (ref. RFC 5646 §2.2.1¶3).
        /// </summary>
        internal static IEnumerable<string> LocalUse
        {
            get
            {
                for (var secondCharacter = 'a'; secondCharacter <= 't'; secondCharacter++)
                for (var thirdCharacter = 'a'; thirdCharacter <= 'z'; thirdCharacter++)
                {
                    yield return string.Concat("q", secondCharacter.ToString(), thirdCharacter.ToString());
                }
            }
        }
    }
}
