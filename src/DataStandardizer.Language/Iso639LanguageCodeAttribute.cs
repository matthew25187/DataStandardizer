using System;
using DataStandardizer.Core;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Language
{
    /// <summary>
    /// Describes an ISO 639 code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso639LanguageCodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with an English name.
        /// </summary>
        /// <param name="englishName">English name of the language code.</param>
        public Iso639LanguageCodeAttribute(string englishName) : base([englishName])
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with an English name and a
        /// French name.
        /// </summary>
        /// <param name="englishName">English name of the language code.</param>
        /// <param name="frenchName">French name of the language code.</param>
        public Iso639LanguageCodeAttribute(string englishName, string frenchName) : base([englishName], [frenchName])
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with an English name, a
        /// scope and a language type.
        /// </summary>
        /// <param name="englishName">English name of the language code.</param>
        /// <param name="scope">Scope of the language code.</param>
        /// <param name="languageType">Type of language the code represents.</param>
        public Iso639LanguageCodeAttribute(string englishName, Iso639LanguageScope scope, Iso639LanguageType languageType):this(englishName)
        {
            Scope = scope;
            LanguageType = languageType;
        }
#else
        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with an English name.
        /// </summary>
        /// <param name="englishName">English name of the language code.</param>
        public Iso639LanguageCodeAttribute([DisallowNull] string englishName) : base(new[] { englishName })
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with an English name and a
        /// French name.
        /// </summary>
        /// <param name="englishName">English name of the language code.</param>
        /// <param name="frenchName">French name of the language code.</param>
        public Iso639LanguageCodeAttribute([DisallowNull] string englishName, [DisallowNull] string frenchName) : base(new[] { englishName }, new[] { frenchName })
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with an English name, a
        /// scope and a language type.
        /// </summary>
        /// <param name="englishName">English name of the language code.</param>
        /// <param name="scope">Scope of the language code.</param>
        /// <param name="languageType">Type of language the code represents.</param>
        public Iso639LanguageCodeAttribute([DisallowNull] string englishName, Iso639LanguageScope scope, Iso639LanguageType languageType) : this(englishName)
        {
            Scope = scope;
            LanguageType = languageType;
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with English names.
        /// </summary>
        /// <param name="englishNames">English names of the language code.</param>
#if NETCOREAPP3_0_OR_GREATER
        public Iso639LanguageCodeAttribute(string[] englishNames) : base(englishNames)
#else
        public Iso639LanguageCodeAttribute([DisallowNull] string[] englishNames) : base(englishNames)
#endif
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with English names and
        /// French names.
        /// </summary>
        /// <param name="englishNames">English names of the language code.</param>
        /// <param name="frenchNames">French names of the language code.</param>
#if NETCOREAPP3_0_OR_GREATER
        public Iso639LanguageCodeAttribute(string[] englishNames, string[] frenchNames) : base(englishNames, frenchNames)
#else
        public Iso639LanguageCodeAttribute([DisallowNull] string[] englishNames, [DisallowNull] string[] frenchNames) : base(englishNames, frenchNames)
#endif
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Iso639LanguageCodeAttribute"/> class with English names, a
        /// scope and a language type.
        /// </summary>
        /// <param name="englishNames">English names of the language code.</param>
        /// <param name="scope">Scope of the language code.</param>
        /// <param name="languageType">Type of language the code represents.</param>
#if NETCOREAPP3_0_OR_GREATER
        public Iso639LanguageCodeAttribute(string[] englishNames, Iso639LanguageScope scope, Iso639LanguageType languageType) : this(englishNames)
#else
        public Iso639LanguageCodeAttribute([DisallowNull] string[] englishNames, Iso639LanguageScope scope, Iso639LanguageType languageType) : this(englishNames)
#endif
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
        [MaybeNull, AllowNull]
        public string Part1Code { get; set; }
#endif
        /// <summary>
        /// Gets or sets the related ISO 639 Part 2B language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Part2BCode { get; set; }
#else
        [MaybeNull, AllowNull]
        public string Part2BCode { get; set; }
#endif
        /// <summary>
        /// Gets or sets the related ISO 639 Part 2T language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Part2TCode { get; set; }
#else
        [MaybeNull, AllowNull]
        public string Part2TCode { get; set; }
#endif
        /// <summary>
        /// Gets the scope for the language code, or <c>null</c> if it has none.
        /// </summary>
        public Iso639LanguageScope? Scope { get; }

        /// <summary>
        /// Gets the type for the language code, or <c>null</c> if it has none.
        /// </summary>
        public Iso639LanguageType? LanguageType { get; }

        /// <summary>
        /// Gets or sets the print name for the language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? PrintName { get; set; }
#else
        [MaybeNull, AllowNull]
        public string PrintName { get; set; }
#endif
        /// <summary>
        /// Gets or sets the inverted name for the language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? InvertedName { get; set; }
#else
        [MaybeNull, AllowNull]
        public string InvertedName { get; set; }
#endif
        /// <summary>
        /// Gets or sets the related macrolanguage code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? MacrolanguageCode { get; set; }
#else
        [MaybeNull, AllowNull]
        public string MacrolanguageCode { get; set; }
#endif

        #endregion
    }
}