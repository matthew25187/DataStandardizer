using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An ordered, read-only list of phrases, as in a Keywords header field (RFC 5322 §3.6.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each element holds the words of one phrase, separated by a space.  The empty elements of the obsolete form (RFC 5322 §4.1) are not kept, and
    /// that form allows no phrases at all, so a parsed list can be empty.  The modern form needs at least one, so an empty list formats as an empty string.
    /// </para>
    /// <para>
    /// RFC 2047 encoded words are kept as written, and a quoted-string of that form stays plain text.
    /// </para>
    /// </remarks>
    public sealed class ImfPhraseList : IReadOnlyList<string>, IEquatable<ImfPhraseList>
    {
        private const string InvalidFormatMessage = "The phrase list is not in a valid format.";

        private readonly ImfPhrase[] _phrases;

        /// <summary>
        /// Initializes a new list.
        /// </summary>
        /// <param name="phrases">The phrases as plain text, in order, which are copied.</param>
        /// <exception cref="ArgumentNullException"><paramref name="phrases"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="phrases"/> is empty, as RFC 5322 §3.6.5 needs at least one phrase, holds a <c>null</c>
        /// element, or holds one with a control character other than tab or an unpaired surrogate.</exception>
        public ImfPhraseList(IEnumerable<string> phrases)
        {
            var elements = ImfGrammar.CopyElements(phrases, nameof(phrases));
            if (elements.Length == 0) throw new ArgumentException("The list has no phrases.", nameof(phrases));

            _phrases = new ImfPhrase[elements.Length];
            for (var index = 0; index < elements.Length; index++)
            {
                if (!ImfGrammar.IsWritableText(elements[index])) throw new ArgumentException("A phrase holds a character that cannot be written.", nameof(phrases));

                _phrases[index] = new ImfPhrase(elements[index], false);
            }
        }

        private ImfPhraseList(ImfPhrase[] phrases)
        {
            _phrases = phrases;
        }

        /// <inheritdoc />
        public int Count => _phrases.Length;

        /// <inheritdoc />
        public string this[int index] => _phrases[index].Text;

        /// <summary>
        /// Parses a list of phrases.
        /// </summary>
        /// <param name="value">The list as UTF-8 (RFC 6532), which may be folded, such as <c>first, "second one"</c>.</param>
        /// <returns>The list.</returns>
        /// <exception cref="FormatException">The value is not a valid list of phrases.</exception>
        public static ImfPhraseList Parse(ReadOnlySpan<byte> value)
        {
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a list of phrases.
        /// </summary>
        /// <param name="value">The list, which may be folded, such as <c>first, "second one"</c>.</param>
        /// <returns>The list.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid list of phrases.</exception>
        public static ImfPhraseList Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParse(value, out var result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a list of phrases.
        /// </summary>
        /// <param name="value">The list as UTF-8 (RFC 6532), which may be folded, such as <c>first, "second one"</c>.</param>
        /// <param name="result">The list, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid list of phrases; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out ImfPhraseList result)
        {
            result = null;
            if (!ImfAddressParser.TryParsePhraseList(value, out var phrases)) return false;

            result = new ImfPhraseList(phrases);
            return true;
        }

        /// <summary>
        /// Parses a list of phrases.
        /// </summary>
        /// <param name="value">The list, which may be folded, such as <c>first, "second one"</c>.</param>
        /// <param name="result">The list, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid list of phrases; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParse([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out ImfPhraseList result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParse(bytes, out result);
        }

        /// <inheritdoc />
        public IEnumerator<string> GetEnumerator()
        {
            foreach (var phrase in _phrases)
            {
                yield return phrase.Text;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Indicates whether this list equals another: it has the same phrases in the same order, compared case-sensitively.
        /// </summary>
        public bool Equals([AllowNull] ImfPhraseList other)
        {
            if (other is null || _phrases.Length != other._phrases.Length) return false;

            for (var index = 0; index < _phrases.Length; index++)
            {
                if (!string.Equals(_phrases[index].Text, other._phrases[index].Text, StringComparison.Ordinal) ||
                    _phrases[index].HasEncodedWordAtom != other._phrases[index].HasEncodedWordAtom)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is ImfPhraseList other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = 0;
            foreach (var phrase in _phrases)
            {
                hash = ImfGrammar.CombineHashCodes(hash, StringComparer.Ordinal.GetHashCode(phrase.Text));
            }

            return hash;
        }

        /// <summary>
        /// Indicates whether two lists are equal.
        /// </summary>
        public static bool operator ==([AllowNull] ImfPhraseList left, [AllowNull] ImfPhraseList right)
        {
            return left is null ? right is null : left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two lists differ.
        /// </summary>
        public static bool operator !=([AllowNull] ImfPhraseList left, [AllowNull] ImfPhraseList right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Formats the list in the modern form, such as <c>first, "second one"</c>.
        /// </summary>
        /// <returns>The list.</returns>
        public override string ToString()
        {
            var output = new StringBuilder();
            for (var index = 0; index < _phrases.Length; index++)
            {
                if (index > 0) output.Append(", ");

                ImfGrammar.AppendPhrase(_phrases[index].Text, _phrases[index].HasEncodedWordAtom, output);
            }

            return output.ToString();
        }
    }
}
