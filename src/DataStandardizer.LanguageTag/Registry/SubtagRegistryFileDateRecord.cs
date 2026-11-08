using System;
using System.Collections.Generic;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif
using static DataStandardizer.LanguageTag.Registry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// File-Date record from the IANA Subtag Registry.
    /// </summary>
    public sealed class SubtagRegistryFileDateRecord : SubtagRegistryRecordBase
    {
#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryFileDateRecord(KeyValuePair<string, string>[] fields)
#else
        internal SubtagRegistryFileDateRecord([DisallowNull] KeyValuePair<string, string>[] fields)
#endif
            : base(fields)
        {
            var fileDate = GetFirstValue(fields, FieldName.FileDate);
            // the registry has checked that File-Date is a full-date before it creates the record
            FileDate = (fileDate is null ? null : ParseDate(fileDate)) ?? default(DateTime);
        }

        /// <summary>
        /// Gets the File-Date field value.
        /// </summary>
        public DateTime FileDate { get; }
    }
}
