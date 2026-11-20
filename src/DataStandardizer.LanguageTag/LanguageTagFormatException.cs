using System;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
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
        /// <summary>
        /// Initialises a new instance of the <see cref="LanguageTagFormatException"/> class with an error message and the language tag that is not correctly formatted.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="languageTag">The incorrectly formatted language tag, which <see cref="LanguageTagFormatException.LanguageTag"/> returns.</param>
        public LanguageTagFormatException(string message, string languageTag)
#else
        /// <summary>
        /// Initialises a new instance of the <see cref="LanguageTagFormatException"/> class with an error message and the language tag that is not correctly formatted.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="languageTag">The incorrectly formatted language tag, which <see cref="LanguageTagFormatException.LanguageTag"/> returns.</param>
        public LanguageTagFormatException([DisallowNull] string message, [DisallowNull] string languageTag)
#endif
            : base(message)
        {
            LanguageTag = languageTag;
        }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Initialises a new instance of the <see cref="LanguageTagFormatException"/> class with an error message, the language tag that is not correctly formatted, and the exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="languageTag">The incorrectly formatted language tag, which <see cref="LanguageTagFormatException.LanguageTag"/> returns.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
        public LanguageTagFormatException(string message, string languageTag, Exception innerException)
#else
        /// <summary>
        /// Initialises a new instance of the <see cref="LanguageTagFormatException"/> class with an error message, the language tag that is not correctly formatted, and the exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="languageTag">The incorrectly formatted language tag, which <see cref="LanguageTagFormatException.LanguageTag"/> returns.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is specified.</param>
        public LanguageTagFormatException([DisallowNull] string message, [DisallowNull] string languageTag, Exception innerException)
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