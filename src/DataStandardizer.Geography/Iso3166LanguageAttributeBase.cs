using System;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Geography
{
    /// <summary>
    /// Base class of the attributes that associate an ISO 3166 code, or a name of it, with a language identified by its ISO 639 codes.
    /// </summary>
    public abstract class Iso3166LanguageAttributeBase : Attribute
    {
#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Initialises a new instance of the <see cref="Iso3166LanguageAttributeBase"/> class.
        /// </summary>
        /// <param name="iso639Part1Code">The ISO 639 Part 1 code of the language, or <c>null</c> if the language has none.</param>
        /// <param name="iso639Part2TCode">The ISO 639 Part 2T code of the language.</param>
        protected internal Iso3166LanguageAttributeBase(string? iso639Part1Code, string iso639Part2TCode)
#else
        /// <summary>
        /// Initialises a new instance of the <see cref="Iso3166LanguageAttributeBase"/> class.
        /// </summary>
        /// <param name="iso639Part1Code">The ISO 639 Part 1 code of the language, or <c>null</c> if the language has none.</param>
        /// <param name="iso639Part2TCode">The ISO 639 Part 2T code of the language.</param>
        protected internal Iso3166LanguageAttributeBase([AllowNull] string iso639Part1Code, [DisallowNull] string iso639Part2TCode)
#endif
        {
            Iso639Part1Code = iso639Part1Code;
            Iso639Part2TCode = iso639Part2TCode;
        }

        /// <summary>
        /// Gets the ISO 639 Part 1 language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Iso639Part1Code { get; }
#else
        [MaybeNull]
        public string Iso639Part1Code { get; }
#endif
        /// <summary>
        /// Gets the ISO 639 Part 2T language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string Iso639Part2TCode { get; }
#else
        [NotNull]
        public string Iso639Part2TCode { get; }
#endif
    }
}