using System;
using System.Collections.Generic;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif
using static DataStandardizer.LanguageTag.Registry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// Subtag record from the IANA Subtag Registry.
    /// </summary>
    public sealed class SubtagRegistrySubtagRecord : SubtagRegistryTagRecordBase
    {
#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistrySubtagRecord(KeyValuePair<string, string>[] fields, DateTime added, DateTime? deprecated)
#else
        internal SubtagRegistrySubtagRecord([DisallowNull] KeyValuePair<string, string>[] fields, DateTime added, DateTime? deprecated)
#endif
            : base(fields, added, deprecated)
        {
            Subtag = GetFirstValue(fields, FieldName.Subtag) ?? string.Empty;
        }

        /// <summary>
        /// Gets the Subtag field value.
        /// </summary>
        /// <remarks>
        /// The value is a subtag of the record's <see cref="SubtagRegistryTagRecordBase.Type"/> in the case that RFC 5646 §3.1.4 gives it, or a range
        /// of such subtags, such as <c>qaa..qtz</c> (ref. RFC 5646 §3.1.1).
        /// </remarks>
        public string Subtag { get; }
    }
}
