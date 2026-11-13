using System;
using System.Globalization;
using System.Text;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    public interface ICsvFileOptions
    {
        /// <summary>
        /// Gets the <see cref="CsvFieldBadValue{TModel}"/> delegate that will be called when a bad value is found in a CSV field.
        /// </summary>
        /// <remarks>
        /// <c>TModel</c> must be the record line type of the reader.  Its constructor throws <see cref="System.ArgumentException"/> for a delegate of
        /// any other type.  A writer never calls the delegate and doesn't check it.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Delegate? BadValueHandler { get; }
#else
        [MaybeNull]
        Delegate BadValueHandler { get; }
#endif
        /// <summary>
        /// Gets the culture to use for converting field values.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        CultureInfo? Culture { get; }
#else
        [MaybeNull]
        CultureInfo Culture { get; }
#endif
        /// <summary>
        /// Gets the character sequence for line breaks embedded in CSV field values.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        string? EmbeddedLineBreak { get; }
#else
        [MaybeNull]
        string EmbeddedLineBreak { get; }
#endif

        /// <summary>
        /// Gets the encoding for the CSV file.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Encoding? Encoding { get; }
#else
        [MaybeNull]
        Encoding Encoding { get; }
#endif
        /// <summary>
        /// Gets the character used to separate fields on a CSV line.
        /// </summary>
        char FieldDelimiterCharacter { get; }

        /// <summary>
        /// Gets the <see cref="CsvFileHeader"/> delegate that will be called to provide the field names, in place of any header line in the file.
        /// </summary>
        /// <remarks>
        /// A reader calls the delegate whatever <see cref="HeaderHandling"/> is, and passes it the header line read from the file, if any.
        /// The reader constructor throws <see cref="System.ArgumentException"/> for a delegate of any other type.  A writer never calls the delegate
        /// and doesn't check it.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Delegate? HeaderHandler { get; }
#else
        Delegate HeaderHandler { get; }
#endif
        /// <summary>
        /// Gets the method for handling file header lines.
        /// </summary>
        /// <remarks>
        /// Applies to reading lines only.
        /// </remarks>
        CsvFileHeaderHandling HeaderHandling { get; }

        /// <summary>
        /// Gets the <see cref="CsvFieldCount{TModel}"/> delegate that will be called when a line has a field count inconsistent with other lines in the CSV file,
        /// or a line to be written has a field with a value that isn't one of the fields written.
        /// </summary>
        /// <remarks>
        /// The line the delegate is called for is skipped: a reader doesn't return it, and a writer writes nothing for it.  When the delegate isn't set,
        /// an inconsistent line throws <see cref="CsvFileException"/>.
        /// <para>
        /// <c>TModel</c> must be the record line type of the reader or writer.  Its constructor throws <see cref="System.ArgumentException"/> for a
        /// delegate of any other type.
        /// </para>
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        Delegate? InconsistentFieldCountHandler { get; }
#else
        [MaybeNull]
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
        /// Gets a flag indicating if blank fields at the end of a line are left out when it is written to a CSV file.
        /// </summary>
        /// <remarks>
        /// Applies to writing lines only.  Leaving out the trailing blank fields relaxes RFC 4180§2¶4, which says that each line should contain
        /// the same number of fields throughout the file.
        /// <para>
        /// A field is blank when the text written for it is empty.  A field whose value is whitespace is not blank.  The fields at the end of a record
        /// line are those last in the order they are written: header line order, else mapper order followed by the unmapped fields of the first
        /// record line, else the order of the first record line.  A record line whose fields are all blank is written with all of them, so that it
        /// isn't written as an empty line.
        /// </para>
        /// </remarks>
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
    public sealed record CsvFileOptions
#else
    public sealed class CsvFileOptions
#endif
        : ICsvFileOptions
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER

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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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


#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
        public CsvFileHeaderHandling HeaderHandling
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = CsvFileHeaderHandling.None;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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