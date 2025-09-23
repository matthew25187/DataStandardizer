using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// The exception that is thrown when a language tag is not correctly formatted.
    /// </summary>
    public class LanguageTagFormatException : FormatException
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        public LanguageTagFormatException(string message, string languageTag)
#else
        public LanguageTagFormatException([NotNull] string message, [NotNull] string languageTag)
#endif
            : base(message)
        {
            LanguageTag = languageTag;
        }

#if NETCOREAPP3_0_OR_GREATER
        public LanguageTagFormatException(string message, string languageTag, Exception innerException)
#else
        public LanguageTagFormatException([NotNull] string message, [NotNull] string languageTag, Exception innerException)
#endif
            : base(message, innerException)
        {
            LanguageTag = languageTag;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the incorrectly formatted language tag.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string LanguageTag { get; }
#else
        [NotNull]
        public string LanguageTag { get; }
#endif

        #endregion
    }
}