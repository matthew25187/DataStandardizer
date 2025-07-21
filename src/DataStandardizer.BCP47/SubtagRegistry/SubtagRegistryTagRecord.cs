using System;
using System.Collections.Generic;
using static DataStandardizer.BCP47.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.BCP47.SubtagRegistry
{
    /// <summary>
    /// Tag record from the IANA Subtag Registry.
    /// </summary>
    public class SubtagRegistryTagRecord : SubtagRegistryTagRecordBase
    {
        internal SubtagRegistryTagRecord(IEnumerable<Tuple<string, object>> fields)
        {
            foreach (var field in fields)
            {
                ((IList<Tuple<string, object>>)this).Add(field);
            }
        }

        /// <summary>
        /// Gets the Tag field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Tag)]
#if NETCOREAPP3_0_OR_GREATER
        public string Tag => GetPropertyValue<string>()!;
#else
        public string Tag => GetPropertyValue<string>();
#endif
    }
}