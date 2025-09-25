using System;
using System.Collections.Generic;
using System.ComponentModel;
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// File-Date record from the IANA Subtag Registry.
    /// </summary>
    public class SubtagRegistryFileDateRecord : SubtagRegistryRecordBase
    {
        public SubtagRegistryFileDateRecord(Tuple<string, object> field)
        {
            ((IList<Tuple<string, object>>)this).Add(field);
        }

        /// <summary>
        /// Gets or sets the File-Date field value.
        /// </summary>
        [SubtagRegistryField(FieldName.FileDate)]
        [TypeConverter(typeof(DateTimeConverter))]
        public DateTime FileDate => GetPropertyValue<DateTime>();
    }
}