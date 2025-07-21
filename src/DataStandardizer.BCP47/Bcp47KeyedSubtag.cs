using System;
using System.Linq;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.BCP47
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
            return _subtag == other._subtag;
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
            return (_subtag != null ? _subtag.GetHashCode() : 0);
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

                var singletonCandidate = _subtag.Split('-').ElementAtOrDefault(0);
                return !string.IsNullOrWhiteSpace(singletonCandidate) ? singletonCandidate[0] : (char?)null;
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

                return _subtag.Split('-').Skip(1).ToArray();
            }
        }

        #endregion
    }
}