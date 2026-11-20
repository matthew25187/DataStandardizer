using System;
using System.Linq;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Core
{
    /// <summary>
    /// Base class of the attributes that describe a member of a standards code enumeration, giving the code's English and French names.
    /// </summary>
    /// <remarks>
    /// A code may have more than one name in a language. <see cref="EnglishName"/> and <see cref="FrenchName"/> give the first of each.
    /// </remarks>
    public abstract class CodeAttributeBase : Attribute
    {
        #region Declarations

        private readonly string[] _englishNames;
        private readonly string[] _frenchNames;

        #endregion

        #region Constructors

        /// <summary>
        /// Initialises a new instance of the <see cref="CodeAttributeBase"/> class with no English or French names.
        /// </summary>
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

        /// <summary>
        /// Initialises a new instance of the <see cref="CodeAttributeBase"/> class with English names and no French names.
        /// </summary>
        /// <param name="englishNames">The English names of the code, the first of which is <see cref="EnglishName"/>.</param>
        protected CodeAttributeBase(string[] englishNames) : this()
        {
            _englishNames = englishNames;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="CodeAttributeBase"/> class with English and French names.
        /// </summary>
        /// <param name="englishNames">The English names of the code, the first of which is <see cref="EnglishName"/>.</param>
        /// <param name="frenchNames">The French names of the code, the first of which is <see cref="FrenchName"/>.</param>
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
            // A derived attribute outside this library may pass null to a protected constructor; return it as before.
            if (names == null || names.Length == 0)
            {
#if NETCOREAPP3_0_OR_GREATER
                return names!;
#else
                return names;
#endif
            }

            var copy = new string[names.Length];
            Array.Copy(names, copy, names.Length);
            return copy;
        }

        #endregion
    }
}