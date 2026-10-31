using System;
using System.Collections.Generic;
#if NET9_0_OR_GREATER
using System.Collections.Frozen;
#endif
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Map from subtags or tags to values, compared case-insensitively in the ASCII range only (ref. RFC 5646 §2.1.1).
    /// </summary>
    /// <remarks>
    /// On .NET 9 and later, a frozen dictionary queried through its span lookup: one hash probe, and no allocation.
    /// On other targets, sorted arrays searched by binary search: about 13 short comparisons for the largest map, and no allocation.
    /// </remarks>
    internal sealed class Bcp47SubtagMap<TValue>
        where TValue : class
    {
#if NET9_0_OR_GREATER
        private readonly FrozenDictionary<string, TValue>.AlternateLookup<ReadOnlySpan<char>> _lookup;
#else
        private readonly string[] _keys;
        private readonly TValue[] _values;
#endif

        /// <param name="keys">Keys, each with the value at the same index of <paramref name="values"/>. Of keys that differ only in case, the first is held, with its value.</param>
        /// <param name="values">Values of the keys.</param>
#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47SubtagMap(List<string> keys, List<TValue> values)
#else
        internal Bcp47SubtagMap([NotNull] List<string> keys, [NotNull] List<TValue> values)
#endif
        {
#if NET9_0_OR_GREATER
            var items = new Dictionary<string, TValue>(keys.Count, StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < keys.Count; index++)
            {
                items.TryAdd(keys[index], values[index]);
            }

            // The source has the same comparer, so the frozen dictionary is built from it without hashing the keys into a dictionary again.
            _lookup = items.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();
#else
            // The keys are sorted with their positions, and the position breaks ties, so of keys that differ only in case the first is kept, on every target.
            var sortedKeys = keys.ToArray();
            var positions = new int[sortedKeys.Length];
            for (var index = 0; index < positions.Length; index++)
            {
                positions[index] = index;
            }

#if NETSTANDARD1_1
            // netstandard1.1 has no Array.Sort of keys with items.
            Array.Sort(positions, (x, y) => ComparePositions(keys, x, y));
            for (var index = 0; index < positions.Length; index++)
            {
                sortedKeys[index] = keys[positions[index]];
            }
#else
            Array.Sort(sortedKeys, positions, StringComparer.OrdinalIgnoreCase);
#endif

            var distinctCount = 0;
            for (var index = 0; index < sortedKeys.Length; index++)
            {
                if (distinctCount > 0 && StringComparer.OrdinalIgnoreCase.Equals(sortedKeys[distinctCount - 1], sortedKeys[index]))
                {
                    if (positions[index] < positions[distinctCount - 1]) positions[distinctCount - 1] = positions[index];
                    continue;
                }

                sortedKeys[distinctCount] = sortedKeys[index];
                positions[distinctCount] = positions[index];
                distinctCount++;
            }

            _keys = new string[distinctCount];
            _values = new TValue[distinctCount];
            for (var index = 0; index < distinctCount; index++)
            {
                _keys[index] = keys[positions[index]];
                _values[index] = values[positions[index]];
            }
#endif
        }

#if NETSTANDARD1_1
        private static int ComparePositions(List<string> keys, int x, int y)
        {
            var comparison = StringComparer.OrdinalIgnoreCase.Compare(keys[x], keys[y]);
            return comparison != 0 ? comparison : x.CompareTo(y);
        }
#endif

#if NET9_0_OR_GREATER
        internal int Count => _lookup.Dictionary.Count;
#else
        internal int Count => _keys.Length;
#endif

        /// <summary>
        /// Determine whether the map contains a subtag.
        /// </summary>
        internal bool ContainsKey(ReadOnlySpan<char> key)
        {
            return TryGetValue(key, out _);
        }

        /// <summary>
        /// Gets the value of a subtag.
        /// </summary>
        /// <remarks>
        /// A key with a character outside the ASCII range is never in the map, so the result does not depend on how a runtime's
        /// <see cref="StringComparison.OrdinalIgnoreCase"/> folds non-ASCII characters such as U+0131 LATIN SMALL LETTER DOTLESS I.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        internal bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
#else
        internal bool TryGetValue(ReadOnlySpan<char> key, [CanBeNull] out TValue value)
#endif
        {
            for (var index = 0; index < key.Length; index++)
            {
                if (key[index] > '\u007F')
                {
                    value = default;
                    return false;
                }
            }

#if NET9_0_OR_GREATER
            return _lookup.TryGetValue(key, out value);
#else
            var low = 0;
            var high = _keys.Length - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var comparison = key.CompareTo(_keys[middle].AsSpan(), StringComparison.OrdinalIgnoreCase);
                if (comparison == 0)
                {
                    value = _values[middle];
                    return true;
                }

                if (comparison < 0)
                {
                    high = middle - 1;
                }
                else
                {
                    low = middle + 1;
                }
            }

            value = default;
            return false;
#endif
        }
    }
}
