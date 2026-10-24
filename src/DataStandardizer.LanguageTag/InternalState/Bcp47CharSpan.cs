using System;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Read-only view of a range of characters in a string, with the members of <c>ReadOnlySpan&lt;char&gt;</c> that the parser uses.
    /// </summary>
    /// <remarks>
    /// <c>System.Memory</c> does not support <c>netstandard1.0</c>, so the parser is compiled against this type there and against
    /// <c>ReadOnlySpan&lt;char&gt;</c> on every other target. It is compiled on every target so that the tests can compare it with
    /// <c>ReadOnlySpan&lt;char&gt;</c>; only <c>netstandard1.0</c> uses it. #258 removes it.
    /// </remarks>
    internal readonly struct Bcp47CharSpan
    {
        private readonly string _text;
        private readonly int _start;

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47CharSpan(string text) : this(text, 0, text.Length)
#else
        internal Bcp47CharSpan([NotNull] string text) : this(text, 0, text.Length)
#endif
        {
        }

        private Bcp47CharSpan(string text, int start, int length)
        {
            _text = text;
            _start = start;
            Length = length;
        }

        public bool IsEmpty => Length == 0;

        public int Length { get; }

        public char this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Length) throw new IndexOutOfRangeException();

                return _text[_start + index];
            }
        }

        public Bcp47CharSpan Slice(int start)
        {
            if ((uint)start > (uint)Length) throw new ArgumentOutOfRangeException(nameof(start));

            return new Bcp47CharSpan(_text, _start + start, Length - start);
        }

        public Bcp47CharSpan Slice(int start, int length)
        {
            if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start)) throw new ArgumentOutOfRangeException(nameof(start));

            return new Bcp47CharSpan(_text, _start + start, length);
        }

        /// <summary>
        /// Compare with another range of characters, as <c>MemoryExtensions.CompareTo</c> does.
        /// </summary>
        public int CompareTo(Bcp47CharSpan other, StringComparison comparisonType)
        {
            // Compare the common length only, so that neither comparison reads past the end of its range.
            var commonLength = Math.Min(Length, other.Length);
            var result = string.Compare(_text, _start, other._text, other._start, commonLength, comparisonType);

            return result != 0 ? result : Length.CompareTo(other.Length);
        }

        public override string ToString()
        {
            return _text is null ? string.Empty : _text.Substring(_start, Length);
        }
    }

#if NETSTANDARD1_0
    internal static class Bcp47CharSpanExtensions
    {
        /// <summary>
        /// Create a view of a whole string, in place of <c>MemoryExtensions.AsSpan</c>.
        /// </summary>
        internal static Bcp47CharSpan AsSpan([NotNull] this string text)
        {
            return new Bcp47CharSpan(text);
        }
    }
#endif
}
