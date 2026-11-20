using System;
using System.Linq;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
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
        [MaybeNull]
        public string EnglishName => _englishNames.FirstOrDefault();
#endif
        /// <summary>
        /// Gets a collection of all English names.
        /// </summary>
        /// <value>
        /// A copy of the attribute's names. Changing it doesn't change the attribute.
        /// </value>
        public string[] EnglishNames => CopyNames(_englishNames);

        /// <summary>
        /// Gets the first French name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FrenchName => _frenchNames.FirstOrDefault();
#else
        [MaybeNull]
        public string FrenchName => _frenchNames.FirstOrDefault();
#endif
        /// <summary>
        /// Gets a collection of all French names.
        /// </summary>
        /// <value>
        /// A copy of the attribute's names. Changing it doesn't change the attribute.
        /// </value>
        public string[] FrenchNames => CopyNames(_frenchNames);

        #endregion

        #region Private Methods

        private static string[] CopyNames(string[] names)
        {
            if (names.Length == 0)
            {
                return names;
            }

            var copy = new string[names.Length];
            Array.Copy(names, copy, names.Length);
            return copy;
        }

        #endregion
    }
}