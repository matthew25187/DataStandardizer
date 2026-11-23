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
        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFileException"/> class.
        /// </summary>
        public CsvFileException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFileException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CsvFileException(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFileException"/> class with a specified error message and a reference to the inner
        /// exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <c>null</c> if no inner exception is
        /// specified.</param>
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