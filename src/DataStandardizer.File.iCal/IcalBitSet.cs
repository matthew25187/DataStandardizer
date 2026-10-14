using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A fixed set of 896 bits, held inline so that a value holding one needs no allocation.
    /// </summary>
    /// <remarks>
    /// Read it through the static members, which take the set by reference, so that a set held in a <c>readonly</c> field is not copied.
    /// </remarks>
    internal struct IcalBitSet : IEquatable<IcalBitSet>
    {
        internal const int Capacity = WordCount * 64;

        private const int WordCount = 14;

        private ulong _word0;
        private ulong _word1;
        private ulong _word2;
        private ulong _word3;
        private ulong _word4;
        private ulong _word5;
        private ulong _word6;
        private ulong _word7;
        private ulong _word8;
        private ulong _word9;
        private ulong _word10;
        private ulong _word11;
        private ulong _word12;
        private ulong _word13;

        /// <summary>
        /// Sets a bit.
        /// </summary>
        internal void Set(int index)
        {
            var wordIndex = index >> 6;
            SetWord(wordIndex, GetWord(in this, wordIndex) | (1UL << (index & 63)));
        }

        /// <summary>
        /// Gets whether a bit is set.
        /// </summary>
        internal static bool Get(in IcalBitSet set, int index)
        {
            return (GetWord(in set, index >> 6) & (1UL << (index & 63))) != 0;
        }

        /// <summary>
        /// Gets whether no bit is set.
        /// </summary>
        internal static bool IsEmpty(in IcalBitSet set)
        {
            return (set._word0 | set._word1 | set._word2 | set._word3 | set._word4 | set._word5 | set._word6 | set._word7 | set._word8 |
                    set._word9 | set._word10 | set._word11 | set._word12 | set._word13) == 0;
        }

        /// <summary>
        /// Gets whether any bit in a range is set.
        /// </summary>
        /// <param name="set">The set.</param>
        /// <param name="start">The index of the first bit in the range.</param>
        /// <param name="end">The index after the last bit in the range.</param>
        internal static bool Any(in IcalBitSet set, int start, int end)
        {
            var index = NextSetBit(in set, start);
            return index >= 0 && index < end;
        }

        /// <summary>
        /// Gets the index of the first set bit at or after an index.
        /// </summary>
        /// <returns>The index; or -1 if there is none.</returns>
        internal static int NextSetBit(in IcalBitSet set, int start)
        {
            var wordIndex = start >> 6;
            if (wordIndex >= WordCount) return -1;

            var word = GetWord(in set, wordIndex) & (ulong.MaxValue << (start & 63));
            while (true)
            {
                if (word != 0) return wordIndex * 64 + CountTrailingZeros(word);

                if (++wordIndex == WordCount) return -1;

                word = GetWord(in set, wordIndex);
            }
        }

        /// <summary>
        /// Counts the set bits in a range.
        /// </summary>
        internal static int Count(in IcalBitSet set, int start, int end)
        {
            var count = 0;
            for (var index = NextSetBit(in set, start); index >= 0 && index < end; index = NextSetBit(in set, index + 1))
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Gets the index of the lowest set bit of a word that is not zero.
        /// </summary>
        internal static int CountTrailingZeros(ulong word)
        {
#if NETCOREAPP3_0_OR_GREATER
            return System.Numerics.BitOperations.TrailingZeroCount(word);
#else
            var count = 0;
            while ((word & 1) == 0)
            {
                word >>= 1;
                count++;
            }

            return count;
#endif
        }

        public bool Equals(IcalBitSet other)
        {
            return _word0 == other._word0 && _word1 == other._word1 && _word2 == other._word2 && _word3 == other._word3 &&
                   _word4 == other._word4 && _word5 == other._word5 && _word6 == other._word6 && _word7 == other._word7 &&
                   _word8 == other._word8 && _word9 == other._word9 && _word10 == other._word10 && _word11 == other._word11 &&
                   _word12 == other._word12 && _word13 == other._word13;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalBitSet other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = _word0 ^ _word1 ^ _word2 ^ _word3 ^ _word4 ^ _word5 ^ _word6 ^ _word7 ^ _word8 ^ _word9 ^ _word10 ^ _word11 ^ _word12 ^
                       _word13;
            return hash.GetHashCode();
        }

        private static ulong GetWord(in IcalBitSet set, int wordIndex)
        {
            switch (wordIndex)
            {
                case 0: return set._word0;
                case 1: return set._word1;
                case 2: return set._word2;
                case 3: return set._word3;
                case 4: return set._word4;
                case 5: return set._word5;
                case 6: return set._word6;
                case 7: return set._word7;
                case 8: return set._word8;
                case 9: return set._word9;
                case 10: return set._word10;
                case 11: return set._word11;
                case 12: return set._word12;
                case 13: return set._word13;
                default: throw new ArgumentOutOfRangeException(nameof(wordIndex));
            }
        }

        private void SetWord(int wordIndex, ulong word)
        {
            switch (wordIndex)
            {
                case 0: _word0 = word; break;
                case 1: _word1 = word; break;
                case 2: _word2 = word; break;
                case 3: _word3 = word; break;
                case 4: _word4 = word; break;
                case 5: _word5 = word; break;
                case 6: _word6 = word; break;
                case 7: _word7 = word; break;
                case 8: _word8 = word; break;
                case 9: _word9 = word; break;
                case 10: _word10 = word; break;
                case 11: _word11 = word; break;
                case 12: _word12 = word; break;
                case 13: _word13 = word; break;
                default: throw new ArgumentOutOfRangeException(nameof(wordIndex));
            }
        }
    }
}
