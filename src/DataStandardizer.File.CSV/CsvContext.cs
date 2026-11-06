using System;
using System.Collections.Generic;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// State of a CSV reader.
    /// </summary>
    public sealed class CsvContext
    {
        internal CsvContext(IReadOnlyDictionary<Type, ICsvFileMapper> mappers, ICsvFileOptions options)
        {
            Options = options;
            Mappers = mappers;
        }

        /// <summary>
        /// Gets the mapper registered on the reader with <see cref="CsvFileIoBase{TRecordLine}.RegisterMapper{TMapper}"/>, keyed by its
        /// record-line type, or no entries if none is registered.
        /// </summary>
        /// <remarks>
        /// The collection is a live view, so reflects mappers registered or unregistered after it was read.  Mappers built from
        /// <see cref="CsvFieldMappingAttribute"/> attributes are held in <see cref="CsvMapperCache"/>, not here.
        /// </remarks>
        public IReadOnlyDictionary<Type, ICsvFileMapper> Mappers { get; }

        /// <summary>
        /// Gets the options used to configure the reader.
        /// </summary>
        public ICsvFileOptions Options { get; }
    }
}