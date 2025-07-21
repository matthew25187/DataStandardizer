using System;
using System.Linq;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Core
{
    public abstract class CodeAttributeBase : Attribute
    {
        #region Declarations

        private readonly string[] _englishNames;
        private readonly string[] _frenchNames;

        #endregion

        #region Constructors

        protected CodeAttributeBase()
        {
#if NETSTANDARD1_3_OR_GREATER||NET
            _englishNames = Array.Empty<string>();
            _frenchNames = Array.Empty<string>();
#else
            _englishNames = new String[] { };
            _frenchNames = new String[] { };
#endif
        }

        protected CodeAttributeBase(string[] englishNames) : this()
        {
            _englishNames = englishNames;
        }

        protected CodeAttributeBase(string[] englishNames, string[] frenchNames) : this(englishNames)
        {
            _frenchNames = frenchNames;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the first English name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishName => _englishNames.FirstOrDefault();
#else
        [CanBeNull]
        public string EnglishName => _englishNames.FirstOrDefault();
#endif
        /// <summary>
        /// Gets a collection of all English names.
        /// </summary>
        public string[] EnglishNames => _englishNames;

        /// <summary>
        /// Gets the first French name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchName => _frenchNames.FirstOrDefault();
#else
        [CanBeNull]
        public string FrenchName => _frenchNames.FirstOrDefault();
#endif
        /// <summary>
        /// Gets a collection of all French names.
        /// </summary>
        public string[] FrenchNames => _frenchNames;

        #endregion
    }
}