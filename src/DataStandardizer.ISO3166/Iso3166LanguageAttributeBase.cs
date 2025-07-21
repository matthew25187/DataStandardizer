using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.ISO3166
{
    public abstract class Iso3166LanguageAttributeBase : Attribute
    {
#if NETCOREAPP3_0_OR_GREATER
        protected internal Iso3166LanguageAttributeBase(string? iso639Part1Code, string iso639Part2TCode)
#else
        protected internal Iso3166LanguageAttributeBase([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode)
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
        [CanBeNull]
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