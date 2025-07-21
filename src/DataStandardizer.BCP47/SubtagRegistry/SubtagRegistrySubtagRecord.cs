using System;
using System.Collections.Generic;
using static DataStandardizer.BCP47.SubtagRegistry.SubtagRegistryConstants;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.BCP47.SubtagRegistry
{
    /// <summary>
    /// Subtag record from the IANA Subtag Registry.
    /// </summary>
    public class SubtagRegistrySubtagRecord : SubtagRegistryTagRecordBase
    {
        internal SubtagRegistrySubtagRecord(IEnumerable<Tuple<string, object>> fields)
        {
            foreach (var field in fields)
            {
                ((IList<Tuple<string, object>>)this).Add(field);
            }
        }

        /// <summary>
        /// Gets the Subtag field value.
        /// </summary>
        [SubtagRegistryField(FieldName.Subtag)]
#if NETCOREAPP3_0_OR_GREATER
        public string Subtag => GetPropertyValue<string>()!;
#else
        public string Subtag => GetPropertyValue<string>();
#endif
    }
}