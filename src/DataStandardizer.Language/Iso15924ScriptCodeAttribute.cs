using System;
using System.Globalization;
using DataStandardizer.Core;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Language
{
    /// <summary>
    /// Describes an <see cref="DataStandardizer.Language.Iso15924Script"/> code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso15924ScriptCodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso15924ScriptCodeAttribute(string englishName, string frenchName) : base([englishName], [frenchName])
        {

        }
#else
        internal Iso15924ScriptCodeAttribute([DisallowNull] string englishName, [DisallowNull] string frenchName) : base(new[] { englishName }, new[] { frenchName })
        {
        }
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal Iso15924ScriptCodeAttribute(string englishName, string frenchName, string date)
#else
        internal Iso15924ScriptCodeAttribute([DisallowNull] string englishName, [DisallowNull] string frenchName, [DisallowNull] string date)
#endif
            : this(englishName, frenchName)
        {
#if NET6_0_OR_GREATER
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                throw new System.ArgumentException($"'{date}' is not a valid date.", nameof(date));

            Date = dateValue;
#else
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateValue))
                throw new ArgumentException($"'{date}' is not a valid date.", nameof(date));

            Date = dateValue;
#endif
        }

#if NETCOREAPP3_0_OR_GREATER
        internal Iso15924ScriptCodeAttribute(string englishName, string frenchName, string date, double age)
#else
        internal Iso15924ScriptCodeAttribute([DisallowNull] string englishName, [DisallowNull] string frenchName, [DisallowNull] string date, double age)
#endif
            : this(englishName, frenchName, date)
        {
            Age = age;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the age of the script code.
        /// </summary>
        public double? Age { get; }

        /// <summary>
        /// Gets the alias for the script code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? Alias { get; set; }
#else
        [MaybeNull, AllowNull]
        public string Alias { get; set; }
#endif
        /// <summary>
        /// Gets the date of the script code.
        /// </summary>
#if NET6_0_OR_GREATER
        public DateOnly? Date { get; }
#else
        public DateTime? Date { get; }
#endif

        #endregion
    }
}