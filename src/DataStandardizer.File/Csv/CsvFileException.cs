using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Exception to normal processing of a CSV file.
    /// </summary>
    public class CsvFileException : Exception
    {
        public CsvFileException()
        {

        }

        public CsvFileException(string message) : base(message)
        {

        }

        public CsvFileException(string message, Exception innerException) : base(message, innerException)
        {

        }

        /// <summary>
        /// Gets the path to the CSV file.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FilePath { get; init; } 
#else
        [CanBeNull]
        public string FilePath { get; internal set; }
#endif
    }
}