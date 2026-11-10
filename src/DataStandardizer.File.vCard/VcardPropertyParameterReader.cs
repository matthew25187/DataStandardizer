using System;
using System.Collections.Generic;
using System.Globalization;
using DataStandardizer.Geography;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Reads the parameters of vCard 4.0 from a content line.
    /// </summary>
    internal static class VcardPropertyParameterReader
    {
        // ref. RFC 6350 §5.3: an integer between 1 and 100.
        internal const int MinPreference = 1;
        internal const int MaxPreference = 100;

        // ref. RFC 6350 §5.3: pref-param = "PREF=" (1*2DIGIT / "100")
        private const int MaxPreferenceDigits = 2;
        private const string MaxPreferenceText = "100";

        // ref. RFC 6715 §3.2: LEVEL-value, in the order of VcardLevel.
        private static readonly string[] LevelTexts = { "beginner", "average", "expert", "high", "medium", "low" };

        // ref. RFC 8605 §3.1: CC-param = "CC=" 2ALPHA
        private const int CountryCodeLength = 2;

        // ref. RFC 9554 §4.6 and §7.4: the values in the IANA registry, in the order of VcardPhoneticSystem.
        private static readonly string[] PhoneticTexts = { "ipa", "jyut", "piny", "script" };

        // ref. RFC 9554 §4.7: prop-id-param = "PROP-ID" "=" 1*255(ALPHA / DIGIT / "-"/ "_")
        private const int MaxPropertyIdLength = 255;

        // ref. RFC 9554 §4.8: script-param = 4ALPHA, a script subtag of RFC 5646 §2.2.3.
        private const int ScriptLength = 4;

        // ref. RFC 9554 §4.4: derived-param = "DERIVED" "=" ("true" / "false")
        private const string TrueText = "true";
        private const string FalseText = "false";

        /// <summary>
        /// Gets the text of an integer.
        /// </summary>
        internal static string FormatInteger(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the text of an integer.
        /// </summary>
        internal static string FormatInteger(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets whether a property may have a parameter of an extension.  A property vCard 4.0 doesn't define may have any, by the <c>any-param</c> of RFC 6350 §3.3.
        /// </summary>
        internal static bool IsAllowedOnProperty(VcardParameterSet parameter, string propertyName)
        {
            var info = VcardPropertyInfo.Find(propertyName);
            return info is null || !info.IsDefinedIn(VcardVersion.V4_0) || (info.Parameters & parameter) != 0;
        }

        /// <summary>
        /// Gets the text of a level.
        /// </summary>
        internal static string FormatLevel(VcardLevel level)
        {
            return LevelTexts[(int)level];
        }

        /// <summary>
        /// Gets whether a value is a member of <see cref="VcardLevel"/>.
        /// </summary>
        internal static bool IsDefinedLevel(VcardLevel level)
        {
            return level >= VcardLevel.Beginner && level <= VcardLevel.Low;
        }

        /// <summary>
        /// Parses the value of a <c>LEVEL</c> parameter (RFC 6715 §3.2), which RFC 6350 §5 makes case-insensitive.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="propertyName">The name of the property it is on.  The RFC limits the values of <c>EXPERTISE</c>, <c>HOBBY</c> and <c>INTEREST</c>, and says nothing of others.</param>
        /// <param name="result">The level, if it is one the property allows.</param>
        internal static bool TryParseLevel(string value, string propertyName, out VcardLevel result)
        {
            for (var index = 0; index < LevelTexts.Length; index++)
            {
                if (string.Equals(value, LevelTexts[index], StringComparison.OrdinalIgnoreCase))
                {
                    result = (VcardLevel)index;
                    return IsLevelOfProperty(result, propertyName);
                }
            }

            result = VcardLevel.Beginner;
            return false;
        }

        /// <summary>
        /// Parses the value of an <c>INDEX</c> parameter.  ref. RFC 6715 §3.1: INDEX-value = integer, and "INDEX values must be strictly positive.  Zero is not allowed."
        /// </summary>
        internal static bool TryParseIndex(string value, out long result)
        {
            return VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Text, out result) && result > 0;
        }

        /// <summary>
        /// Parses the value of a <c>CC</c> parameter (RFC 8605 §3.1), which RFC 6350 §5 makes case-insensitive.
        /// </summary>
        internal static bool TryParseCountryCode(string value, out Iso3166Part1Alpha2Country result)
        {
            // TODO: RFC 8605 §6.1 cites the third edition of ISO 3166, of 1988, and this is today's list.  So it rejects a code that has been withdrawn since, such as DD, and accepts one
            // assigned since, such as RS.  Revisit it if the edition matters to someone, which needs that edition's list.
            result = default(Iso3166Part1Alpha2Country);
            if (value.Length != CountryCodeLength) return false;

            foreach (var character in value)
            {
                if (!((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z'))) return false;
            }

            return Enum.TryParse(value.ToUpperInvariant(), false, out result);
        }

        /// <summary>
        /// Gets the text of a phonetic system.
        /// </summary>
        internal static string FormatPhonetic(VcardPhoneticSystem system)
        {
            return PhoneticTexts[(int)system];
        }

        /// <summary>
        /// Gets the text of a <c>DERIVED</c> parameter.
        /// </summary>
        internal static string FormatDerived(bool value)
        {
            return value ? TrueText : FalseText;
        }

        /// <summary>
        /// Gets whether a value is a member of <see cref="VcardPhoneticSystem"/>.
        /// </summary>
        internal static bool IsDefinedPhonetic(VcardPhoneticSystem system)
        {
            return system >= VcardPhoneticSystem.Ipa && system <= VcardPhoneticSystem.Script;
        }

        /// <summary>
        /// Parses the value of a <c>PHONETIC</c> parameter, which RFC 6350 §5 makes case-insensitive.  ref. RFC 9554 §4.6: it "MUST be one of the values enumerated in the IANA 'vCard
        /// Parameter Values' registry", which holds these four, although its ABNF also has <c>iana-token</c> and <c>x-name</c>.
        /// </summary>
        internal static bool TryParsePhonetic(string value, out VcardPhoneticSystem result)
        {
            for (var index = 0; index < PhoneticTexts.Length; index++)
            {
                if (string.Equals(value, PhoneticTexts[index], StringComparison.OrdinalIgnoreCase))
                {
                    result = (VcardPhoneticSystem)index;
                    return true;
                }
            }

            result = VcardPhoneticSystem.Ipa;
            return false;
        }

        /// <summary>
        /// Parses the value of a <c>DERIVED</c> parameter (RFC 9554 §4.4), whose ABNF strings RFC 5234 §2.3 makes case-insensitive, as its example <c>DERIVED=TRUE</c> shows.
        /// </summary>
        internal static bool TryParseDerived(string value, out bool result)
        {
            result = string.Equals(value, TrueText, StringComparison.OrdinalIgnoreCase);
            return result || string.Equals(value, FalseText, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets whether a line has <c>DERIVED=true</c> (RFC 9554 §4.4).
        /// </summary>
        internal static bool IsDerived(VcardContentLine line)
        {
            var text = GetValue(line, VcardParameterNames.Derived);
            bool result;
            return text != null && TryParseDerived(text, out result) && result;
        }

        /// <summary>
        /// Gets whether text is a valid <c>PROP-ID</c> (RFC 9554 §4.7).
        /// </summary>
        internal static bool IsPropertyId(string value)
        {
            if (value.Length == 0 || value.Length > MaxPropertyIdLength) return false;

            foreach (var character in value)
            {
                if (!(IsLetter(character) || VcardValueFormatting.IsDigit(character) || character == '-' || character == '_')) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets whether text is a valid <c>SCRIPT</c> (RFC 9554 §4.8).
        /// </summary>
        internal static bool IsScript(string value)
        {
            if (value.Length != ScriptLength) return false;

            foreach (var character in value)
            {
                if (!IsLetter(character)) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets whether a line may have a <c>USERNAME</c>.  ref. RFC 9554 §4.10: "The IMPP or SOCIALPROFILE value type MUST be URI."  The value type of an unknown property isn't known.
        /// </summary>
        internal static bool IsUsernameAllowed(VcardContentLine line)
        {
            return IsAllowedOnProperty(VcardParameterSet.Username, line.Name) &&
                   (!VcardValueTypeRegistry.IsKnownProperty(line.Name.AsSpan()) || VcardValueTypeRegistry.GetValueType(line) == VcardValueType.Uri);
        }

        /// <summary>
        /// Gets whether a parameter is written in quotes whatever its value holds.  ref. RFC 9554 §4.9 and §4.10: the case of <c>SERVICE-TYPE</c> and <c>USERNAME</c> is significant and
        /// "MUST be preserved", and RFC 6350 §5 says that a value not in quotes is case-insensitive.  Erratum 8652 of RFC 9554 quotes the value in the example.
        /// </summary>
        internal static bool IsAlwaysQuoted(string name)
        {
            return string.Equals(name, VcardParameterNames.ServiceType, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, VcardParameterNames.Username, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets every value of a parameter that may be repeated or hold a list, in order, each split on its commas.
        /// </summary>
        /// <remarks>
        /// ref. RFC 6350 §6.4.1: "These type parameter values can be specified as a parameter list (e.g., TYPE=text;TYPE=voice) or as a value list (e.g., TYPE="text,voice")", and
        /// the examples of §5.9 and §8 write a list in one quoted string in the same way, though the ABNF has no quotes there.
        /// </remarks>
        internal static List<string> GetListValues(VcardContentLine line, string name)
        {
            var result = new List<string>();
            foreach (var parameter in line.ParameterArray)
            {
                if (!string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var value in parameter.ValueArray)
                {
                    result.AddRange(value.Split(','));
                }
            }

            return result;
        }

        /// <summary>
        /// Parses the value of a <c>PREF</c> parameter (RFC 6350 §5.3): <c>pref-param = "PREF=" (1*2DIGIT / "100")</c>, between 1 and 100.
        /// </summary>
        internal static bool TryParsePreference(string value, out int result)
        {
            result = 0;
            if (value.Length == 0 || (value.Length > MaxPreferenceDigits && value != MaxPreferenceText)) return false;

            foreach (var character in value)
            {
                if (!VcardValueFormatting.IsDigit(character)) return false;
            }

            result = int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
            return result >= MinPreference && result <= MaxPreference;
        }

        /// <summary>
        /// Gets the level of preference of a line: the value of its <c>PREF</c> parameter; or one past the least preferred value, which is how RFC 6350 §5.3 reads an instance
        /// without it.  A value that isn't valid says nothing, so it counts as absent.
        /// </summary>
        internal static int GetPreferenceOrLeast(VcardContentLine line)
        {
            var parameter = line.GetParameter(VcardParameterNames.Preference);
            int preference;
            return parameter != null && TryParsePreference(parameter.ValueArray[0], out preference) ? preference : MaxPreference + 1;
        }

        /// <summary>
        /// Gets the first value of a parameter, or the first value of the first parameter of that name.
        /// </summary>
        /// <returns>The value; or <c>null</c> if the line doesn't have the parameter.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string? GetValue(VcardContentLine line, string name)
#else
        internal static string GetValue(VcardContentLine line, string name)
#endif
        {
            return line.GetParameter(name)?.ValueArray[0];
        }

        /// <summary>
        /// Groups items by their <c>ALTID</c>, in the order each group first appears (RFC 6350 §5.4).  An item without it is a group of its own, and values are compared as opaque
        /// strings.
        /// </summary>
        internal static List<List<TItem>> GroupByAlternatives<TItem>(IEnumerable<TItem> items, Func<TItem, VcardContentLine> lineOf)
        {
            var groups = new List<List<TItem>>();
            var groupsByAltId = new Dictionary<string, List<TItem>>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                var altId = GetValue(lineOf(item), VcardParameterNames.AlternativeId);
                if (altId != null && groupsByAltId.ContainsKey(altId))
                {
                    groupsByAltId[altId].Add(item);
                    continue;
                }

                var group = new List<TItem> { item };
                groups.Add(group);
                if (altId != null)
                {
                    groupsByAltId.Add(altId, group);
                }
            }

            return groups;
        }

        private static bool IsLetter(char character)
        {
            return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
        }

        private static bool IsLevelOfProperty(VcardLevel level, string propertyName)
        {
            // ref. RFC 6715 §3.2: "'beginner', 'average', 'expert' when used with EXPERTISE", and "'high', 'medium', 'low' when used with HOBBY or INTEREST".
            if (string.Equals(propertyName, VcardPropertyNames.Expertise, StringComparison.OrdinalIgnoreCase)) return level <= VcardLevel.Expert;

            var isHobbyOrInterest = string.Equals(propertyName, VcardPropertyNames.Hobby, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(propertyName, VcardPropertyNames.Interest, StringComparison.OrdinalIgnoreCase);
            return !isHobbyOrInterest || level >= VcardLevel.High;
        }

        /// <summary>
        /// Gets the index of the first of the items with the lowest <c>PREF</c> (RFC 6350 §5.3); or -1 if there are no items.
        /// </summary>
        internal static int IndexOfPreferred<TItem>(IReadOnlyList<TItem> items, Func<TItem, VcardContentLine> lineOf)
        {
            var preferredIndex = -1;
            var lowest = int.MaxValue;
            for (var index = 0; index < items.Count; index++)
            {
                var preference = GetPreferenceOrLeast(lineOf(items[index]));
                if (preference < lowest)
                {
                    lowest = preference;
                    preferredIndex = index;
                }
            }

            return preferredIndex;
        }
    }
}
