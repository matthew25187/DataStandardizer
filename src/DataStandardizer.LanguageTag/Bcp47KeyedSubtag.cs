using System;
#if NETSTANDARD
using JetBrains.Annotations; 
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
            internal const string UninitializedInstance = "The " + nameof(Bcp47KeyedSubtag) + " instance is not initialized.";
        }
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _subtag;
#else
        [CanBeNull] private readonly string _subtag;
#endif

        #endregion

        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47KeyedSubtag(string subtag)
#else
        internal Bcp47KeyedSubtag([NotNull] string subtag)
#endif
        {
            _subtag = subtag ?? throw new ArgumentNullException(nameof(subtag));
        }

        #endregion

        #region Operators

#if NETCOREAPP3_0_OR_GREATER
        public static explicit operator Bcp47KeyedSubtag(string value)
#else
        public static explicit operator Bcp47KeyedSubtag([NotNull] string value)
#endif
        {
            return new Bcp47KeyedSubtag(value);
        }

#if NETCOREAPP3_0_OR_GREATER
        public static implicit operator string?(Bcp47KeyedSubtag value)
#else
        [CanBeNull]
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
        /// Gets the single character that identifies the subtag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47KeyedSubtag"/> is not initialized.</exception>
        public char? Singleton
        {
            get
            {
                if (_subtag is null) throw new InvalidOperationException(ErrorMessage.UninitializedInstance);

                // The singleton is the first character of the first segment, or null when that segment is empty or whitespace.
                var segmentEnd = _subtag.IndexOf('-');
                if (segmentEnd < 0) segmentEnd = _subtag.Length;
                for (var i = 0; i < segmentEnd; i++)
                {
                    if (!char.IsWhiteSpace(_subtag[i])) return _subtag[0];
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the subtags that provide additional information related to the singleton.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Bcp47KeyedSubtag"/> is not initialized.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public string[] Subtags
#else
        [NotNull]
        public string[] Subtags
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
#elif NET8_0_OR_GREATER
                var span = _subtag.AsSpan();
                var segmentCount = span.Count('-') + 1;
                var ranges = segmentCount <= 32 ? stackalloc Range[segmentCount] : new Range[segmentCount];
                span.Split(ranges, '-');
                var subtags = new string[segmentCount - 1];
                for (var i = 1; i < segmentCount; i++)
                {
                    subtags[i - 1] = _subtag[ranges[i]];
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