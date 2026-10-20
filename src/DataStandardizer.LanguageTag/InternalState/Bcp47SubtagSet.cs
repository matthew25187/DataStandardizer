using System;
using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
#if NETSTANDARD1_0
using CharSpan = DataStandardizer.LanguageTag.InternalState.Bcp47CharSpan;
#else
using CharSpan = System.ReadOnlySpan<char>;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Set of subtags or tags, compared case-insensitively in the ASCII range only (ref. RFC 5646 §2.1.1).
    /// </summary>
    /// <remarks>
    /// A sorted array searched by binary search: about 13 short comparisons for the largest set, and no allocation.
    /// </remarks>
    internal sealed class Bcp47SubtagSet
    {
        private readonly string[] _items;

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47SubtagSet(IEnumerable<string> items)
#else
        internal Bcp47SubtagSet([NotNull] IEnumerable<string> items)
#endif
        {
            var sortedItems = new List<string>(items);
            sortedItems.Sort(StringComparer.OrdinalIgnoreCase);

            // Drop items that differ only in case, so that each is held once.
            var distinctCount = 0;
            for (var index = 0; index < sortedItems.Count; index++)
            {
                var item = sortedItems[index];
                if (distinctCount > 0 && StringComparer.OrdinalIgnoreCase.Equals(sortedItems[distinctCount - 1], item)) continue;

                sortedItems[distinctCount++] = item;
            }

            sortedItems.RemoveRange(distinctCount, sortedItems.Count - distinctCount);
            _items = sortedItems.ToArray();
        }

        internal int Count => _items.Length;

        /// <summary>
        /// Determine whether the set contains a subtag.
        /// </summary>
        /// <remarks>
        /// A value with a character outside the ASCII range is never in the set, so the result does not depend on how a runtime's
        /// <see cref="StringComparison.OrdinalIgnoreCase"/> folds non-ASCII characters such as U+0131 LATIN SMALL LETTER DOTLESS I.
        /// </remarks>
        internal bool Contains(CharSpan value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (value[index] > '\u007F') return false;
            }

            var low = 0;
            var high = _items.Length - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var comparison = value.CompareTo(_items[middle].AsSpan(), StringComparison.OrdinalIgnoreCase);
                if (comparison == 0) return true;

                if (comparison < 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return false;
        }
    }
}
