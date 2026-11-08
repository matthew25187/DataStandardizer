using System;
using System.Collections.Generic;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif
using static DataStandardizer.LanguageTag.Registry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// Tag record from the IANA Subtag Registry.
    /// </summary>
    public sealed class SubtagRegistryTagRecord : SubtagRegistryTagRecordBase
    {
#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryTagRecord(KeyValuePair<string, string>[] fields, DateTime? added, DateTime? deprecated)
#else
        internal SubtagRegistryTagRecord([DisallowNull] KeyValuePair<string, string>[] fields, DateTime? added, DateTime? deprecated)
#endif
            : base(fields, added, deprecated)
        {
            Tag = GetFirstValue(fields, FieldName.Tag) ?? string.Empty;
        }

        /// <summary>
        /// Gets the Tag field value.
        /// </summary>
        public string Tag { get; }
    }
}
