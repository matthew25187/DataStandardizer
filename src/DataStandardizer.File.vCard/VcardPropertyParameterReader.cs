using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Reads the parameters RFC 6350 defines from a content line.
    /// </summary>
    internal static class VcardPropertyParameterReader
    {
        // ref. RFC 6350 §5.3: an integer between 1 and 100.
        internal const int MinPreference = 1;
        internal const int MaxPreference = 100;

        // ref. RFC 6350 §5.3: pref-param = "PREF=" (1*2DIGIT / "100")
        private const int MaxPreferenceDigits = 2;
        private const string MaxPreferenceText = "100";

        /// <summary>
        /// Gets the text of an integer.
        /// </summary>
        internal static string FormatInteger(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
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
