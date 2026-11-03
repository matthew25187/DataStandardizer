using System;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Exception to normal processing of a CSV file.
    /// </summary>
    public sealed class CsvFileException : Exception
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
#if NET5_0_OR_GREATER
        public string? FilePath { get; init; } 
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? FilePath { get; internal set; }
#else
        [MaybeNull, AllowNull]
        public string FilePath { get; internal set; }
#endif
    }
}