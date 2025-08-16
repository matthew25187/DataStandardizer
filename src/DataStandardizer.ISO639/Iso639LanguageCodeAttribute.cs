using System;
using DataStandardizer.Core;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.ISO639
{
    /// <summary>
    /// Describes an ISO 639 code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso639LanguageCodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        public Iso639LanguageCodeAttribute(string englishName) : base([englishName])
        {

        }

        public Iso639LanguageCodeAttribute(string englishName, string frenchName) : base([englishName], [frenchName])
        {

        }

        public Iso639LanguageCodeAttribute(string englishName, Iso639LanguageScope scope, Iso639LanguageType languageType):this(englishName)
        {
            Scope = scope;
            LanguageType = languageType;
        }
#else
        public Iso639LanguageCodeAttribute([NotNull] string englishName) : base(new[] { englishName })
        {

        }

        public Iso639LanguageCodeAttribute([NotNull] string englishName, [NotNull] string frenchName) : base(new[] { englishName }, new[] { frenchName })
        {

        }

        public Iso639LanguageCodeAttribute(string englishName, Iso639LanguageScope scope, Iso639LanguageType languageType) : this(englishName)
        {
            Scope = scope;
            LanguageType = languageType;
        }
#endif

        public Iso639LanguageCodeAttribute(string[] englishNames) : base(englishNames)
        {

        }

        public Iso639LanguageCodeAttribute(string[] englishNames, string[] frenchNames) : base(englishNames, frenchNames)
        {

        }

        public Iso639LanguageCodeAttribute(string[] englishNames, Iso639LanguageScope scope, Iso639LanguageType languageType) : this(englishNames)
        {
            Scope = scope;
            LanguageType = languageType;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the related ISO 639 Part 1 language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Part1Code { get; set; }
#else
        [CanBeNull]
        public string Part1Code { get; set; }
#endif
        /// <summary>
        /// Gets or sets the related ISO 639 Part 2B language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Part2BCode { get; set; }
#else
        [CanBeNull]
        public string Part2BCode { get; set; }
#endif
        /// <summary>
        /// Gets or sets the related ISO 639 Part 2T language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Part2TCode { get; set; }
#else
        [CanBeNull]
        public string Part2TCode { get; set; }
#endif
        /// <summary>
        /// Gets or sets the scope for the language code.
        /// </summary>
        public Iso639LanguageScope? Scope { get; }

        /// <summary>
        /// Gets or sets the type for the language code.
        /// </summary>
        public Iso639LanguageType? LanguageType { get; }

        /// <summary>
        /// Gets or sets the print name for the language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? PrintName { get; set; }
#else
        [CanBeNull]
        public string PrintName { get; set; }
#endif
        /// <summary>
        /// Gets or sets the inverted name for the language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? InvertedName { get; set; }
#else
        [CanBeNull]
        public string InvertedName { get; set; }
#endif
        /// <summary>
        /// Gets or sets the related macrolanguage code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? MacrolanguageCode { get; set; }
#else
        [CanBeNull]
        public string MacrolanguageCode { get; set; }
#endif

        #endregion
    }
}