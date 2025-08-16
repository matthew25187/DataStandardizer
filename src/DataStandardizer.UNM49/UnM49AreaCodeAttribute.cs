using System;
using System.Linq;
using DataStandardizer.Core;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.UNM49
{
    /// <summary>
    /// Describes a UN M49 code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnM49AreaCodeAttribute : CodeAttributeBase
    {
        #region Declarations

        private enum NameIndex
        {
            Global,
            Region,
            SubRegion,
            IntermediateRegion,
            CountryOrArea
        }

        private readonly string[] _arabicNames;
        private readonly string[] _chineseNames;
        private readonly string[] _russianNames;
        private readonly string[] _spanishNames;

        #endregion

        #region Constructors

        private UnM49AreaCodeAttribute()
            : base(Enumerable.Repeat(string.Empty, Enum.GetValues(typeof(NameIndex)).Length).ToArray(), Enumerable.Repeat(string.Empty, Enum.GetValues(typeof(NameIndex)).Length).ToArray())
        {
            var nameCount = Enum.GetValues(typeof(NameIndex)).Length;
            _chineseNames = Enumerable.Repeat(string.Empty, nameCount).ToArray();
            _russianNames = Enumerable.Repeat(string.Empty, nameCount).ToArray();
            _spanishNames = Enumerable.Repeat(string.Empty, nameCount).ToArray();
            _arabicNames = Enumerable.Repeat(string.Empty, nameCount).ToArray();
        }

        public UnM49AreaCodeAttribute(ushort globalCode) : this()
        {
            GlobalCode = globalCode;
        }

        public UnM49AreaCodeAttribute(ushort globalCode, ushort regionCode) : this(globalCode)
        {
            RegionCode = regionCode;
        }

        public UnM49AreaCodeAttribute(ushort globalCode, ushort regionCode, ushort subRegionCode) : this(globalCode, regionCode)
        {
            SubRegionCode = subRegionCode;
        }

        public UnM49AreaCodeAttribute(ushort globalCode, ushort regionCode, ushort subRegionCode, ushort intermediateRegionCode) : this(globalCode, regionCode, subRegionCode)
        {
            IntermediateRegionCode = intermediateRegionCode;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the global code related to this UN M49 code.
        /// </summary>
        public ushort? GlobalCode { get; }

        /// <summary>
        /// Gets or sets the English name for the global code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishGlobalName
#else
        [CanBeNull]
        public string EnglishGlobalName
#endif
        {
            get => !string.IsNullOrEmpty(EnglishNames[(int)NameIndex.Global]) ? EnglishNames[(int)NameIndex.Global] : null;
            set => EnglishNames[(int)NameIndex.Global] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Chinese name for the global code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ChineseGlobalName
#else
        [CanBeNull]
        public string ChineseGlobalName
#endif
        {
            get => !string.IsNullOrEmpty(_chineseNames[(int)NameIndex.Global]) ? _chineseNames[(int)NameIndex.Global] : null;
            set => _chineseNames[(int)NameIndex.Global] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Russian name for the global code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RussianGlobalName
#else
        [CanBeNull]
        public string RussianGlobalName
#endif
        {
            get => !string.IsNullOrEmpty(_russianNames[(int)NameIndex.Global]) ? _russianNames[(int)NameIndex.Global] : null;
            set => _russianNames[(int)NameIndex.Global] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the French name for the global code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchGlobalName
#else
        [CanBeNull]
        public string FrenchGlobalName
#endif
        {
            get => !string.IsNullOrEmpty(FrenchNames[(int)NameIndex.Global]) ? FrenchNames[(int)NameIndex.Global] : null;
            set => FrenchNames[(int)NameIndex.Global] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Spanish name for the global code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SpanishGlobalName
#else
        [CanBeNull]
        public string SpanishGlobalName
#endif
        {
            get => !string.IsNullOrEmpty(_spanishNames[(int)NameIndex.Global]) ? _spanishNames[(int)NameIndex.Global] : null;
            set => _spanishNames[(int)NameIndex.Global] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Arabic name for the global code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ArabicGlobalName
#else
        [CanBeNull]
        public string ArabicGlobalName
#endif
        {
            get => !string.IsNullOrEmpty(_arabicNames[(int)NameIndex.Global]) ? _arabicNames[(int)NameIndex.Global] : null;
            set => _arabicNames[(int)NameIndex.Global] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the region code related to this UN M49 code.
        /// </summary>
        public ushort? RegionCode { get; }

        /// <summary>
        /// Gets or sets the English name for the region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishRegionName
#else
        [CanBeNull]
        public string EnglishRegionName
#endif
        {
            get => !string.IsNullOrEmpty(EnglishNames[(int)NameIndex.Region]) ? EnglishNames[(int)NameIndex.Region] : null;
            set => EnglishNames[(int)NameIndex.Region] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Chinese name for the region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ChineseRegionName
#else
        [CanBeNull]
        public string ChineseRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_chineseNames[(int)NameIndex.Region]) ? _chineseNames[(int)NameIndex.Region] : null;
            set => _chineseNames[(int)NameIndex.Region] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Russian name for the region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RussianRegionName
#else
        [CanBeNull]
        public string RussianRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_russianNames[(int)NameIndex.Region]) ? _russianNames[(int)NameIndex.Region] : null;
            set => _russianNames[(int)NameIndex.Region] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the French name for the region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchRegionName
#else
        [CanBeNull]
        public string FrenchRegionName
#endif
        {
            get => !string.IsNullOrEmpty(FrenchNames[(int)NameIndex.Region]) ? FrenchNames[(int)NameIndex.Region] : null;
            set => FrenchNames[(int)NameIndex.Region] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Spanish name for the region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SpanishRegionName
#else
        [CanBeNull]
        public string SpanishRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_spanishNames[(int)NameIndex.Region]) ? _spanishNames[(int)NameIndex.Region] : null;
            set => _spanishNames[(int)NameIndex.Region] = value ?? String.Empty;
        }

        /// <summary>
        /// Gets or sets the Arabic name for the region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ArabicRegionName
#else
        [CanBeNull]
        public string ArabicRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_arabicNames[(int)NameIndex.Region]) ? _arabicNames[(int)NameIndex.Region] : null;
            set => _arabicNames[(int)NameIndex.Region] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the sub-region code related to this UN M49 code.
        /// </summary>
        public ushort? SubRegionCode { get; }

        /// <summary>
        /// Gets or sets the English name for the sub-region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishSubRegionName
#else
        [CanBeNull]
        public string EnglishSubRegionName
#endif
        {
            get => !string.IsNullOrEmpty(EnglishNames[(int)NameIndex.SubRegion]) ? EnglishNames[(int)NameIndex.SubRegion] : null;
            set => EnglishNames[(int)NameIndex.SubRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Chinese name for the sub-region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ChineseSubRegionName
#else
        [CanBeNull]
        public string ChineseSubRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_chineseNames[(int)NameIndex.SubRegion]) ? _chineseNames[(int)NameIndex.SubRegion] : null;
            set => _chineseNames[(int)NameIndex.SubRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Russian name for the sub-region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RussianSubRegionName
#else
        [CanBeNull]
        public string RussianSubRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_russianNames[(int)NameIndex.SubRegion]) ? _russianNames[(int)NameIndex.SubRegion] : null;
            set => _russianNames[(int)NameIndex.SubRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the French name for the sub-region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchSubRegionName
#else
        [CanBeNull]
        public string FrenchSubRegionName
#endif
        {
            get => !string.IsNullOrEmpty(FrenchNames[(int)NameIndex.SubRegion]) ? FrenchNames[(int)NameIndex.SubRegion] : null;
            set => FrenchNames[(int)NameIndex.SubRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Spanish name for the sub-region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SpanishSubRegionName
#else
        [CanBeNull]
        public string SpanishSubRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_spanishNames[(int)NameIndex.SubRegion]) ? _spanishNames[(int)NameIndex.SubRegion] : null;
            set => _spanishNames[(int)NameIndex.SubRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Arabic name for the sub-region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ArabicSubRegionName
#else
        [CanBeNull]
        public string ArabicSubRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_arabicNames[(int)NameIndex.SubRegion]) ? _arabicNames[(int)NameIndex.SubRegion] : null;
            set => _arabicNames[(int)NameIndex.SubRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the intermediate region code related to this UN M49 code.
        /// </summary>
        public ushort? IntermediateRegionCode { get; }

        /// <summary>
        /// Gets or sets the English name for the intermediate region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishIntermediateRegionName
#else
        [CanBeNull]
        public string EnglishIntermediateRegionName
#endif
        {
            get => !string.IsNullOrEmpty(EnglishNames[(int)NameIndex.IntermediateRegion]) ? EnglishNames[(int)NameIndex.IntermediateRegion] : null;
            set => EnglishNames[(int)NameIndex.IntermediateRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Chinese name for the intermediate region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ChineseIntermediateRegionName
#else
        [CanBeNull]
        public string ChineseIntermediateRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_chineseNames[(int)NameIndex.IntermediateRegion]) ? _chineseNames[(int)NameIndex.IntermediateRegion] : null;
            set => _chineseNames[(int)NameIndex.IntermediateRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Russian name for the intermediate region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RussianIntermediateRegionName
#else
        [CanBeNull]
        public string RussianIntermediateRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_russianNames[(int)NameIndex.IntermediateRegion]) ? _russianNames[(int)NameIndex.IntermediateRegion] : null;
            set => _russianNames[(int)NameIndex.IntermediateRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the French name for the intermediate region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchIntermediateRegionName
#else
        [CanBeNull]
        public string FrenchIntermediateRegionName
#endif
        {
            get => !string.IsNullOrEmpty(FrenchNames[(int)NameIndex.IntermediateRegion]) ? FrenchNames[(int)NameIndex.IntermediateRegion] : null;
            set => FrenchNames[(int)NameIndex.IntermediateRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Spanish name for the intermediate region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SpanishIntermediateRegionName
#else
        [CanBeNull]
        public string SpanishIntermediateRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_spanishNames[(int)NameIndex.IntermediateRegion]) ? _spanishNames[(int)NameIndex.IntermediateRegion] : null;
            set => _spanishNames[(int)NameIndex.IntermediateRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Arabic name for the intermediate region code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ArabicIntermediateRegionName
#else
        [CanBeNull]
        public string ArabicIntermediateRegionName
#endif
        {
            get => !string.IsNullOrEmpty(_arabicNames[(int)NameIndex.IntermediateRegion]) ? _arabicNames[(int)NameIndex.IntermediateRegion] : null;
            set => _arabicNames[(int)NameIndex.IntermediateRegion] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the English name for the M49 code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishCountryOrAreaName
#else
        [CanBeNull]
        public string EnglishCountryOrAreaName
#endif
        {
            get => !string.IsNullOrEmpty(EnglishNames[(int)NameIndex.CountryOrArea]) ? EnglishNames[(int)NameIndex.CountryOrArea] : null;
            set => EnglishNames[(int)NameIndex.CountryOrArea] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Chinese name for the M49 code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ChineseCountryOrAreaName
#else
        [CanBeNull]
        public string ChineseCountryOrAreaName
#endif
        {
            get => !string.IsNullOrEmpty(_chineseNames[(int)NameIndex.CountryOrArea]) ? _chineseNames[(int)NameIndex.CountryOrArea] : null;
            set => _chineseNames[(int)NameIndex.CountryOrArea] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Russian name for the M49 code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RussianCountryOrAreaName
#else
        [CanBeNull]
        public string RussianCountryOrAreaName
#endif
        {
            get => !string.IsNullOrEmpty(_russianNames[(int)NameIndex.CountryOrArea]) ? _russianNames[(int)NameIndex.CountryOrArea] : null;
            set => _russianNames[(int)NameIndex.CountryOrArea] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the French name for the M49 code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchCountryOrAreaName
#else
        [CanBeNull]
        public string FrenchCountryOrAreaName
#endif
        {
            get => !string.IsNullOrEmpty(FrenchNames[(int)NameIndex.CountryOrArea]) ? FrenchNames[(int)NameIndex.CountryOrArea] : null;
            set => FrenchNames[(int)NameIndex.CountryOrArea] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Spanish name for the M49 code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SpanishCountryOrAreaName
#else
        [CanBeNull]
        public string SpanishCountryOrAreaName
#endif
        {
            get => !string.IsNullOrEmpty(_spanishNames[(int)NameIndex.CountryOrArea]) ? _spanishNames[(int)NameIndex.CountryOrArea] : null;
            set => _spanishNames[(int)NameIndex.CountryOrArea] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the Arabic name for the M49 code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? ArabicCountryOrAreaName
#else
        [CanBeNull]
        public string ArabicCountryOrAreaName
#endif
        {
            get => !string.IsNullOrEmpty(_arabicNames[(int)NameIndex.CountryOrArea]) ? _arabicNames[(int)NameIndex.CountryOrArea] : null;
            set => _arabicNames[(int)NameIndex.CountryOrArea] = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the ISO 3166 Part 1 Alpha-2 language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Iso3166Part1Alpha2Code { get; set; }
#else
        [CanBeNull]
        public string Iso3166Part1Alpha2Code { get; set; }
#endif
        /// <summary>
        /// Gets or sets the ISO 3166 Part 1 Alpha-3 language code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Iso3166Part1Alpha3Code { get; set; }
#else
        [CanBeNull]
        public string Iso3166Part1Alpha3Code { get; set; }
#endif

        #endregion
    }
}