using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// State of a CSV reader or writer.
    /// </summary>
    public sealed class CsvContext
    {
        private readonly ReadOnlyDictionary<Type, ICsvFileMapper> _mappersWrapper;

        internal CsvContext(IDictionary<Type, ICsvFileMapper> mappers, ICsvFileOptions options)
        {
            Options = options;
            _mappersWrapper = new ReadOnlyDictionary<Type, ICsvFileMapper>(mappers);
        }

        /// <summary>
        /// Gets a collection of the mappers in use by the reader or writer.
        /// </summary>
        public IReadOnlyDictionary<Type, ICsvFileMapper> Mappers => _mappersWrapper;

        /// <summary>
        /// Gets the options used to configure the reader or writer.
        /// </summary>
        public ICsvFileOptions Options { get; }
    }
}