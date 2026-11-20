using System;
using System.Collections.Generic;
using DataStandardizer.LanguageTag.InternalState;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// Subtag from an IETF BCP 47 language tag.
    /// </summary>
    public readonly struct Bcp47KeyedSubtag : IEquatable<Bcp47KeyedSubtag>
    {
        #region Declarations

        private static class ErrorMessage
        {
            internal const string SubtagNull = "The subtag must not be null.";
            internal const string SubtagNotKeyed = "The subtag is neither one extension (RFC 5646 §2.2.6) nor one private use sequence (RFC 5646 §2.2.7).";
            internal const string UninitializedInstance = "The " + nameof(Bcp47KeyedSubtag) + " instance is not initialised.";
        }
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _subtag;
#else
        [MaybeNull] private readonly string _subtag;
#endif

        #endregion

        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47KeyedSubtag(string subtag)
#else
        internal Bcp47KeyedSubtag([DisallowNull] string subtag)
#endif
        {
            _subtag = subtag ?? throw new ArgumentNullException(nameof(subtag));
        }

        #endregion

        #region Operators

        /// <summary>
        /// Convert a string to a keyed subtag.
        /// </summary>
        /// <param name="value">
        /// Subtag string: one extension, a singleton other than <c>x</c> followed by one or more subtags of two to eight letters or digits (<c>extension</c>, RFC 5646 §2.2.6),
        /// or one private use sequence, <c>x</c> followed by one or more subtags of one to eight letters or digits (<c>privateuse</c>, RFC 5646 §2.2.7).
        /// Letters may be in either case, and the subtag keeps the case it is written in.
        /// </param>
        /// <exception cref="InvalidCastException">The subtag is <c>null</c>, or is neither one extension nor one private use sequence.</exception>
        /// <remarks>
        /// This checks the shape of the subtag only, and looks nothing up: the extension's singleton may be one that nothing has assigned.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static explicit operator Bcp47KeyedSubtag(string value)
#else
        public static explicit operator Bcp47KeyedSubtag([DisallowNull] string value)
#endif
        {
            if (value is null)
            {
                throw new InvalidCastException(ErrorMessage.SubtagNull);
            }

            var span = value.AsSpan();
            if (!Bcp47LanguageTagParser.IsExtension(span) && !Bcp47LanguageTagParser.IsPrivateUse(span))
            {
                throw new InvalidCastException(ErrorMessage.SubtagNotKeyed);
            }

            return new Bcp47KeyedSubtag(value);
        }

#if NETCOREAPP3_0_OR_GREATER
        public static implicit operator string?(Bcp47KeyedSubtag value)
#else
        [return: MaybeNull]
        public static implicit operator string(Bcp47KeyedSubtag value)
#endif
        {
            return value._subtag;
        }

        public static bool operator ==(Bcp47KeyedSubtag left, Bcp47KeyedSubtag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Bcp47KeyedSubtag left, Bcp47KeyedSubtag right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Public Methods

        public bool Equals(Bcp47KeyedSubtag other)
        {
            return string.Equals(_subtag, other._subtag, StringComparison.OrdinalIgnoreCase);
        }

#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
        {
            return obj is Bcp47KeyedSubtag other && Equals(other);
        }
#else
        public override bool Equals(object obj)
        {
            return obj is Bcp47KeyedSubtag other && Equals(other);
        }
#endif

        public override int GetHashCode()
        {
            return (_subtag != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(_subtag) : 0);
        }

        public override string ToString()
        {
            return _subtag ?? base.ToString() ?? this.GetType().FullName ?? this.GetType().Name;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the single character that identifies the subtag: the extension's singleton (RFC 5646 §2.2.6), or <c>x</c> for a private use sequence (§2.2.7).
        /// </summary>
        /// <remarks>
        /// The value is never <c>null</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47KeyedSubtag"/> is not initialised.</exception>
        public char? Singleton
        {
            get
            {
                if (_subtag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                // Every instance holds one extension or one private use sequence, whose first subtag is its one-character singleton.
                return _subtag[0];
            }
        }

        /// <summary>
        /// Gets the subtags that provide additional information related to the singleton.
        /// </summary>
        /// <remarks>
        /// Each read splits the subtag afresh, so the list belongs to the caller.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47KeyedSubtag"/> is not initialised.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public IReadOnlyList<string> Subtags
#else
        [NotNull]
        public IReadOnlyList<string> Subtags
#endif
        {
            get
            {
                if (_subtag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                // Every segment after the first, so one per separator.
#if NET10_0_OR_GREATER
                var span = _subtag.AsSpan();
                var subtags = new string[span.Count('-')];
                var index = -1;
                foreach (var range in span.Split('-'))
                {
                    if (index >= 0) subtags[index] = _subtag[range];
                    index++;
                }

                return subtags;
#else
                var separatorCount = 0;
                foreach (var c in _subtag)
                {
                    if (c == '-') separatorCount++;
                }

                var subtags = new string[separatorCount];
                var segmentStart = _subtag.IndexOf('-') + 1;
                for (var i = 0; i < separatorCount; i++)
                {
                    var segmentEnd = _subtag.IndexOf('-', segmentStart);
                    if (segmentEnd < 0) segmentEnd = _subtag.Length;
                    subtags[i] = _subtag.Substring(segmentStart, segmentEnd - segmentStart);
                    segmentStart = segmentEnd + 1;
                }

                return subtags;
#endif
            }
        }

        #endregion
    }
}