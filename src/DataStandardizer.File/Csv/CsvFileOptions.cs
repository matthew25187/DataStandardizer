using System;
using System.Globalization;
using System.Text;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
{
    public interface ICsvFileOptions
    {
        /// <summary>
        /// Gets the delegate that will be called when a bad value is found in a CSV field.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        Delegate? BadValueHandler { get; }
#else
        [CanBeNull]
        Delegate BadValueHandler { get; }
#endif
        /// <summary>
        /// Gets the culture to use for converting field values.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        CultureInfo? Culture { get; }
#else
        [CanBeNull]
        CultureInfo Culture { get; }
#endif
        /// <summary>
        /// Gets the character sequence for line breaks embedded in CSV field values.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        string? EmbeddedLineBreak { get; }
#else
        [CanBeNull]
        string EmbeddedLineBreak { get; }
#endif

        /// <summary>
        /// Gets the encoding for the CSV file.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        Encoding? Encoding { get; }
#else
        [CanBeNull]
        Encoding Encoding { get; }
#endif
        /// <summary>
        /// Gets the character used to separate fields on a CSV line.
        /// </summary>
        char FieldDelimiterCharacter { get; }

        /// <summary>
        /// Gets a flag indicating if the reader should expect to find a header line in the CSV file.
        /// </summary>
        /// <remarks>
        /// Applies to reading lines only.
        /// </remarks>
        bool HasHeaderLine { get; }

        /// <summary>
        /// Gets the delegate that will be called when the header for a CSV file is being prepared.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        Delegate? HeaderHandler { get; }
#else
        Delegate HeaderHandler { get; }
#endif
        /// <summary>
        /// Gets the delegate that will be called when a line has a field count inconsistent with other lines in the CSV file.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        Delegate? InconsistentFieldCountHandler { get; }
#else
        [CanBeNull]
        Delegate InconsistentFieldCountHandler { get; }
#endif
        /// <summary>
        /// Gets the method for handling quoted field values.
        /// </summary>
        /// <remarks>
        /// Applies to writing lines only.
        /// </remarks>
        CsvFieldQuoteHandling QuoteHandling { get; }

        /// <summary>
        /// Gets a flag indicating if blank fields at the end of a line should be output to a CSV file.
        /// </summary>
        bool SuppressTrailingBlankFields { get; }

        /// <summary>
        /// Gets the character sequence for line breaks terminating CSV lines.
        /// </summary>
        string TerminatorLineBreak { get; }
    }

    /// <summary>
    /// Options to configure the behaviour of a CSV reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 4180.
    /// </remarks>
#if NET5_0_OR_GREATER
    public record CsvFileOptions
#else
    public class CsvFileOptions
#endif
        : ICsvFileOptions
    {
#if NETCOREAPP3_0_OR_GREATER

        public Delegate? BadValueHandler
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
#else
        public Delegate BadValueHandler { get; set; }
#endif
#if NETCOREAPP3_0_OR_GREATER
        public CultureInfo? Culture
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public CultureInfo Culture { get; set; }
#endif
#if NETCOREAPP3_0_OR_GREATER
        public string? EmbeddedLineBreak
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public string EmbeddedLineBreak { get; set; }
#endif
#if NETCOREAPP3_0_OR_GREATER
        public Encoding? Encoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public Encoding Encoding { get; set; }
#endif
        public char FieldDelimiterCharacter
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = ',';

        public bool HasHeaderLine
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }


#if NETCOREAPP3_0_OR_GREATER
        public Delegate? HeaderHandler
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public Delegate HeaderHandler { get; set; }
#endif

#if NETCOREAPP3_0_OR_GREATER
        public Delegate? InconsistentFieldCountHandler
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
set;
#endif
        }
#else
        public Delegate InconsistentFieldCountHandler { get; set; }
#endif

        public CsvFieldQuoteHandling QuoteHandling
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = CsvFieldQuoteHandling.Required;

        public bool SuppressTrailingBlankFields
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = false;

        public string TerminatorLineBreak
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = "\r\n";
    }
}