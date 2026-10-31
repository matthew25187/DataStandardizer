using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Subtag record from the IANA Subtag Registry.
    /// </summary>
    public sealed class SubtagRegistrySubtagRecord : SubtagRegistryTagRecordBase
    {
#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistrySubtagRecord(KeyValuePair<string, string>[] fields)
#else
        internal SubtagRegistrySubtagRecord([NotNull] KeyValuePair<string, string>[] fields)
#endif
            : base(fields)
        {
            Subtag = GetFirstValue(fields, FieldName.Subtag) ?? string.Empty;
        }

        /// <summary>
        /// Gets the Subtag field value.
        /// </summary>
        /// <remarks>
        /// The value may denote a range of subtags, such as <c>qaa..qtz</c> (ref. RFC 5646 §3.1.4).
        /// </remarks>
        public string Subtag { get; }
    }
}
