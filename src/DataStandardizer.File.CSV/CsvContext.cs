using System;
using System.Collections.Generic;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// State of a CSV reader or writer.
    /// </summary>
    public sealed class CsvContext
    {
        internal CsvContext(IReadOnlyDictionary<Type, ICsvFileMapper> mappers, ICsvFileOptions options)
        {
            Options = options;
            Mappers = mappers;
        }

        /// <summary>
        /// Gets a collection of the mappers in use by the reader or writer.
        /// </summary>
        public IReadOnlyDictionary<Type, ICsvFileMapper> Mappers { get; }

        /// <summary>
        /// Gets the options used to configure the reader or writer.
        /// </summary>
        public ICsvFileOptions Options { get; }
    }
}