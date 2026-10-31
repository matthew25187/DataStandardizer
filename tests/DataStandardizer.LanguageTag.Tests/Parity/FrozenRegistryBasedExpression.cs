using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataStandardizer.LanguageTag.Tests.Parity
{
    /// <summary>
    /// The regular expression that validated language tags against a Subtag Registry before the parser replaced it (#223).
    /// </summary>
    /// <remarks>
    /// Frozen copy of the pattern the <c>Bcp47RegistryBased*ExpressionFactory</c> classes composed at commit c26161d, including the expansion of
    /// range records by <c>SubtagRegistrySubtagRange.Expand</c> (#239), with the options and <c>^...\z</c> anchoring of <c>Bcp47ExpressionCache</c>
    /// and the ASCII check of <c>Bcp47TagExpression</c>. The duplicate checks of <c>Bcp47LanguageTagParts.FromMatch</c> are
    /// <see cref="FrozenRulesBasedExpressions.IsWellFormed"/>. Do not change it to follow the package: it is the reference the parser is compared with.
    /// </remarks>
    internal sealed class FrozenRegistryBasedExpression
    {
        private const RegexOptions ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled;

        private const string ExtensionPattern = "(?!x)[0-9a-zA-Z](?:-[0-9a-zA-Z]{2,8})+";
        private const string PrivateUsePattern = "x(?:-[0-9a-zA-Z]{1,8})+";

        private const string FieldNameType = "Type";
        private const string FieldNameSubtag = "Subtag";
        private const string FieldNameTag = "Tag";

        private readonly Regex _languageTag;

        internal FrozenRegistryBasedExpression(SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            var primaryLanguageSubtagGroupPattern = $"(?<{FrozenRulesBasedExpressions.PrimaryLanguageGroupName}>{ComposeSubtagPattern(subtagRegistry, "language")})";
            var extendedLanguageSubtagGroupPattern = $"(?:(?:-(?<{FrozenRulesBasedExpressions.ExtendedLanguageGroupName}>{ComposeSubtagPattern(subtagRegistry, "extlang")})){{1,3}})";
            var scriptSubtagGroupPattern = $"(?:-(?<{FrozenRulesBasedExpressions.ScriptGroupName}>{ComposeSubtagPattern(subtagRegistry, "script")}))";
            var regionSubtagGroupPattern = $"(?:-(?<{FrozenRulesBasedExpressions.RegionGroupName}>{ComposeSubtagPattern(subtagRegistry, "region")}))";
            var variantSubtagGroupPattern = $"(?:(?:-(?<{FrozenRulesBasedExpressions.VariantGroupName}>{ComposeSubtagPattern(subtagRegistry, "variant")}))+)";
            var extensionSubtagGroupPattern = $"(?:(?:-(?<{FrozenRulesBasedExpressions.ExtensionGroupName}>{ExtensionPattern}))+)";
            var privateUseSubtagGroupPattern = $"(?:-(?<{FrozenRulesBasedExpressions.PrivateUseGroupName}>{PrivateUsePattern}))";

            var grandfatheredLanguageTags = GetValues(subtagRegistry, "grandfathered", FieldNameTag);
            var grandfatheredLanguageTagPattern = string.Join("|", grandfatheredLanguageTags.Select(Regex.Escape));
            var languageTagPattern = string.Concat("(?:",
                grandfatheredLanguageTagPattern, "|",
                "(?<", FrozenRulesBasedExpressions.PrivateUseGroupName, ">", PrivateUsePattern, ")|",
                primaryLanguageSubtagGroupPattern,
                extendedLanguageSubtagGroupPattern, "?",
                scriptSubtagGroupPattern, "?",
                regionSubtagGroupPattern, "?",
                variantSubtagGroupPattern, "?",
                extensionSubtagGroupPattern, "?",
                privateUseSubtagGroupPattern, "?)");

            _languageTag = new Regex($"^{languageTagPattern}\\z", ExpressionOptions);
        }

        /// <summary>
        /// Match a language tag as the registry-based overloads did; any input with a character outside the ASCII range was rejected before matching.
        /// </summary>
        internal Match Match(string input)
        {
            return FrozenRulesBasedExpressions.IsAscii(input) ? _languageTag.Match(input) : System.Text.RegularExpressions.Match.Empty;
        }

        private static string ComposeSubtagPattern(SubtagRegistry.SubtagRegistry subtagRegistry, string type)
        {
            return string.Join("|", GetValues(subtagRegistry, type, FieldNameSubtag).SelectMany(Expand).Select(Regex.Escape));
        }

        private static IEnumerable<string> GetValues(SubtagRegistry.SubtagRegistry subtagRegistry, string type, string fieldName)
        {
            return subtagRegistry
                .Where(record => record.Fields.Any(field => field.Key == FieldNameType && field.Value == type))
                .Select(record => record.Fields.Where(field => field.Key == fieldName).Select(field => field.Value).FirstOrDefault())
                .Where(value => value != null);
        }

        private static IEnumerable<string> Expand(string subtag)
        {
            var separatorIndex = subtag.IndexOf("..", StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return new[] { subtag };
            }

            var first = subtag.Substring(0, separatorIndex);
            var last = subtag.Substring(separatorIndex + 2);
            if (!IsRange(first, last))
            {
                return new[] { subtag };
            }

            return ExpandRange(first, last);
        }

        private static bool IsRange(string first, string last)
        {
            if (first.Length == 0 || first.Length != last.Length)
            {
                return false;
            }

            for (var index = 0; index < first.Length; index++)
            {
                var firstCharacter = char.ToLowerInvariant(first[index]);
                var lastCharacter = char.ToLowerInvariant(last[index]);
                if (!IsAsciiLetter(firstCharacter) && !IsAsciiDigit(firstCharacter)
                    || IsAsciiLetter(firstCharacter) != IsAsciiLetter(lastCharacter)
                    || IsAsciiDigit(firstCharacter) != IsAsciiDigit(lastCharacter))
                {
                    return false;
                }
            }

            return string.Compare(first, last, StringComparison.OrdinalIgnoreCase) <= 0;
        }

        private static IEnumerable<string> ExpandRange(string first, string last)
        {
            var current = first.ToCharArray();
            while (true)
            {
                var subtag = new string(current);
                yield return subtag;
                if (string.Equals(subtag, last, StringComparison.OrdinalIgnoreCase))
                {
                    yield break;
                }

                for (var index = current.Length - 1; index >= 0; index--)
                {
                    if (current[index] != GetHighest(current[index]))
                    {
                        current[index]++;
                        break;
                    }

                    current[index] = GetLowest(current[index]);
                }
            }
        }

        private static char GetLowest(char character) => character >= 'a' ? 'a' : character >= 'A' ? 'A' : '0';

        private static char GetHighest(char character) => character >= 'a' ? 'z' : character >= 'A' ? 'Z' : '9';

        private static bool IsAsciiLetter(char character) => character >= 'a' && character <= 'z';

        private static bool IsAsciiDigit(char character) => character >= '0' && character <= '9';
    }
}
