using System;
using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// File-Date record from the IANA Subtag Registry.
    /// </summary>
    public sealed class SubtagRegistryFileDateRecord : SubtagRegistryRecordBase
    {
#if NETCOREAPP3_0_OR_GREATER
        internal SubtagRegistryFileDateRecord(KeyValuePair<string, string>[] fields)
#else
        internal SubtagRegistryFileDateRecord([NotNull] KeyValuePair<string, string>[] fields)
#endif
            : base(fields)
        {
            var fileDate = GetFirstValue(fields, FieldName.FileDate);
            FileDate = (fileDate is null ? null : ParseDate(fileDate)) ?? default(DateTime);
        }

        /// <summary>
        /// Gets the File-Date field value, or the default <see cref="DateTime"/> when it is not a date in the <c>yyyy-MM-dd</c> form.
        /// </summary>
        public DateTime FileDate { get; }
    }
}
