using System;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// A span of characters for a test case: the whole of a string, or the middle of a longer one.
    /// </summary>
    /// <remarks>
    /// A test case factory makes each one, so a test only takes the span. A check that read outside a span taken from the middle of
    /// a string would see the characters around it.
    /// </remarks>
    public sealed class SpanInput
    {
        /// <summary>
        /// Characters around a span taken from the middle of a string.
        /// </summary>
        private const string Padding = "zz";

        private readonly string _text;
        private readonly int _start;
        private readonly int _length;

        private SpanInput(string text, int start, int length, string description)
        {
            _text = text;
            _start = start;
            _length = length;
            Description = description;
        }

        /// <summary>
        /// What the span holds, for the name of a test case.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// A span over the whole of a string.
        /// </summary>
        public static SpanInput Whole(string value)
        {
            return new SpanInput(value, 0, value.Length, "\"" + value + "\"");
        }

        /// <summary>
        /// A span over a string that has other characters on each side of it.
        /// </summary>
        public static SpanInput Padded(string value)
        {
            return new SpanInput(Padding + value + Padding, Padding.Length, value.Length, "\"" + value + "\" padded");
        }

        public ReadOnlySpan<char> AsSpan()
        {
            return _text.AsSpan(_start, _length);
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
