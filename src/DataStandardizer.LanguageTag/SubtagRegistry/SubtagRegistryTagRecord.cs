using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Tag record from the IANA Subtag Registry.
    /// </summary>
    public sealed class SubtagRegistryTagRecord : SubtagRegistryTagRecordBase
    {
#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryTagRecord(KeyValuePair<string, string>[] fields)
#else
        internal SubtagRegistryTagRecord([NotNull] KeyValuePair<string, string>[] fields)
#endif
            : base(fields)
        {
            Tag = GetFirstValue(fields, FieldName.Tag) ?? string.Empty;
        }

        /// <summary>
        /// Gets the Tag field value.
        /// </summary>
        public string Tag { get; }
    }
}
